#r "paket:
source https://api.nuget.org/v3/index.json
source /Users/viktora/developer/my-nugets
nuget FSharp.Core 4.5.4
nuget Fake.Core.Target
nuget Fake.Core.ReleaseNotes
nuget Fake.DotNet.Cli
nuget Fake.Dotnet.Testing.Expecto
nuget Fake.IO.FileSystem
nuget Fake.Tools.Git
nuget FSharp.Data
nuget Newtonsoft.Json
nuget Fake.Javascript.Yarn 
nuget FakeUtils //"
#load "./.fake/build.fsx/intellisense.fsx"
#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095

#endif

#nowarn "52"

open System
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.JavaScript
open ViktorVan.Fake

let tee f x =
    f x |> ignore
    x

module Config =
    let clientPath = Path.getFullName "./src/ChickenCheck.Client"
    let clientOutputPath = Path.combine clientPath "output"
    let backendPath = Path.getFullName "./src/ChickenCheck.Backend"
    let localBackendPath = Path.getFullName "./src/ChickenCheck.Backend.Local"
    let backendProj = Path.combine backendPath "ChickenCheck.Backend.fsproj"
    let backendBinPath =
        match Environment.environVarOrDefault "CHICKENCHECK_CONFIGURATION" "debug" with
        | "release" -> Path.combine backendPath "bin/Release/netcoreapp2.2"
        | _ -> Path.combine backendPath "bin/Debug/netcoreapp2.2"

    let deployPath = Path.getFullName "./deploy"
    let backendDeployPath = Path.combine deployPath "ChickenCheck.Backend"
    let clientDeployPath = Path.combine deployPath "ChickenCheck.Client/output"
    let migrationsPath = "./src/ChickenCheck.Migrations"
    let tokenSecret = Environment.environVarOrDefault "this isn't the secret you are looking for" "CHICKENCHECK_TOKEN_SECRET"

module Tools =
    let run cmd args workingDir =
        let arguments =
            args
            |> String.split ' '
            |> Arguments.OfArgs
        Command.RawCommand(cmd, arguments)
        |> CreateProcess.fromCommand
        |> CreateProcess.withWorkingDirectory workingDir
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore

    let runWithResult cmd args =
        let raiseError (res: ProcessResult<ProcessOutput>) =
            if res.ExitCode <> 0 then failwith res.Result.Error
            else res

        CreateProcess.fromRawCommand cmd args
        |> CreateProcess.redirectOutput
        |> Proc.run
        |> raiseError
    
module AzureConfig =
    let location = Environment.environVarOrDefault "CHICKENCHECK_AZURE_LOCATION" "westeurope" |> Azure.Location.parse
    let appName = Environment.environVarOrDefault "CHICKENCHECK_AZURE_APPNAME" "chickencheck"
    let resourceGroup = 
        match Environment.environVarOrNone "CHICKENCHECK_AZURE_RESOURCEGROUP" with 
        | Some rg -> rg |> Azure.ResourceGroupName
        | None -> appName |> Azure.ResourceGroupName
    let storageName = appName |> Azure.StorageAccountName.create
    let devStorageName = appName |> sprintf "%sdev" |> Azure.StorageAccountName.create 
    let storageSku = Environment.environVarOrDefault "CHICKENCHECK_AZURE_STORAGE_SKU" "standard_lrs" |> Azure.StorageSku.Parse
    let functionsAppName = appName |> sprintf "%s-functions" |> Azure.FunctionsApp
    let tenant = Environment.environVarOrNone "CHICKENCHECK_AZURE_TENANT" |> Option.map Azure.Tenant
    let databaseServer = appName |> Azure.ServerName
    let chickenCheckDatabase = appName |> sprintf "%sdb" |> Azure.DatabaseName

    let private dbUsername = Environment.environVarOrNone "CHICKENCHECK_DATABASE_USERNAME" 
    let private dbPassword = Environment.environVarOrNone "CHICKENCHECK_DATABASE_PASSWORD" 
    let databaseCredentials = 
        match dbUsername, dbPassword with
        | Some user, Some pw ->
            (user, pw) |> Azure.Credentials |> Some
        | _ -> None

let tryGetFakeVar key =
    FakeVar.get key
    |> function
    | Some value -> value
    | None -> sprintf "Could not read FAKE variable '%s'" key |> failwith

let setStorageAccessKeyVar = FakeVar.set "StorageAccessKey"
let getStorageAccessKeyVar() : Azure.AccessKey = tryGetFakeVar "StorageAccessKey" 
let setStorageConnectionStringVar = FakeVar.set "StorageConnectionString"
let getStorageConnectionStringVar() : Azure.ConnectionString = tryGetFakeVar "StorageConnectionString"
let setChickenCheckDbConnectionString = FakeVar.set "ChickenCheckDbConnectionString"
let getChickenCheckDbConnectionStringVar() : Azure.ConnectionString = tryGetFakeVar "ChickenCheckDbConnectionString"
let setInstrumentationKeyVar = FakeVar.set "AppInsightsInstrumentationKey"
let getInstrumentationKeyVar() : Azure.InstrumentationKey = tryGetFakeVar "AppInsightsInstrumentationKey"


Target.create "Clean" <| fun _ ->
    Shell.cleanDirs [ Config.deployPath; Config.clientOutputPath ]
    !! "src/**/bin/**/*"
    ++ "src/**/obj/**/*"
    ++ "test/**/bin/**/*"
    ++ "test/**/obj/**/*"
    |> File.deleteAll

// Build
    
Target.create "RestoreBackend" <| fun _ ->
    DotNet.restore id Config.backendPath
Target.create "BuildBackend" <| fun _ -> 
    DotNet.build (fun p -> { p with Configuration = DotNet.BuildConfiguration.Release }) Config.backendPath

Target.create "InstallClient" <| fun _ -> Yarn.install id

Target.create "CompileFable" <| fun _ ->
    (sprintf "webpack --config %s/webpack.config.js" Config.clientPath, id) ||> Yarn.exec

Target.create "FullBuild" ignore

// Development
Target.create "SetupDevStorage" <| fun _ ->
    let (connectionString, _) = Azure.Storage.createAccount AzureConfig.resourceGroup AzureConfig.devStorageName AzureConfig.location AzureConfig.storageSku
    Azure.Functions.setLocalSettings Config.backendPath connectionString

Target.create "Run" <| fun _ ->

    // let localSettings = Path.combine Config.backendPath "local.settings.json" |> Seq.singleton
    // Shell.copy Config.backendBinPath localSettings
    // DotNet.build (fun p -> { p with Configuration = DotNet.BuildConfiguration.Debug }) Config.backendPath

    // let backend = async { Azure.Functions.start Config.backendBinPath }
    let backend = 
        async { 
            DotNet.exec 
                (fun p -> 
                    { p with WorkingDirectory = Config.localBackendPath }) 
                "watch run" 
                (sprintf "%s %s" Config.backendPath Config.backendBinPath) |> ignore 
        }
    let client = async { 
        (sprintf "webpack-dev-server --config %s/webpack.config.js" Config.clientPath, id) 
        ||> Yarn.exec }

    let browser =
        let openBrowser url =
            //https://github.com/dotnet/corefx/issues/10361
            Command.ShellCommand url
            |> CreateProcess.fromCommand
            |> CreateProcess.ensureExitCodeWithMessage "opening browser failed"
            |> Proc.run
            |> ignore
        
        async {
            do! Async.Sleep 15000
            openBrowser "http://localhost:8080"
        }
    [ backend; client; browser ]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore

// Infrastructure

Target.create "AzureLogin" <| fun _ ->
    // verify we are logged in to correct tenant
    match AzureConfig.tenant with
    | None -> invalidArg "AzureConfig.tenant" "Missing setting for Azure Tenant"
    | Some tenant ->
    let currentTenant = Azure.Account.getCurrentTenant()
    if currentTenant <> tenant then failwith "Not logged in to expected azure tenant"

Target.create "SetupResourceGroup" <| fun _ ->
    Azure.ResourceGroup.create AzureConfig.location AzureConfig.resourceGroup |> ignore

Target.create "SetupAppInsights" <| fun _ ->
    Azure.AppInsights.create AzureConfig.resourceGroup AzureConfig.appName AzureConfig.location
    |> setInstrumentationKeyVar

Target.create "SetupStorageAccount" <| fun _ ->
    let (connectionString, accessKey) =
        Azure.Storage.createAccount AzureConfig.resourceGroup AzureConfig.storageName AzureConfig.location AzureConfig.storageSku 

    setStorageConnectionStringVar connectionString
    setStorageAccessKeyVar accessKey
    Azure.Storage.enableStaticWebhosting AzureConfig.storageName "404.html" "index.html"

Target.create "SetupFunctionsApp" <| fun _ ->
    Azure.Functions.create AzureConfig.resourceGroup AzureConfig.location AzureConfig.functionsAppName AzureConfig.storageName

Target.create "SetupDatabase" <| fun _ ->
    let getConnectionString credentials =
        let withServerName (Azure.ServerName serverName) connectionString =
            String.replace "<servername>" serverName connectionString
        let withUsername user connectionString =
            String.replace "<username>" user connectionString
        let withPassword pw connectionString =
            String.replace "<password>" pw connectionString

        let (connectionString: Azure.ConnectionString) = Azure.Sql.getConnectionString AzureConfig.chickenCheckDatabase 

        let (Azure.Credentials (user, pw)) = credentials
        connectionString.Val
        |> withServerName AzureConfig.databaseServer
        |> withUsername user
        |> withPassword pw
        |> Azure.ConnectionString.create

    let getLocalIp() =
        Tools.runWithResult "curl" [ "ifconfig.me" ]
        |> (fun res -> res.Result.Output)

    match AzureConfig.databaseCredentials with
    | None -> failwith "Missing setting for database credentials"
    | Some credentials ->
        Azure.Sql.createDatabase AzureConfig.location AzureConfig.resourceGroup AzureConfig.databaseServer AzureConfig.chickenCheckDatabase credentials

        let localIp = getLocalIp()
        Azure.Sql.createFirewallRule AzureConfig.resourceGroup AzureConfig.databaseServer Environment.MachineName localIp

        Azure.Sql.allowAccessFromAzureServices AzureConfig.resourceGroup AzureConfig.databaseServer

        let connectionString = getConnectionString credentials 

        setChickenCheckDbConnectionString connectionString

// Deploy
Target.create "PublishBackend" <| fun _ ->
    DotNet.publish (fun o ->
        { o with Configuration = DotNet.BuildConfiguration.Release
                 OutputPath = Some Config.backendDeployPath }) Config.backendProj

    let host = Path.combine Config.backendPath "host.json" 
    Shell.copyFile Config.backendDeployPath host

Target.create "BundleClient" <| fun _ ->
    Shell.copyDir Config.clientDeployPath Config.clientOutputPath FileFilter.allFiles

Target.create "Bundle" ignore

Target.create "UploadWebsite" <| fun _ ->
    let storageConnection = getStorageConnectionStringVar()
    let blob = Azure.BlobName "$web"
    Azure.Storage.deleteAll storageConnection blob
    Azure.Storage.uploadBatchBlob Config.clientDeployPath storageConnection blob

Target.create "DeployFunctionsApp" <| fun _ ->
    Azure.Functions.deploy Config.backendDeployPath AzureConfig.functionsAppName

    let (storageConnectionString: Azure.ConnectionString) = getStorageConnectionStringVar()
    let (dbConnectionString: Azure.ConnectionString) = getChickenCheckDbConnectionStringVar()
    let (Azure.InstrumentationKey instrumentationKey) = getInstrumentationKeyVar()
    let settings = [ "CHICKENCHECK_CONNECTIONSTRING", dbConnectionString.Val 
                     "CHICKENCHECK_TOKEN_SECRET", Config.tokenSecret
                     "STORAGE_CONNECTION", storageConnectionString.Val
                     "APPINSIGHTS_INSTRUMENTATIONKEY", instrumentationKey ] |> Map.ofList
    Azure.Functions.setAppSettings AzureConfig.resourceGroup AzureConfig.functionsAppName settings

Target.create "RunMigrations" <| fun _ ->
    let (connectionString: Azure.ConnectionString) = getChickenCheckDbConnectionStringVar()
    let args = sprintf "--connectionstring \"%s\"" connectionString.Val
    DotNet.exec (fun o -> { o with WorkingDirectory = Config.migrationsPath }) "run" args
    |> (fun res -> if not res.OK then String.Join(", ", res.Errors) |> failwith)

Target.create "SetupInfrastructure" ignore

Target.create "Deploy" ignore

let releaseNotes = System.IO.File.ReadAllLines "RELEASE_NOTES.md"

let release =
    releaseNotes
    |> ReleaseNotes.parse

Target.create "TagRelease" <| fun _ ->
    // Read additional information from the release notes document

    let tagName = string release.NugetVersion
    printfn "%s" tagName
    Fake.Tools.Git.Branches.deleteTag "" tagName
    Fake.Tools.Git.Branches.tag "" tagName
    Fake.Tools.Git.Branches.pushTag "" "origin" tagName

Target.create "SetReleaseNotes" <| fun _ ->
    let lines = [
        "// auto-generated from RELEASE_NOTES.md"
        ""
        "module internal ReleaseNotes"
        ""
        (sprintf "let version = \"%s\"" release.NugetVersion)
        ""
        "let notes = \"\"\""] @ Array.toList releaseNotes @ [ "\"\"\"" ]
    System.IO.File.WriteAllLines(Config.clientPath + "/ReleaseNotes.fs", lines)

// Dependency order
"SetupResourceGroup"
    ==> "SetupDevStorage"

"RestoreBackend" ==> "BuildBackend"
"InstallClient" ==> "SetReleaseNotes" ==> "CompileFable"

"BuildBackend" <=> "CompileFable"
    ==> "FullBuild"

"Clean" ?=> "RestoreBackend"
"Clean" ?=> "InstallClient"

"BuildBackend"
    ==> "PublishBackend"

"CompileFable"
    ==> "BundleClient"

"Clean" 
    ==> "PublishBackend" <=> "BundleClient" 
    ==> "Bundle" 

"BundleClient"
    ==> "UploadWebsite"

"PublishBackend"
    ==> "DeployFunctionsApp"

"AzureLogin"
    ==> "SetupResourceGroup"
    ==> "SetupStorageAccount"
    ==> "SetupDatabase"
    ==> "SetupFunctionsApp" 
    ==> "SetupInfrastructure"

"SetupResourceGroup"
    ==> "SetupDatabase"
    ==> "SetupInfrastructure"

"SetupStorageAccount"
    ==> "SetupInfrastructure"

"SetupStorageAccount"
    ==> "UploadWebsite" 

"SetupResourceGroup"
    ==> "SetupAppInsights"
    ==> "SetupInfrastructure"

"SetupAppInsights"
    ==> "DeployFunctionsApp"

"SetupFunctionsApp"
    ==> "DeployFunctionsApp"

"SetupDatabase"
    ==> "RunMigrations"

"UploadWebsite" 
   ==> "TagRelease"

"DeployFunctionsApp" 
    ==> "TagRelease"

"RunMigrations"
    ==> "TagRelease"

"TagRelease"
    ==> "Deploy"

let ctx = Target.WithContext.runOrDefaultWithArguments "FullBuild"
Target.updateBuildStatus ctx
Target.raiseIfError ctx
