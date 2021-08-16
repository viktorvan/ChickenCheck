open System
open BlackFox
open BlackFox.Fake
open Fake.Core
open Fake.DotNet
open Fake.Tools
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

let execContext = Context.FakeExecutionContext.Create false "Build.fs" [ ]
Context.setExecutionContext (Context.RuntimeContext.Fake execContext)

type Tag =
    | Build
    | Docker
    | Dev
    | Prod

//-----------------------------------------------------------------------------
// Metadata and Configuration
//-----------------------------------------------------------------------------

let sln = "ChickenCheck.sln"
let rootPath = __SOURCE_DIRECTORY__
let outputDir = rootPath @@ "output"
let src = rootPath @@ "src"
let serverPath = src @@ "ChickenCheck.Backend"
let serverProj = serverPath @@ "ChickenCheck.Backend.fsproj"
let migrationsPath = src @@ "ChickenCheck.Migrations"
let dbBackupPath = src @@ "ChickenCheck.DbBackup"
let unitTestsPath = rootPath @@ "test" @@ "ChickenCheck.UnitTests"
let webTestsPath = rootPath @@ "test" @@ "ChickenCheck.WebTests"
let localDatabaseUser = ChickenCheckConfiguration.config.Value.Local.Db.User
let localDatabasePassword = ChickenCheckConfiguration.config.Value.Local.Db.Password
let connectionString = sprintf $"User ID=%s{localDatabaseUser};Password=%s{localDatabasePassword};Host=localhost;Port=5432;Database=chickencheck;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;Connection Lifetime=0;"
let dockerRegistry = "cloud.canister.io:5000/viktorvan"
let appName = "chickencheck"
let serverDockerfile = "Backend.Dockerfile"
let toolsDockerfile = "Tools.Dockerfile"
let arm64ImageSuffix = "-arm64"
let dockerWebTestContainerName = "chickencheckwebtest"
let srcGlob = src @@ "**/*.??proj"
let testsGlob = rootPath  @@ "test/**/*.??proj"
let changelog = Fake.Core.Changelog.load "CHANGELOG.md"
let semVersion = changelog.LatestEntry.SemVer
let devDomain = ChickenCheckConfiguration.config.Value.Dev.Domain
let dev0ClientId = ChickenCheckConfiguration.config.Value.Dev.ClientId
let dev0ClientSecret = ChickenCheckConfiguration.config.Value.Dev.ClientSecret
let prodDomain = ChickenCheckConfiguration.config.Value.Prod.Domain
let prod0ClientId = ChickenCheckConfiguration.config.Value.Prod.ClientId
let prod0ClientSecret = ChickenCheckConfiguration.config.Value.Prod.ClientSecret


//-----------------------------------------------------------------------------
// Helpers
//-----------------------------------------------------------------------------

type FakeVarService<'T>(key) =
    member __.Get() : 'T option = FakeVar.get key
    member __.GetOrFail() : 'T = FakeVar.getOrFail key
    member __.Set(value: 'T) = FakeVar.set key value

let buildVersionService = FakeVarService<SemVerInfo> "buildVersion"

let fullVersion (semVer: SemVerInfo) =
    if semVer.Build = bigint(0) then
        { semVer with 
            Build = bigint(1)
            Original = None }
    else semVer

let getGitTags() = 
    match Git.CommandHelper.runGitCommand "" "tag" with
    | false, _,_ -> 
        failwith "git error"
    | true, existingTags, _ -> 
        existingTags 

let getBuildVersion() =
    match buildVersionService.Get() with
    | None ->
        let getNextVersion existingVersionTags version =
            let nextVersions = 
                let versionWithBuildNumber (i:int) = { version with Build = version.Build + bigint(i); Original = None}
                Seq.initInfinite versionWithBuildNumber
            let validVersion = 
                let isExistingTag (v:SemVerInfo) = 
                    existingVersionTags |> List.contains v.AsString
                nextVersions
                |> Seq.skipWhile isExistingTag
                |> Seq.head

            validVersion

        let existingTags = getGitTags() |> List.filter (fun (str:string) -> str.StartsWith("BUILD")) |> List.map (fun str -> str.Substring(6))
        let version = getNextVersion existingTags (fullVersion semVersion)
        buildVersionService.Set version
        version.AsString
    | Some v -> v.AsString

let dockerImageFullName imageName version =
    sprintf "%s/%s:%s" dockerRegistry imageName version

let dockerBuildImage dockerfile imageName version =
    let fullName = dockerImageFullName imageName version
    let args = [ "build"; "-t"; fullName; "-f"; dockerfile; "." ]
    Common.docker args ""

let dockerPushImage imageName version =
    let fullName = dockerImageFullName imageName version
    let args = [ "push"; fullName ]
    Common.docker args ""

let getHelmPackageName() =
    Directory.findFirstMatchingFile "*.tgz" rootPath

let escapeComma (str: string) =
    str.Replace(",", "\,")


//-----------------------------------------------------------------------------
// Build Target Implementations
//-----------------------------------------------------------------------------

let clean _ =
    [ "temp"; outputDir ]
    |> Shell.cleanDirs

    !! srcGlob
    ++ testsGlob
    |> Seq.collect(fun p ->
        ["bin";"obj"]
        |> Seq.map(fun sp ->
            IO.Path.GetDirectoryName p @@ sp)
        )
    |> Shell.cleanDirs

    [ "paket-files/paket.restore.cached" ]
    |> Seq.iter Shell.rm

let verifyCleanWorkingDirectory _ =
    if not (Git.Information.isCleanWorkingCopy "") then failwith "Working directory is not clean"
    let currentBranchName = Git.Information.getBranchName "" 
    if (currentBranchName <> "master") then failwithf "Can only release master, current branch is: %s" currentBranchName 

let verifyDockerInstallation _ =
    Common.docker [ "version" ] ""

let dotnetRestore _ = DotNet.restore id sln

let runMigrations _ = Common.runMigrations migrationsPath connectionString

let writeVersionToFile _ =
    let sb = System.Text.StringBuilder("module Version\n\n")
    Printf.bprintf sb "    let version = \"%s\"\n" (getBuildVersion())

    File.writeString false (serverPath @@ "Version.fs") (sb.ToString())
    
let dotnetBuild ctx =
    let args =
        [
            sprintf "/p:PackageVersion=%s" (fullVersion semVersion).AsString
        ]
    DotNet.build(fun c ->
        { c with
//            Configuration = Common.configuration ctx.Context.AllExecutingTargets
            NoRestore = true
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
        }) sln

let dotnetPublishServer ctx =
    let args =
        [
            sprintf "/p:PackageVersion=%s" (fullVersion semVersion).AsString
        ]
    DotNet.publish(fun c ->
        { c with
//            Configuration = Common.configuration (ctx.Context.AllExecutingTargets)
            NoBuild = true
            NoRestore = true
            SelfContained = Some false
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
            OutputPath = Some (outputDir @@ "server")
        }) serverProj

let dotnetPublishMigrations ctx =
    let args =
        [
            sprintf "/p:PackageVersion=%s" (fullVersion semVersion).AsString
        ]
    DotNet.publish(fun c ->
        { c with
//            Configuration = Common.configuration (ctx.Context.AllExecutingTargets)
            NoBuild = true
            NoRestore = true
            SelfContained = Some false
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
            OutputPath = Some (outputDir @@ "migrations")
        }) migrationsPath

let dotnetPublishDbBackup ctx =
    let args =
        [
            sprintf "/p:PackageVersion=%s" (fullVersion semVersion).AsString
        ]
    DotNet.publish(fun c ->
        { c with
            Configuration = Common.configuration (ctx.Context.AllExecutingTargets)
            NoBuild = true
            NoRestore = true
            SelfContained = Some false
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
            OutputPath = Some (outputDir @@ "dbbackup")
        }) dbBackupPath

let dockerBuild _ =
    Common.docker [ "--version" ] ""
    let tag = getBuildVersion() + arm64ImageSuffix
    dockerBuildImage serverDockerfile appName tag
    dockerBuildImage toolsDockerfile (appName + "-tools") tag

let dockerPush _ =
    let tag = getBuildVersion() + arm64ImageSuffix
    dockerPushImage appName tag
    dockerPushImage (appName + "-tools") tag

let runUnitTests ctx =
    let configuration = "release"//Common.configuration (ctx.Context.AllExecutingTargets)
    let args = sprintf "--configuration %s --no-restore --no-build --fail-on-focused-tests" (configuration.ToString())
    DotNet.exec (fun c ->
        { c with 
            WorkingDirectory = unitTestsPath }) "run" args
    |> (fun res -> if not res.OK then failwithf "RunUnitTests failed")

// Using DotNet.test to run the tests would give us better test result reporting in Azure DevOps, but:
// There is a bug in DotNet.test that, it does not use RunSettingsArguments https://github.com/fsharp/FAKE/issues/2376
// Until it is fixed we can run the tests with dotnet run (above) to be able to pass arguments.
//let runUnitTests _ =
//    DotNet.test
//        (fun c ->
//            { c with
//                NoBuild = true
//                RunSettingsArguments = Some "-- Expecto.fail-on-focused-tests=true" })
//        sln

let runWebTests ctx =
    let configuration = "release"//Common.configuration (ctx.Context.AllExecutingTargets)
    let args = sprintf "--configuration %s --no-restore --no-build -- https://dev.chickens.viktorvan.com true" (configuration.ToString())
    DotNet.exec (fun c ->
        { c with WorkingDirectory = webTestsPath }) "run" args
    |> (fun res -> if not res.OK then failwithf "RunWebTests failed")

let watchApp _ =
    let server() =
        Common.DotNetWatch "run" serverPath // not working in .net preview 6

    [ server ]
    |> Seq.iter (Common.invokeAsync >> Async.Catch >> Async.Ignore >> Async.Start)
    printfn "Press Ctrl+C (or Ctrl+Break) to stop..."
    let cancelEvent = Console.CancelKeyPress |> Async.AwaitEvent |> Async.RunSynchronously
    cancelEvent.Cancel <- true

let watchTests _ =
    !! testsGlob
    |> Seq.map(fun proj -> fun () ->
        Common.DotNetWatch "test" proj
        |> ignore
    )
    |> Seq.iter (Common.invokeAsync >> Async.Catch >> Async.Ignore >> Async.Start)

    printfn "Press Ctrl+C (or Ctrl+Break) to stop..."
    let cancelEvent = Console.CancelKeyPress |> Async.AwaitEvent |> Async.RunSynchronously
    cancelEvent.Cancel <- true

let gitTagDeployment (tag: Tag) _ =
    let addTag envStr =
        match tag with
        | Build | Docker -> ()
        | Dev | Prod ->
            try 
                Git.Branches.deleteTag "" envStr
            with _ -> 
                Trace.tracef "Could not find existing tag %s" envStr
            Git.Branches.tag "" envStr

    let addTagWithVersion version =
        let existingTags = getGitTags()
        if existingTags |> List.contains version then
            Git.Branches.deleteTag "" version
        Git.Branches.tag "" version

    let gitPush() =
        let branch = Git.Information.getBranchName ""
        Git.Branches.pushBranch "" "origin" branch

    let envStr = (tag.ToString().ToUpper())
    let version = envStr + "-" + getBuildVersion()

    addTag envStr
    addTagWithVersion version

    if tag = Prod then gitPush()

let helmPackage _ =
    let version = getBuildVersion()
    let packageArgs = [
        "package"
        "--app-version"
        version
        "./helm"
    ]

    !! "*.tgz"
    |> Seq.iter Shell.rm

    Common.kubectl [ "config"; "use-context"; "microk8s" ] ""
    Common.helm packageArgs ""

let helmInstallDev _ = 
    let deployArgs = [
        "upgrade"
        sprintf "%s-dev" appName
        "--install"
        "-f"
        "./helm/values.dev.yaml"
        "--set"
        sprintf "authentication.clientSecret=%s" ChickenCheckConfiguration.config.Value.Dev.ClientSecret
        "--set"
        sprintf "dataProtectionCertificatePassword=%s" ChickenCheckConfiguration.config.Value.DataProtectionCertificatePassword |> escapeComma
        getHelmPackageName()
    ]

    Common.kubectl [ "config"; "use-context"; "microk8s" ] ""
    Common.helm deployArgs rootPath |> ignore

let waitForDeployment env _ =
    let waitForResponse timeout url =
        let sw = System.Diagnostics.Stopwatch.StartNew()
        let mutable lastExceptionMsg = ""
        let rec waitForResponse'() =
            if sw.Elapsed < timeout then
                try 
                    Fake.Net.Http.get "" "" url |> ignore
                    Trace.tracefn "Site %s responded after %f s" url sw.Elapsed.TotalSeconds
                with exn ->
                    lastExceptionMsg <- exn.ToString()
                    Trace.tracefn "Site %s not responding after %f s, waiting..." url sw.Elapsed.TotalSeconds
                    System.Threading.Thread.Sleep 2000
                    waitForResponse'()
            else 
                Trace.traceErrorfn "%O" lastExceptionMsg 
                failwithf "Site %s is not running after %f s" url timeout.TotalSeconds
        waitForResponse'()


    Trace.tracefn "Waiting 5 seconds before warmup tests..."
    System.Threading.Thread.Sleep 5000
    match env with
    | Dev ->
        waitForResponse (TimeSpan.FromSeconds(30.)) "https://dev.chickens.viktorvan.com/eggs"
    | Prod ->
        waitForResponse (TimeSpan.FromSeconds(30.)) "https://chickens.viktorvan.com/eggs"
    | _ -> ()

let helmInstallProd _ = 
    let deployArgs = [
        "upgrade"
        appName
        "--install"
        "-f"
        "./helm/values.prod.yaml"
        "--set"
        sprintf "authentication.clientSecret=%s" ChickenCheckConfiguration.config.Value.Prod.ClientSecret
        "--set"
        sprintf "dataProtectionCertificatePassword=%s" ChickenCheckConfiguration.config.Value.DataProtectionCertificatePassword |> escapeComma
        "--set"
        sprintf "azureStorageConnectionString=%s" ChickenCheckConfiguration.config.Value.Backup.AzureStorageConnectionString
        getHelmPackageName()
    ]

    Common.kubectl [ "config"; "use-context"; "microk8s" ] ""
    Common.helm deployArgs rootPath |> ignore

//-----------------------------------------------------------------------------
// Build Target Declaration
//-----------------------------------------------------------------------------

module Targets =
    let createTargetsAndGetDefault() =
        let clean = BuildTask.createFn "Clean" [] (fun _ -> clean())
        let dotnetRestore = BuildTask.create "DotnetRestore" [ clean.IfNeeded ] { dotnetRestore() }
        let runMigrations = BuildTask.create "RunMigrations" [ dotnetRestore ] { runMigrations() }
        let writeVersionToFile = BuildTask.create "WriteVersionToFile" [ dotnetRestore ] { writeVersionToFile() }
        let dotnetBuild = BuildTask.create "DotnetBuild" [ runMigrations; writeVersionToFile ] { dotnetBuild() }
        let runUnitTests = BuildTask.create "RunUnitTests" [ dotnetBuild ] { runUnitTests() }
        let watchApp = BuildTask.create "WatchApp" [ writeVersionToFile ] { watchApp() }
        let run = BuildTask.createEmpty "Run" [ watchApp ]
        let watchTests = BuildTask.create "WatchTests" [ dotnetRestore ] { watchTests() }
        let build = BuildTask.createEmpty "Build" [ runUnitTests ]
        let publishServer = BuildTask.create "DotnetPublishServer" [ build ] { dotnetPublishServer() }
        let publishMigrations = BuildTask.create "dotnetPublishMigrations" [ build ] { dotnetPublishMigrations() }
        let package = BuildTask.createEmpty "Package" [ clean; publishServer; publishMigrations ]
        let verifyDockerInstallation = BuildTask.create "VerifyDockerInstallation" [] { verifyDockerInstallation() }
        let dockerBuild = BuildTask.create "DockerBuild" [ package; verifyDockerInstallation ] { dockerBuild() }
        let dockerPush = BuildTask.create "DockerPush" [ dockerBuild ] { dockerPush() }
        let gitTagDockerDeployment = BuildTask.create "GitTagDockerDeployment" [ dockerPush ] { gitTagDeployment Docker () }
        let helmPackage = BuildTask.create "HelmPackage" [ gitTagDockerDeployment ] { helmPackage() }
        let helmInstallDev = BuildTask.create "HelmInstallDev" [ helmPackage ] { helmInstallDev() }
        let gitTagDevDeployment = BuildTask.create "GitTagDevDeployment" [ helmInstallDev ] { gitTagDeployment Dev () }
        let waitForDevDeployment = BuildTask.create "WaitForDevDeployment" [ gitTagDevDeployment ] { waitForDeployment Dev () }
        let runWebTests = BuildTask.create "RunWebTests" [ waitForDevDeployment ] { runWebTests() }
        let helmInstallProd = BuildTask.create "HelmInstallProd" [ runWebTests ] { helmInstallProd() }
        let gitTagProdDeployment = BuildTask.create "GitTagProdDeployment" [ helmInstallProd ] { gitTagDeployment Prod () }
        let waitForProdDeployment = BuildTask.create "WaitForProdDeployment" [ gitTagProdDeployment ] { waitForDeployment Prod () }
        let verifyCleanWorkingDirectory = BuildTask.create "VerifyCleanWorkingDirectory" [ clean.IfNeeded ] { verifyCleanWorkingDirectory() }
        let createRelease = BuildTask.createEmpty "CreateRelease" [ verifyCleanWorkingDirectory; waitForProdDeployment ]
        BuildTask.createEmpty "Default" [ run ]


//-----------------------------------------------------------------------------
// Start
//-----------------------------------------------------------------------------


[<EntryPoint>]

let main args =
    BuildTask.setupContextFromArgv args
    let defaultTarget = Targets.createTargetsAndGetDefault()
    BuildTask.runOrDefaultApp defaultTarget
