module ChickenCheck.Backend.Configuration
open FsConfig
open Microsoft.Extensions.Configuration
open System.IO


type Authentication =
    { [<DefaultValue("ApiUser")>]
      ApiUsername: string
      [<DefaultValue("secret")>]
      ApiPassword: string }
type DataProtection =
    { [<DefaultValue("../../dataprotection/keys")>]
      Path: string
      [<DefaultValue("../../dataprotection/chickencheck.pfx")>]
      Certificate: string
      CertificatePassword: string }
type Config =
    { ConnectionString: string
      [<DefaultValue("8085")>]
      ServerPort: uint16
      [<DefaultValue("../../output/server/public")>]
      PublicPath: string
      DataProtection: DataProtection
      Authentication: Authentication
      [<DefaultValue("localhost")>]
      Domain: string
      [<DefaultValue("https://localhost:8085/")>]
      BasePath: string
      [<DefaultValue("https")>]
      RequestScheme: string }

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
               PublicPath = config.PublicPath |> Path.GetFullPath
               DataProtection = 
                   {| Path = DirectoryInfo config.DataProtection.Path
                      Certificate = config.DataProtection.Certificate
                      CertificatePassword = config.DataProtection.CertificatePassword |}
               Authentication = config.Authentication
               Domain = config.Domain
               BasePath = config.BasePath
               RequestScheme = config.RequestScheme |}
        | Error msg ->
            match msg with
            | BadValue (name, msg) -> invalidArg name msg
            | NotFound name -> invalidArg name "Not found"
            | NotSupported name -> invalidArg name "Could not read config value"
