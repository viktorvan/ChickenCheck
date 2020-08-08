#r "paket: groupref Build //"
#load ".fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "Facades/netstandard"
#r "netstandard"
#endif
#load "src/tools/common.fsx"
#load "src/tools/chickenCheckConfiguration.fsx"
open System
open Fake.Core
open Fake.DotNet
open Fake.Tools
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators


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
let unitTestsPath = rootPath @@ "test" @@ "ChickenCheck.UnitTests"
let webTestsPath = rootPath @@ "test" @@ "ChickenCheck.WebTests"
let connectionString = sprintf "Data Source=%s/database-dev.db" serverPath
let dockerRegistry = "microk8s-1.local:32000"
let appName = "chickencheck"
let serverDockerFile = serverPath @@ "Dockerfile"
let migrationsDockerFile = migrationsPath @@ "Dockerfile"
let arm64ImageSuffix = "-arm64"
let dockerWebTestContainerName = "chickencheckwebtest"
let helmPackagesGlob = rootPath @@ appName + "*.tgz"
let srcGlob = src @@ "**/*.??proj"
let testsGlob = rootPath  @@ "test/**/*.??proj"
let changelog = Fake.Core.Changelog.load "CHANGELOG.md"
let semVersion = changelog.LatestEntry.SemVer
let releaseTagPrefix = "Release-"
let prodDeployTagPrefix = "PROD-"
let devDomain = ChickenCheckConfiguration.config.Value.Dev.Domain
let dev0ClientId = ChickenCheckConfiguration.config.Value.Dev.ClientId
let dev0ClientSecret = ChickenCheckConfiguration.config.Value.Dev.ClientSecret
let prodDomain = ChickenCheckConfiguration.config.Value.Prod.Domain
let prod0ClientId = ChickenCheckConfiguration.config.Value.Prod.ClientId
let prod0ClientSecret = ChickenCheckConfiguration.config.Value.Prod.ClientSecret


//-----------------------------------------------------------------------------
// Helpers
//-----------------------------------------------------------------------------

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

let getReleaseVersionTags() =
    getGitTags()
    |> List.filter (fun t -> t.StartsWith releaseTagPrefix)
    |> List.map (fun t -> t.Substring(releaseTagPrefix.Length))

let getProdDeployVersionTags() =
    getGitTags()
    |> List.filter (fun t -> t.StartsWith prodDeployTagPrefix)
    |> List.map (fun t -> t.Substring(prodDeployTagPrefix.Length))

let getBuildVersion() =
    let storeVersion (v: SemVerInfo) =
        FakeVar.set "tagVersion" v
    let getStoredVersion() : SemVerInfo option =
        FakeVar.get<SemVerInfo> "tagVersion"

    match getStoredVersion() with
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

        let existingTags = getReleaseVersionTags()
        let version = getNextVersion existingTags (fullVersion semVersion)
        storeVersion version
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

//-----------------------------------------------------------------------------
// Build Target Implementations
//-----------------------------------------------------------------------------

let clean _ =
    [ "bin"; "temp"; outputDir ]
    |> Shell.cleanDirs

    !! srcGlob
    ++ testsGlob
    |> Seq.collect(fun p ->
        ["bin";"obj"]
        |> Seq.map(fun sp ->
            IO.Path.GetDirectoryName p @@ sp)
        )
    |> Shell.cleanDirs

    !! helmPackagesGlob
    |> Seq.iter Shell.rm

    [ "paket-files/paket.restore.cached" ]
    |> Seq.iter Shell.rm

let verifyCleanWorkingDirectory _ =
    if not (Git.Information.isCleanWorkingCopy "") then failwith "Working directory is not clean"
    let currentBranchName = Git.Information.getBranchName "" 
    if (currentBranchName <> "master") then failwithf "Can only release master, current branch is: %s" currentBranchName 

let dotnetRestore _ = DotNet.restore id sln

let runMigrations _ = Common.runMigrations migrationsPath connectionString

let writeVersionToFile  _ =
    let sb = System.Text.StringBuilder("module Version\n\n")
    Printf.bprintf sb "    let version = \"%s\"\n" (getBuildVersion())

    File.writeString false (serverPath @@ "Version.fs") (sb.ToString())

let installClient _ =
    printfn "Node version:"
    Common.node [ "--version" ] rootPath
    printfn "npm version:"
    Common.npm [ "--version" ] rootPath
    Common.npm [ "install" ] rootPath

let dotnetBuild ctx =
    let args =
        [
            sprintf "/p:PackageVersion=%s" (fullVersion semVersion).AsString
        ]
    DotNet.build(fun c ->
        { c with
            Configuration = Common.configuration (ctx.Context.AllExecutingTargets)
            NoRestore = true
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
        }) sln

let bundleClient _ =
    Common.npx [ "webpack"; "--config webpack.config.js"; "--mode=production" ] rootPath

let dotnetPublishServer ctx =
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
            OutputPath = Some (outputDir @@ "server")
        }) serverProj

let dotnetPublishMigrations ctx =
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
            OutputPath = Some (outputDir @@ "migrations")
        }) migrationsPath

let dockerBuild _ =
    let tag = getBuildVersion() + arm64ImageSuffix
    dockerBuildImage serverDockerFile appName tag
    dockerBuildImage migrationsDockerFile (appName + "-migrations") tag

let dockerPush _ =
    let tag = getBuildVersion() + arm64ImageSuffix
    dockerPushImage appName tag
    dockerPushImage (appName + "-migrations") tag

let runUnitTests ctx =
    let configuration = Common.configuration (ctx.Context.AllExecutingTargets)
    let args = sprintf "--configuration %s --no-restore --no-build --fail-on-focused-tests" (configuration.ToString())
    DotNet.exec (fun c ->
        { c with 
            WorkingDirectory = unitTestsPath }) "run" args
    |> (fun res -> if not res.OK then failwithf "RunUnitTests failed")

let runWebTests ctx =
    Target.activateBuildFailure "DockerCleanUp"
    let configuration = Common.configuration (ctx.Context.AllExecutingTargets)
    let args = sprintf "--configuration %s --no-restore --no-build -- https://dev.chickens.viktorvan.com" (configuration.ToString())
    DotNet.exec (fun c ->
        { c with WorkingDirectory = webTestsPath }) "run" args
    |> (fun res -> if not res.OK then failwithf "RunWebTests failed")

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

let watchApp _ =

    let server() =
        System.Threading.Thread.Sleep 15000
        Common.DotNetWatch "run" serverPath

    let bundleDevClient() =
        [ outputDir @@ "server/public" ]
        |> Shell.cleanDirs
        Common.npx [ "webpack"; "--config webpack.config.js"; "--mode=development"; "--watch" ] rootPath

    [ server; bundleDevClient ]
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

let gitTagRelease _ =
    let releaseTag (version: string) =
        match Git.Information.getBranchName "" with
        | "master" -> releaseTagPrefix + version
        | branch -> releaseTagPrefix + version + "-" + branch

    let version = getBuildVersion()
    releaseTag version |> Git.Branches.tag ""
    releaseTag version |> Git.Branches.pushTag "" "origin"

let gitTagDeployment prefix _ =
    let version = prefix + getBuildVersion()
    let existingTags = getProdDeployVersionTags()

    if not (List.contains version existingTags) then
        Git.Branches.tag "" version
    else
        Trace.tracefn "%s tag already exists for this version" prefix

let helmPackage _ =
    let version = getBuildVersion()
    let packageArgs = [
        "package"
        "--app-version"
        version
        "./helm"
    ]
    Common.kubectl [ "config"; "use-context"; "microk8s" ] ""
    Common.helm packageArgs ""

let helmInstallDev _ = 
    let deployArgs = [
        "upgrade"
        sprintf "%s-dev" appName
        "-f"
        "./helm/values.dev.yaml"
        "--set"
        sprintf "authentication.clientSecret=%s" ChickenCheckConfiguration.config.Value.Dev.ClientSecret
        "chickencheck-0.1.2.tgz"
    ]

    Common.kubectl [ "config"; "use-context"; "microk8s" ] ""
    Common.helm deployArgs rootPath

let helmInstallProd _ = 
    let deployArgs = [
        "upgrade"
        appName
        "-f"
        "./helm/values.prod.yaml"
        "--set"
        sprintf "authentication.clientSecret=%s" ChickenCheckConfiguration.config.Value.Prod.ClientSecret
        rootPath @@ sprintf "%s*.tgz" appName
    ]

    Common.kubectl [ "config"; "use-context"; "microk8s" ] ""
    Common.helm deployArgs ""

//-----------------------------------------------------------------------------
// Build Target Declaration
//-----------------------------------------------------------------------------

Target.create "Clean" clean
Target.create "DotnetRestore" dotnetRestore
Target.create "RunMigrations" runMigrations
Target.create "InstallClient" installClient
Target.create "WriteVersionToFile" writeVersionToFile
Target.create "DotnetBuild" dotnetBuild
Target.create "BundleClient" bundleClient
Target.create "RunUnitTests" runUnitTests
Target.create "WatchApp" watchApp
Target.create "WatchTests" watchTests
Target.create "Build" ignore
Target.create "DotnetPublishServer" dotnetPublishServer
Target.create "DotnetPublishMigrations" dotnetPublishMigrations
Target.create "Package" ignore
Target.create "DockerBuild" dockerBuild
Target.create "DockerPush" dockerPush
Target.create "HelmPackage" helmPackage
Target.create "HelmInstallDev" helmInstallDev
Target.create "GitTagDevDeployment" (gitTagDeployment "DEV-")
Target.create "RunWebTests" runWebTests
Target.create "VerifyCleanWorkingDirectory" verifyCleanWorkingDirectory
Target.create "GitTagRelease" gitTagRelease
Target.create "CreateRelease" ignore
Target.create "helmInstallProd" helmInstallProd
Target.create "GitTagProdDeployment" (gitTagDeployment "PROD-")

//-----------------------------------------------------------------------------
// Build Target Dependencies
//-----------------------------------------------------------------------------

// Only call Clean if 'Package' was in the call chain
// Ensure Clean is called before 'DotnetRestore' and 'InstallClient'
"Clean" ?=> "DotnetRestore"
"Clean" ?=> "InstallClient"
"Clean" ==> "Package"

"DotnetRestore" 
    ==> "RunMigrations" 
    ==> "DotNetBuild"

"WriteVersionToFile"
    ?=> "WatchApp"

"DotnetRestore" <=> "InstallClient"
    ==> "BundleClient"
    ==> "WriteVersionToFile"
    ==> "DotnetBuild"
    ==> "RunUnitTests"
    ==> "Build"
    ==> "DotnetPublishServer"
    ==> "DotnetPublishMigrations"
    ==> "Package"
    ==> "DockerBuild"
    ==> "DockerPush"
    ==> "HelmPackage"
    ==> "HelmInstallDev"
    ==> "GitTagDevDeployment"
    ==> "RunWebTests"
    ==> "GitTagRelease"
    ==> "HelmInstallProd"
    ==> "GitTagProdDeployment"
    ==> "CreateRelease"

"Clean" ?=> "VerifyCleanWorkingDirectory"
"VerifyCleanWorkingDirectory" ?=> "BundleClient"
"VerifyCleanWorkingDirectory" ==> "CreateRelease"

"DotnetRestore"
    ==> "WatchTests"

//-----------------------------------------------------------------------------
// Start
//-----------------------------------------------------------------------------

Target.runOrDefaultWithArguments "Build"
