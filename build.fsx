#r "paket: groupref Build //"
#load ".fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "Facades/netstandard"
#r "netstandard"
#endif
#load "src/tools/common.fsx"
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
let dockerImageName = "chickencheck"
let dockerfile = "Dockerfile"
let dockerfileArm64 = "Dockerfile-arm64"
let arm64ImageSuffix = "-arm64"
let dockerWebTestContainerName = "chickencheckwebtest"
let srcGlob = src @@ "**/*.??proj"
let testsGlob = rootPath  @@ "test/**/*.??proj"
let changelog = Fake.Core.Changelog.load "CHANGELOG.md"
let semVersion = changelog.LatestEntry.SemVer
let webTestPort = 8087
let k8sDeployment = rootPath @@ "k8s" @@ "ChickenCheckApp.yaml"
let releaseTagPrefix = "Release-"

//-----------------------------------------------------------------------------
// Helpers
//-----------------------------------------------------------------------------


let getReleaseVersionTags() =
    let getGitTags() = 
        match Git.CommandHelper.runGitCommand "" "tag" with
        | false, _,_ -> 
            failwith "git error"
        | true, existingTags, _ -> 
            existingTags 

    getGitTags()
    |> List.filter (fun t -> t.StartsWith releaseTagPrefix)
    |> List.map (fun t -> t.Substring(releaseTagPrefix.Length))

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
                let isExistingTag (v:SemVerInfo) = existingVersionTags |> List.contains v.AsString
                nextVersions
                |> Seq.skipWhile isExistingTag
                |> Seq.head

            if validVersion.Build = bigint(0) then
                { validVersion with 
                    Build = bigint(1)
                    Original = None }
            else 
                validVersion

        let existingTags = getReleaseVersionTags()
        let version = getNextVersion existingTags semVersion
        storeVersion version
        version
    | Some v -> v

let getDeployVersion() =
    let pattern = sprintf "image: localhost:32000\/%s:(.*)%s" dockerImageName arm64ImageSuffix
    let regex = String.getRegEx pattern
    File.readAsString k8sDeployment
    |> regex.Match
    |> (fun m -> m.Groups.[1].Value)
    |> SemVer.parse

let dockerImageFullName version =
    sprintf "%s/%s:%s" dockerRegistry dockerImageName version

let dockerBuildImage dockerfile version =
    let fullName = dockerImageFullName version
    let args = [ "build"; "-t"; fullName; "-f"; dockerfile; "." ]
    Common.docker args ""

let dockerPushImage version =
    let fullName = dockerImageFullName version
    let args = [ "push"; fullName ]
    Common.docker args ""

let dockerRunWebTestContainer dbFile =
    let args = 
        [ "run"
          "-d"
          "-p"
          sprintf "%i:8085" webTestPort
          "--name"
          dockerWebTestContainerName
          "-e"
          "ChickenCheck_ConnectionString=Data Source=/var/lib/chickencheck/" + dbFile
          "-e"
          "ChickenCheck_PublicPath=server/public"
          "-v"
          rootPath + ":/var/lib/chickencheck"
          (getBuildVersion().AsString) |> dockerImageFullName]
    Common.docker (args) ""

let dockerCleanUp _ =
    try
        Common.docker [ "stop"; dockerWebTestContainerName ] ""
        Common.docker [ "rm"; dockerWebTestContainerName ] ""
    with
        | e -> Trace.tracef "Failed to stop running docker container: %s" e.Message

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

    [ "paket-files/paket.restore.cached" ]
    |> Seq.iter Shell.rm

let dotnetRestore _ = DotNet.restore id sln

let runMigrations _ = Common.runMigrations migrationsPath connectionString

let writeVersionToFile  _ =
    let sb = System.Text.StringBuilder("module Version\n\n")
    Printf.bprintf sb "    let version = \"%s\"\n" (getBuildVersion().AsString)

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
            sprintf "/p:PackageVersion=%s" semVersion.AsString
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
            sprintf "/p:PackageVersion=%s" semVersion.AsString
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
            OutputPath = Some (outputDir @@ "Server")
        }) serverProj

let dockerBuild _ =
    let tag = getBuildVersion().AsString
    dockerBuildImage dockerfile tag
    let tagArm64 = tag + arm64ImageSuffix
    dockerBuildImage dockerfileArm64 tagArm64

let dockerPush _ =
    let tag = getBuildVersion().AsString + "-arm64"
    dockerPushImage tag

let runUnitTests ctx =
    let configuration = Common.configuration (ctx.Context.AllExecutingTargets)
    let args = sprintf "--configuration %s --no-restore --no-build --fail-on-focused-tests" (configuration.ToString())
    DotNet.exec (fun c ->
        { c with 
            WorkingDirectory = unitTestsPath }) "run" args
    |> (fun res -> if not res.OK then failwithf "RunUnitTests failed")

let runWebTests ctx =
    Target.activateBuildFailure "DockerCleanUp"
    let dbFile = "webtest.db"
    let configuration = Common.configuration (ctx.Context.AllExecutingTargets)
    let args = sprintf "--configuration %s --no-restore --no-build -- %i" (configuration.ToString()) webTestPort
    try
        rootPath @@ dbFile 
        |> sprintf "Data Source=%s" 
        |> Common.runMigrations migrationsPath
        dockerRunWebTestContainer dbFile
        DotNet.exec (fun c ->
            { c with WorkingDirectory = webTestsPath }) "run" args
        |> (fun res -> if not res.OK then failwithf "RunWebTests failed")
        dockerCleanUp()
    finally
        File.delete dbFile   

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
        Common.npx [ "webpack"; "--config webpack.config.js"; "--mode=development" ] rootPath

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
    let releaseTag (version: SemVerInfo) =
        match Git.Information.getBranchName "" with
        | "master" -> releaseTagPrefix + version.AsString
        | branch -> releaseTagPrefix + version.AsString + "-" + branch

    let version = getBuildVersion()
    releaseTag version |> Git.Branches.tag ""
    releaseTag version |> Git.Branches.pushTag "" "origin"

let gitTagDeployment _ =
    let version = "PROD-" + getDeployVersion().AsString
    Git.Branches.tag "" version

let updateDeployVersion _ =
    let deployImageName = sprintf "localhost:32000/%s:%s%s" dockerImageName (getBuildVersion().AsString) arm64ImageSuffix
    Trace.tracefn "Updating kubernetes deployment file %s with new image version %s" k8sDeployment deployImageName
    let pattern = sprintf "image: localhost:32000\/%s:.*%s" dockerImageName arm64ImageSuffix
    File.readAsString k8sDeployment
    |> String.regex_replace pattern ("image: " + deployImageName)
    |> File.writeString false k8sDeployment

let deploy _ = 
    let latestReleaseBuild = getReleaseVersionTags() |> List.last |> SemVer.parse
    let deployVersion = getDeployVersion()

    let rec getUserConfirmation() =
        let prompt =
            if latestReleaseBuild > deployVersion then
                sprintf "There is a newer buildVersion (%s), are you sure you still want to deploy version %s (yes/no)?\n> " latestReleaseBuild.AsString deployVersion.AsString
            else 
                sprintf "Deploy version %s (yes/no)?\n> " deployVersion.AsString
        match UserInput.getUserInput prompt with
        | "yes" -> true
        | "no" -> false
        | _ -> getUserConfirmation()

    let args = [
        "apply"
        "-f"
        k8sDeployment
    ]
    if getUserConfirmation() then
        Common.kubectl args ""
    else 
        failwith "User aborted deployment"

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
Target.create "Package" ignore
Target.create "DockerBuild" dockerBuild
Target.create "RunWebTests" runWebTests
Target.create "DockerPush" dockerPush
Target.create "GitTagRelease" gitTagRelease
Target.create "CreateRelease" ignore
Target.create "UpdateDeployVersion" updateDeployVersion
Target.create "DeployToKubernetes" deploy
Target.create "GitTagDeployment" gitTagDeployment
Target.create "Deploy" ignore
Target.createBuildFailure "DockerCleanUp" dockerCleanUp

//-----------------------------------------------------------------------------
// Build Target Dependencies
//-----------------------------------------------------------------------------

// Only call Clean if 'Package' was in the call chain
// Ensure Clean is called before 'DotnetRestore' and 'InstallClient'
"Clean" ?=> "DotnetRestore"
"Clean" ?=> "InstallClient"
"Clean" ==> "Package"

"DotnetRestore" ==> "RunMigrations" ==> "DotNetBuild"

"DotnetRestore" <=> "InstallClient"
    ==> "BundleClient"
    ==> "WriteVersionToFile"
    ==> "DotnetBuild"
    ==> "RunUnitTests"
    ==> "Build"
    ==> "DotnetPublishServer"
    ==> "Package"
    ==> "DockerBuild"
    ==> "RunWebTests"
    ==> "DockerPush"
    ==> "GitTagRelease"
    ==> "UpdateDeployVersion"
    ==> "CreateRelease"

"DeployToKubernetes"
    ==> "GitTagDeployment"
    ==> "Deploy"

"DotnetRestore"
    ==> "WatchTests"

//-----------------------------------------------------------------------------
// Start
//-----------------------------------------------------------------------------

Target.runOrDefaultWithArguments "Build"
