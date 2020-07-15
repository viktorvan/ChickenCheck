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
open Fake.BuildServer


//-----------------------------------------------------------------------------
// Metadata and Configuration
//-----------------------------------------------------------------------------

let sln = "ChickenCheck.sln"
let rootPath = __SOURCE_DIRECTORY__
let outputDir = rootPath @@ "output"
let outputDirArm64 = rootPath @@ "output-arm64"
let src = rootPath @@ "src"
let serverPath = src @@ "ChickenCheck.Backend"
let serverProj = serverPath @@ "ChickenCheck.Backend.fsproj"
let clientPath = src @@ "ChickenCheck.Client"
let migrationsPath = src @@ "ChickenCheck.Migrations"
let unitTestsPath = rootPath @@ "test" @@ "ChickenCheck.UnitTests"
let connectionString = sprintf "Data Source=%s/database-dev.db" serverPath
let dockerRegistry = "microk8s-1.local:32000"
let dockerImageName = "chickencheck"
let dockerfile = "Dockerfile"
let dockerfileArm64 = "Dockerfile-arm64"
let dockerWebTestContainerName = "chickencheckwebtest"

let srcCodeGlob =
    !! ( src  @@ "**/*.fs")
    ++ ( src  @@ "**/*.fsx")

let testsCodeGlob =
    !! (rootPath  @@ "test/**/*.fs")
    ++ (rootPath  @@ "test/**/*.fsx")

let srcGlob = src @@ "**/*.??proj"
let testsGlob = rootPath  @@ "test/**/*.??proj"

let changelog = Fake.Core.Changelog.load "CHANGELOG.md"
let semVersion = changelog.LatestEntry.SemVer
let fullVersion =
    if BuildServer.isLocalBuild then
        semVersion.AsString
    else
        sprintf "%s.%s" (semVersion.AsString) TeamFoundation.Environment.BuildId

//-----------------------------------------------------------------------------
// Helpers
//-----------------------------------------------------------------------------

let storeVersion (v: SemVerInfo) =
    FakeVar.set "tagVersion" v
let getStoredVersion() : SemVerInfo option =
    FakeVar.get<SemVerInfo> "tagVersion"

let getBuildVersion() =
    match getStoredVersion() with
    | None ->
        let getNextVersion' existingVersionTags version =
            let nextVersions = 
                let versionWithBuildNumber (i:int) = { version with Build = version.Build + bigint(i); Original = None}
                Seq.initInfinite versionWithBuildNumber
            let validVersion = 
                let isExistingTag (v:SemVerInfo) = existingVersionTags |> List.contains v.AsString
                nextVersions
                |> Seq.skipWhile isExistingTag
                |> Seq.head

            validVersion

        match Git.CommandHelper.runGitCommand "" "tag" with
        | false, _,_ -> failwith "git error"
        | true, existingTags, _ ->
            let version =getNextVersion' existingTags semVersion
            storeVersion version
            version
    | Some v -> v

let dockerImageFullName version =
    sprintf "%s/%s:%s" dockerRegistry dockerImageName version

let dockerBuildImage dockerfile version =
    let fullName = dockerImageFullName version
    let args = sprintf "build -t %s -f %s ." fullName dockerfile
    Common.docker args ""

let dockerPushImage version =
    let fullName = dockerImageFullName version
    let args = sprintf "push %s" fullName
    Common.docker args ""

let dockerRunWebTestContainer dbFile =
    let args = sprintf "run -d -p 8085:8085 --name %s -e ChickenCheck_ConnectionString=\"Data Source=/var/lib/chickencheck/webtest.db\" -e ChickenCheck_PublicPath=\"/server/public\" -v .:/var/lib/chickencheck %s" dockerWebTestContainerName (getBuildVersion().AsString |> dockerImageFullName)
    Common.docker (args) ""

let dockerCleanUp _ =
    try
        Common.docker ("stop " + dockerWebTestContainerName) ""
        Common.docker ("remove " + dockerWebTestContainerName) ""
    with
        | e -> Trace.tracef "Failed to stop running docker container: %s" e.Message

//-----------------------------------------------------------------------------
// Build Target Implementations
//-----------------------------------------------------------------------------

let clean _ =
    [ "bin"; "temp"; outputDir; outputDirArm64 ]
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

let installClient _ =
    printfn "Node version:"
    Common.node "--version" rootPath
    printfn "npm version:"
    Common.npm "--version" rootPath
    Common.npm "install" rootPath

let dotnetBuild ctx =
    let args =
        [
            sprintf "/p:PackageVersion=%s" semVersion.AsString
            "--no-restore"
        ]
    DotNet.build(fun c ->
        { c with
            Configuration = Common.configuration (ctx.Context.AllExecutingTargets)
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
        }) sln

let updateChangeLog  _ =
    let printEntry sb (entry: Changelog.ChangelogEntry) =
        Printf.bprintf sb
            "            \"%s - %s\"\n"
            entry.SemVer.AsString
            (entry.Date |> Option.map (fun d -> d.ToString("yyyy-MM-dd")) |> Option.defaultValue "XXXX")
    let sb = System.Text.StringBuilder("module ChangeLog\n\n")
    Printf.bprintf sb "    let version = \"%s\"\n" fullVersion
    Printf.bprintf sb "    let changelog =\n        [\n"
    changelog.Entries |> Seq.iter (printEntry sb)
    Printf.bprintf sb "        ]\n"

    File.writeString false (serverPath @@ "ChangeLog.fs") (sb.ToString())

let bundleClient _ =
    [ outputDir @@ "server/public"; outputDirArm64 @@ "server/public" ]
    |> Shell.cleanDirs
    Common.npx "webpack --config webpack.prod.js" rootPath

    Shell.copyDir (outputDirArm64 @@ "server/public") (outputDir @@ "server/public") (fun _ -> true)

let dotnetPublishServer ctx =
    let args =
        [
            sprintf "/p:PackageVersion=%s" semVersion.AsString
        ]
    DotNet.publish(fun c ->
        { c with
            Configuration = Common.configuration (ctx.Context.AllExecutingTargets)
            Runtime = Some "linux-musl-x64"
            SelfContained = Some false
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
            OutputPath = Some (outputDir @@ "Server")
        }) serverProj
    DotNet.publish(fun c ->
        { c with
            Configuration = Common.configuration (ctx.Context.AllExecutingTargets)
            Runtime = Some "linux-arm64"
            SelfContained = Some false
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
            OutputPath = Some (outputDirArm64 @@ "Server")
        }) serverProj

let dockerBuild _ =
    let tag = getBuildVersion().AsString
    // let tagArm64 = tag + "-arm64"
    dockerBuildImage dockerfile tag
    // dockerBuildImage dockerfile tagArm64

let dockerPush _ =
    let tag = getBuildVersion().AsString + "-arm64"
    dockerPushImage tag

let runUnitTests _ =
    let args =
        match BuildServer.isLocalBuild  with
        | true -> ""
        | false -> "--fail-on-focused-tests"
    DotNet.exec (fun c ->
        { c with WorkingDirectory = unitTestsPath }) "run" args
    |> (fun res -> if not res.OK then failwithf "RunUnitTests failed")

let runWebTests _ =
    Target.activateBuildFailure "DockerCleanUp"
    let dbFile = "webtest.db"
    try
        File.create dbFile
        dockerRunWebTestContainer dbFile
        DotNet.exec (fun c ->
            { c with WorkingDirectory = unitTestsPath }) "run" ""
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
        Common.npx "webpack --config webpack.dev.js" rootPath

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

let gitTagBuild _ =
    let tag (version: SemVerInfo) =
        match Git.Information.getBranchName "" with
        | "master" -> version.AsString
        | branch -> version.AsString + "-" + branch

    let version = getBuildVersion()
    tag version |> Git.Branches.tag ""
    tag version |> Git.Branches.pushTag "" "origin"

//-----------------------------------------------------------------------------
// Build Target Declaration
//-----------------------------------------------------------------------------

Target.create "Clean" clean
Target.create "DotnetRestore" dotnetRestore
Target.create "RunMigrations" runMigrations
Target.create "InstallClient" installClient
Target.create "DotnetBuild" dotnetBuild
Target.create "BundleClient" bundleClient
Target.create "UpdateChangeLog" updateChangeLog
Target.create "RunUnitTests" runUnitTests
Target.create "WatchApp" watchApp
Target.create "WatchTests" watchTests
Target.create "Build" ignore
Target.create "DotnetPublishServer" dotnetPublishServer
Target.create "Package" ignore
Target.create "DockerBuild" dockerBuild
Target.create "RunWebTests" runWebTests
Target.create "DockerPush" dockerPush
Target.create "GitTagBuild" gitTagBuild
Target.create "CreateRelease" ignore
Target.createBuildFailure "DockerCleanUp" dockerCleanUp

//-----------------------------------------------------------------------------
// Build Target Dependencies
//-----------------------------------------------------------------------------

// Only call Clean if 'Package' was in the call chain
// Ensure Clean is called before 'DotnetRestore' and 'InstallClient'
"Clean" ?=> "DotnetRestore"
"Clean" ?=> "InstallClient"
"Clean" ==> "Package"
"UpdateChangeLog" ==> "dotnetBuild"

"DotnetRestore" ==> "RunMigrations" ==> "DotNetBuild"

"DotnetRestore" <=> "InstallClient"
    ==> "BundleClient"
    ==> "DotnetBuild"
    ==> "RunUnitTests"
    ==> "Build"
    ==> "DotnetPublishServer"
    ==> "Package"
    ==> "DockerBuild"
    // ==> "DockerPush"
    ==> "GitTagBuild"
    ==> "CreateRelease"

"DotnetRestore"
    ==> "WatchTests"

//-----------------------------------------------------------------------------
// Start
//-----------------------------------------------------------------------------

Target.runOrDefaultWithArguments "Build"
