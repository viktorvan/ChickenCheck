#if !FAKE
#load "../../.fake/build.fsx/intellisense.fsx"
#endif

open System
open Fake.Core
open Fake.DotNet

let invokeAsync f = async { f () }

let isRelease (targets : Target list) =
    targets
    |> Seq.map(fun t -> t.Name)
    |> Seq.exists ((=)"CreateRelease")

let configuration (targets : Target list) =
    let defaultVal = if isRelease targets then "Release" else "Debug"
    match Environment.environVarOrDefault "CONFIGURATION" defaultVal with
    | "Debug" -> DotNet.BuildConfiguration.Debug
    | "Release" -> DotNet.BuildConfiguration.Release
    | config -> DotNet.BuildConfiguration.Custom config

let DotNetWatch watchCmd workingDir =
    DotNet.exec
        (fun p -> { p with WorkingDirectory = workingDir })
        (sprintf "watch %s" watchCmd)
        ""
    |> ignore

let runMigrations migrationsPath connectionString =
    let args = sprintf "--connectionstring \"%s\"" connectionString
    DotNet.exec (fun o -> { o with WorkingDirectory = migrationsPath }) "run" args
    |> (fun res -> if not res.OK then String.Join(", ", res.Errors) |> failwith)

module Tools =
    let platformTool tool winTool =
        let tool = if Environment.isUnix then tool else winTool
        match ProcessUtils.tryFindFileOnPath tool with
        | Some t -> t
        | _ ->
            let errorMsg =
                tool + " was not found in path. " +
                "Please install it and make sure it's available from your path. " +
                "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
            failwith errorMsg

    let nodeTool = platformTool "node" "node.exe"
    let npmTool = platformTool "npm" "npx.exe"
    let npxTool = platformTool "npx" "npx.exe"
    let docker = platformTool "docker" "docker.exe"
    let kubectl = platformTool "kubectl" "kubectl.exe"

    let runTool cmd args workingDir =
        let arguments = args |> Arguments.OfArgs
        Command.RawCommand (cmd, arguments)
        |> CreateProcess.fromCommand
        |> CreateProcess.withWorkingDirectory workingDir
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore

let node (args: string list) = Tools.runTool Tools.nodeTool args
let npm (args: string list) = Tools.runTool Tools.npmTool args
let npx (args: string list) = Tools.runTool Tools.npxTool args
let docker (args: string list) = Tools.runTool Tools.docker args
let kubectl (args: string list) = Tools.runTool Tools.kubectl args
