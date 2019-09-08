#r "paket:
nuget FSharp.Core 4.5.4
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.Dotnet.Testing.Expecto
nuget Fake.IO.FileSystem
nuget FSharp.Data
nuget Newtonsoft.Json
nuget Fake.Javascript.Yarn //"
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
open FSharp.Data
open FSharp.Data.JsonExtensions
open Newtonsoft.Json

let tee f x =
    f x |> ignore
    x

module Config =
    let clientPath = Path.getFullName "./src/ChickenCheck.Client"
    let clientOutputPath = Path.combine clientPath "output"
    let backendPath = Path.getFullName "./src/ChickenCheck.Backend"
    let backendProj = Path.combine backendPath "ChickenCheck.Backend.fsproj"
    let backendBinPath =
        match Environment.environVarOrDefault "CHICKENCHECK_CONFIGURATION" "debug" with
        | "release" -> Path.combine backendPath "bin/Release/netcoreapp2.2"
        | _ -> Path.combine backendPath "bin/Debug/netcoreapp2.2"

    let deployPath = Path.getFullName "./deploy"
    let backendDeployPath = Path.combine deployPath "ChickenCheck.Backend"
    let clientDeployPath = Path.combine deployPath "ChickenCheck.Client/output"
    let migrationsPath = "./src/ChickenCheck.Migrations"

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

module Azure =
    let private az args = 
        Tools.runWithResult "az" args

    let private func workingDir args = 
        Tools.run "func" args workingDir

    let private toJson (result:ProcessResult<ProcessOutput>) =
        result.Result.Output
        |> JsonValue.Parse


    type ServicePrincipal = ServicePrincipal of string
    type ServicePrincipalSecret = ServicePrincipalSecret of string
    type Tenant = Tenant of string
    module Tenant =
        let create str =
            if String.IsNullOrWhiteSpace str then invalidArg "TenantId" "Cannot be empty"
            else Tenant str

    type Location = 
        | WestEurope
        member this.Value = 
            match this with
            | WestEurope -> "westeurope"

    module Location =   
        let parse str =
            match str with
            | "westeurope" -> WestEurope
            | _ -> failwith "invalid Azure location"

    type ResourceGroupName = ResourceGroupName of string

    type StorageAccountName = private StorageAccountName of string
    module StorageAccountName =
        let create (str:string) =
            str.ToLower() |> StorageAccountName

    type ConnectionString = private ConnectionString of string
    module ConnectionString =
        let create str =
            if String.IsNullOrWhiteSpace str then invalidArg "connectionString" "Connection string cannot be empty"
            elif str.StartsWith("\"") || str.EndsWith("\"") then invalidArg "connectionString" "Not correctly parsed"
            else ConnectionString str
        let value (ConnectionString conn) = conn

    type ConnectionString with
        member this.Val = ConnectionString.value this


    type InstrumentationKey = InstrumentationKey of string

    type AccessKey = AccessKey of string
    module AccessKey =
        let create str =
            if String.isNullOrWhiteSpace str then invalidArg "accessKey" "Access key cannot be empty"
            else AccessKey str

    type Credentials = Credentials of Username: string * Password: string
    module Credentials =
        let create (user, pw) =
            if String.isNullOrWhiteSpace user then invalidArg "Username" "Cannot be empty"
            elif String.isNullOrWhiteSpace pw then invalidArg "Password" "Cannot be empty"
            else Credentials (Username = user, Password = pw)

    type ServerName = ServerName of string
    type DatabaseName = DatabaseName of string
    type BlobName = BlobName of string

    type StorageSku = 
        | StandardLRS
        member this.Value = 
            match this with
            | StandardLRS -> "Standard_LRS"
        static member Parse (str:string) =
            match str.ToLower() with
            | "standard_lrs" -> StandardLRS
            | _ -> failwith "invalid Storage SKU"
    type FunctionsApp = FunctionsApp of string 
    module ResourceGroup =
        let create (location:Location) (ResourceGroupName name) =  
            [ "group"; "create"; "--location"; location.Value; "--name"; name ]
            |> az

    module Account =
        let login (ServicePrincipal servicePrincipal) (ServicePrincipalSecret secret) (Tenant tenant) = 
            [ "login"; "--service-principal"; "-u"; servicePrincipal; "-p"; secret; "--tenant"; tenant ] 
            |> az
            |> ignore

        let getCurrentTenant() =
            let getTenantId (json:JsonValue) =  
                json?tenantId.AsString()
                |> Tenant.create

            [ "account"; "show" ]
            |> az
            |> toJson
            |> getTenantId

    module AppInsights =
        type Properties = { kind: string }
        let create (ResourceGroupName resourceGroup) appName (location:Location)=
            let getInstrumentationKey (json:JsonValue) =
                json?properties?InstrumentationKey.AsString()
                |> InstrumentationKey



            let properties = """{ "kind" : "api" }"""
            ["resource"; "create"; "--resource-group"; resourceGroup; "--resource-type"; "Microsoft.Insights/components"; "--name"; sprintf "%s-appinsights" appName; "--location"; location.Value; "--properties"; properties]
            |> az
            |> toJson
            |> getInstrumentationKey

    module Storage =
        let createAccount (ResourceGroupName resourceGroup) (StorageAccountName name) (location:Location) (sku:StorageSku) =
            let getConnectionString (json:JsonValue) =
                json?connectionString.AsString()
                |> ConnectionString.create

            [ "storage"; "account"; "create"; "--name"; name; "--resource-group"; resourceGroup; "--location"; location.Value; "--sku"; sku.Value; "--kind"; "StorageV2" ]
            |> az
            |> ignore

            let connectionString =
                [ "storage"; "account"; "show-connection-string"; "--resource-group"; resourceGroup; "--name"; name]
                |> az
                |> toJson
                |> getConnectionString

            let getKey1 (json:JsonValue) =
                json.AsArray().[0]?value.AsString()
                |> AccessKey.create

            let accessKey =
                [ "storage"; "account"; "keys"; "list"; "-n"; name ]
                |> az
                |> toJson
                |> getKey1

            connectionString, accessKey

        let enableStaticWebhosting (StorageAccountName storageAccountName) errorDocument indexDocument =
            az [ "extension"; "add"; "--name"; "storage-preview" ]
            |> ignore

            [ "storage"; "blob"; "service-properties"; "update"; "--account-name"; storageAccountName; "--static-website"; "--404-document"; errorDocument; "--index-document"; indexDocument ]
            |> az
            |> ignore

        let uploadBatchBlob sourcePath (ConnectionString storageConnectionString) (BlobName blob)=
            [ "storage"; "blob"; "upload-batch"; "-s"; sourcePath; "-d"; blob; "--connection-string"; storageConnectionString ]
            |> az
            |> ignore

        let deleteAll (ConnectionString storageConnectionString) (BlobName blob) =
            [ "storage"; "blob"; "delete-batch"; "-s"; blob; "--pattern"; "'*'"; "--connection-string"; storageConnectionString ]
            |> az
            |> ignore

    module Functions =
        let create (ResourceGroupName resourceGroup) (location: Location) (FunctionsApp name) (StorageAccountName storageAccountName) =
            [ "functionapp"; "create"; "--resource-group"; resourceGroup; "--consumption-plan-location"; location.Value; "--name"; name; "--storage-account"; storageAccountName ]
            |> az
            |> ignore

        let setAppSettings (ResourceGroupName resourceGroup) (FunctionsApp functionsApp) settings =

            let settings =
                settings
                |> Map.toList
                |> List.map (fun (key, value) -> sprintf "%s=%s" key value)

            [ "functionapp"; "config"; "appsettings"; "set"; "--resource-group"; resourceGroup; "--name"; functionsApp; "--settings" ] @ settings
            |> az
            |> ignore

        let start workingDir =
            func workingDir "start"
        
        let private setDotNetRuntime workingDir =
            sprintf "settings add FUNCTIONS_WORKER_RUNTIME dotnet" |> func workingDir

        let deploy workingDir (FunctionsApp functionsAppName) =    
            setDotNetRuntime workingDir
            sprintf "azure functionapp publish %s" functionsAppName
            |> func workingDir

        let setLocalSettings workingDir (ConnectionString storageConnection) =   
            setDotNetRuntime workingDir
            sprintf "settings add AzureWebJobsStorage %s" storageConnection |> func workingDir

    module Sql =
        let createDatabase (location:Location) (ResourceGroupName resourceGroup) (ServerName serverName) (DatabaseName databaseName) (Credentials (user, pw)) =

            let createServer() =
                [ "sql"; "server"; "create"; "--name"; serverName; "--location"; location.Value; "-g"; resourceGroup; "-u"; user; "-p"; pw ]
                |> az
                |> ignore

            createServer()

            [ "sql"; "db"; "create"; "-g"; resourceGroup; "-s"; serverName; "-n"; databaseName; "-e"; "Basic" ]
            |> az
            |> ignore

        let createFirewallRule (ResourceGroupName resourceGroup) (ServerName serverName) ruleName ipAddress =
            [ "sql"; "server"; "firewall-rule"; "create"; "-g"; resourceGroup; "-s"; serverName; "-n"; ruleName; "--start-ip-address"; ipAddress; "--end-ip-address"; ipAddress ]
            |> az
            |> ignore

        let allowAccessFromAzureServices resourceGroup serverName =
            createFirewallRule resourceGroup serverName "azure-services" "0.0.0.0"

        let getConnectionString (DatabaseName name) =
            let parseConnectionString (result: ProcessResult<ProcessOutput>) =
                result.Result.Output
                |> (fun (str:string) -> str.Trim().Trim('"'))
                |> ConnectionString.create

            [ "sql"; "db"; "show-connection-string"; "--name"; name; "--client"; "ado.net" ]
            |> az
            |> parseConnectionString
    
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
    let backend = async { Azure.Functions.start Config.backendBinPath }
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
        
        let localSettings = Path.combine Config.backendPath "local.settings.json" |> Seq.singleton
        Shell.copy Config.backendBinPath localSettings
        DotNet.build (fun p -> { p with Configuration = DotNet.BuildConfiguration.Debug }) Config.backendPath

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
    let settings = [ "ChickenCheckDbConnectionString", dbConnectionString.Val 
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

// Dependency order
"SetupResourceGroup"
    ==> "SetupDevStorage"

"RestoreBackend" ==> "BuildBackend"
"InstallClient" ==> "CompileFable"

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
   ==> "Deploy"

"DeployFunctionsApp" 
    ==> "Deploy"

"RunMigrations"
    ==> "Deploy"

let ctx = Target.WithContext.runOrDefaultWithArguments "FullBuild"
Target.updateBuildStatus ctx
Target.raiseIfError ctx
