open System
open System.Threading.Tasks
open Argu
open Microsoft.Azure.Storage
open Microsoft.Azure.Storage.Blob

open FSharp.Control.Tasks.V2.ContextInsensitive

type Filepath = Path of string
type AzureStoreConnString = ConnString of string
type UploadFile = AzureStoreConnString -> Filepath -> Task<unit>

type CLIArguments =
    | [<Mandatory>] DatabasePath of string
    | [<Mandatory>] AzureStorageConnectionString of string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | DatabasePath _ -> "the path to the sqlite database file"
            | AzureStorageConnectionString  _ -> "Azure storage connection string"

let parser = ArgumentParser.Create<CLIArguments>(programName = "ChickenCheck.DbBackup.exe")

let uploadFile : UploadFile =
    fun (ConnString conn) (Path filepath) ->
        let storageAccount = CloudStorageAccount.Parse(conn)   
        let blobClient = storageAccount.CreateCloudBlobClient()
        let suffix = DateTime.UtcNow.ToString("yyyyMMddHHmm")
        
        task {
            let container = blobClient.GetContainerReference("chickencheck-backups")
            let! _ = container.CreateIfNotExistsAsync() 
            let blockBlob = container.GetBlockBlobReference(sprintf "chickencheck_db_backup_%s.db" suffix)
            return! blockBlob.UploadFromFileAsync filepath
        }
    
[<EntryPoint>]
let main argv =
    let args = parser.Parse argv
    let databasePath = args.GetResult DatabasePath |> Path
    let storageConnString = args.GetResult AzureStorageConnectionString |> ConnString
    
    uploadFile storageConnString databasePath
    |> Async.AwaitTask
    |> Async.RunSynchronously

    0 // return an integer exit code
