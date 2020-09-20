#if !FAKE
#load "../../.fake/build.fsx/intellisense.fsx"
#endif
open FsConfig
open Microsoft.Extensions.Configuration
open System.IO

type Authentication =
    { Domain: string
      ClientId: string
      ClientSecret: string 
      ApiPassword: string }
type Backup =
    { AzureStorageConnectionString: string}
type Config =
    { DataProtectionCertificatePassword: string
      Prod: Authentication
      Backup: Backup }

let configRoot =
    ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddUserSecrets("de84dfcb-ba79-4405-8811-8124e96a1b3b")
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
