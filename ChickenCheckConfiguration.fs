module ChickenCheckConfiguration

open FsConfig
open Microsoft.Extensions.Configuration
open System.IO

type Authentication =
    { Domain: string
      ClientId: string
      ClientSecret: string }
type Database =
    { User: string
      Password: string }
type Local = { Db: Database }
type Backup =
    { AzureStorageConnectionString: string}
type Config =
    { DataProtectionCertificatePassword: string
      Local: Local
      Dev: Authentication 
      DevDb: Database
      Prod: Authentication
      ProdDb: Database
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
