#if !FAKE
#load "../../.fake/build.fsx/intellisense.fsx"
#endif
open FsConfig
open Microsoft.Extensions.Configuration
open System.IO

type Authentication =
    { Domain: string
      ClientId: string
      ClientSecret: string }
type Config =
    { Authentication: Authentication }

let configRoot =
    ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddUserSecrets("f1dcbf68-77d8-40ba-807d-b98d9be91d5e")
        .Build()

let [<Literal>] ConfigErrorMsg =
    """


************************************************************
* Failed to read configuration variables.
* For local development you need to add local user-secrets,
************************************************************


"""

let config =
    lazy
        let appConfig = AppConfig(configRoot)

        match appConfig.Get<Config>() with
        | Ok config ->
            config
        | Error msg ->
            match msg with
            | BadValue (name, msg) -> invalidArg name msg
            | NotFound name -> invalidArg name "Not found"
            | NotSupported name -> invalidArg name "Could not read config value"
