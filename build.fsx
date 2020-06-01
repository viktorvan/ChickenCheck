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
let src = rootPath @@ "src"
let serverPath = src @@ "ChickenCheck.Backend"
let serverProj = serverPath @@ "ChickenCheck.Backend.fsproj"
let clientPath = src @@ "ChickenCheck.Client"
let migrationsPath = src @@ "ChickenCheck.Migrations"
let unitTestsPath = rootPath @@ "test" @@ "ChickenCheck.UnitTests"
let connectionString = sprintf "Data Source=%s/database.db" serverPath

let srcCodeGlob =
    !! ( src  @@ "**/*.fs")
    ++ ( src  @@ "**/*.fsx")

let testsCodeGlob =
    !! (__SOURCE_DIRECTORY__  @@ "test/**/*.fs")
    ++ (__SOURCE_DIRECTORY__  @@ "test/**/*.fsx")

let srcGlob = src @@ "**/*.??proj"
let testsGlob = __SOURCE_DIRECTORY__  @@ "test/**/*.??proj"

let changelog = Fake.Core.Changelog.load "CHANGELOG.md"
let semVersion = changelog.LatestEntry.SemVer.AsString
let fullVersion =
    if BuildServer.isLocalBuild then
        semVersion
    else
        sprintf "%s.%s" semVersion TeamFoundation.Environment.BuildId

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

let runMigrations _ =
    if BuildServer.isLocalBuild then
        Common.runMigrations migrationsPath connectionString

let installClient _ =
    printfn "Node version:"
    Common.node "--version" __SOURCE_DIRECTORY__
    printfn "Yarn version:"
    Common.yarn "--version" __SOURCE_DIRECTORY__
    Common.yarn "install --frozen-lockfile" __SOURCE_DIRECTORY__

let dotnetBuild ctx =
    let args =
        [
            sprintf "/p:PackageVersion=%s" semVersion
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

    File.writeString false (clientPath @@ "ChangeLog.fs") (sb.ToString())

let buildClient _ =
    Common.yarn "webpack-cli -p" __SOURCE_DIRECTORY__

let dotnetPublishServer ctx =
    let args =
        [
            sprintf "/p:PackageVersion=%s" semVersion
            "--no-restore"
            "--no-build"
        ]
    DotNet.publish(fun c ->
        { c with
            Configuration = Common.configuration (ctx.Context.AllExecutingTargets)
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
            OutputPath = outputDir @@ "Server" |> Some
        }) serverProj

let runUnitTests _ =
    let args =
        match BuildServer.isLocalBuild  with
        | true -> ""
        | false -> "--fail-on-focused-tests"
    DotNet.exec (fun c ->
        { c with WorkingDirectory = unitTestsPath }) "run" args
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

let watchApp _ =

    let server() = Common.DotNetWatch "run" serverPath

    let client() = Common.yarn "webpack-dev-server --config webpack.config.js" clientPath

    let functions() = ()

    let browser() =
        let openBrowser url =
            //https://github.com/dotnet/corefx/issues/10361
            Command.ShellCommand url
            |> CreateProcess.fromCommand
            |> CreateProcess.ensureExitCodeWithMessage "opening browser failed"
            |> Proc.run
            |> ignore

        System.Threading.Thread.Sleep 15000
        openBrowser "http://localhost:8080"

    [ server; client; functions; browser ]
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
    if BuildServer.isTFBuild then
        let tag =
            match TeamFoundation.Environment.BuildSourceBranchName with
            | branch when branch = "master" -> sprintf "CI-%s" fullVersion
            | branch -> sprintf "CI-%s-%s" fullVersion branch

        Git.Branches.tag "" tag
        Git.Branches.pushTag "" "origin" tag

//-----------------------------------------------------------------------------
// Build Target Declaration
//-----------------------------------------------------------------------------

Target.create "Clean" clean
Target.create "DotnetRestore" dotnetRestore
Target.create "RunMigrations" runMigrations
Target.create "InstallClient" installClient
Target.create "DotnetBuild" dotnetBuild
Target.create "BuildClient" buildClient
Target.create "UpdateChangeLog" updateChangeLog
Target.create "RunUnitTests" runUnitTests
Target.create "WatchApp" watchApp
Target.create "WatchTests" watchTests
Target.create "Build" ignore
Target.create "DotnetPublishServer" dotnetPublishServer
Target.create "Package" ignore
Target.create "GitTagBuild" gitTagBuild
Target.create "CreateRelease" ignore

//-----------------------------------------------------------------------------
// Build Target Dependencies
//-----------------------------------------------------------------------------

// Only call Clean if 'Package' was in the call chain
// Ensure Clean is called before 'DotnetRestore' and 'InstallClient'
"Clean" ?=> "DotnetRestore"
"Clean" ?=> "InstallClient"
"Clean" ==> "Package"
"UpdateChangeLog" ==> "BuildClient"

"DotnetRestore" ==> "RunMigrations" ==> "DotNetBuild"

"DotnetRestore" <=> "InstallClient"
    ==> "DotnetBuild" ==> "BuildClient"
    ==> "RunUnitTests"
    ==> "Build"
    ==> "DotnetPublishServer"
    ==> "Package"
    ==> "GitTagBuild"
    ==> "CreateRelease"

"DotnetRestore"
    ==> "WatchTests"

//-----------------------------------------------------------------------------
// Start
//-----------------------------------------------------------------------------

Target.runOrDefaultWithArguments "Build"
