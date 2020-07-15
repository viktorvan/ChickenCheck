module ChickenCheck.Backend.Configuration
open FsConfig
open Microsoft.Extensions.Configuration
open System.IO


//type Authentication =
//    { Domain: string
//      Audience: string }
type Config =
    { ConnectionString: string
      [<DefaultValue("8085")>]
      ServerPort: uint16
      [<DefaultValue("../../output/server/public")>]
      PublicPath: string }
//      Authentication: Authentication }

let configRoot =
    ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddUserSecrets("f1dcbf68-77d8-40ba-807d-b98d9be91d5e")
        .AddEnvironmentVariables("ChickenCheck_")
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
               ServerPort = config.ServerPort
               PublicPath = config.PublicPath |> Path.GetFullPath |}
//               Authentication = config.Authentication |}
        | Error msg ->
            match msg with
            | BadValue (name, msg) -> invalidArg name msg
            | NotFound name -> invalidArg name "Not found"
            | NotSupported name -> invalidArg name "Could not read config value"
