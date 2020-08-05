module ChickenCheck.WebTests.Configuration
open FsConfig
open Microsoft.Extensions.Configuration
open System.IO


type Config =
    { Username: string
      Password: string }

let configRoot =
    ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddUserSecrets("450c1a87-2f77-4e5b-b5d2-37975ef0123d")
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
