module ChickenCheck.Backend.Configuration
open FsConfig
open Microsoft.Extensions.Configuration
open System
open System.IO


[<Convention("CHICKENCHECK")>]
type Config =
    { ConnectionString: string
      TokenSecret: string }

let configRoot =
    ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddUserSecrets("f1dcbf68-77d8-40ba-807d-b98d9be91d5e")
        .AddEnvironmentVariables()
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
            {| ConnectionString = Database.ConnectionString.create config.ConnectionString
               TokenSecret = config.TokenSecret |}
        | Error msg ->
            match msg with
            | BadValue (name, msg) -> invalidArg name msg
            | NotFound name -> invalidArg name "Not found"
            | NotSupported name -> invalidArg name "Could not read config value"
