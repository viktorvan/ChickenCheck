namespace ChickenCheck.Domain

open System
open FsToolkit.ErrorHandling


type AsyncResult<'TResult, 'TError> = Async<Result<'TResult, 'TError>> 
type ValidationError = ValidationError of Param : string * Message : string
type DatabaseError = DatabaseError of Message : string
type LoginError =
    | UserDoesNotExist
    | PasswordIncorrect
type AuthenticationError =
    | UserTokenExpired
    | TokenInvalid of string
    | UserDoesNotHaveAccess
    | TokenGenerationFailed of string
type DomainError = 
    | Validation of ValidationError
    | Database of DatabaseError
    | Login of LoginError
    | Authentication of AuthenticationError
    | Duplicate
    | NotFound
    | ConfigMissing of Key: string
type NaturalNum = NaturalNum of int
type String200 = String200 of string
type String1000 = String1000 of string
type Email = Email of string
type SecurityToken = SecurityToken of String1000
type GenerateToken = string -> string -> Result<SecurityToken, AuthenticationError>
type SecureRequest<'T> = { Token: SecurityToken; Content: 'T }
type Password = Password of string
type PasswordHash = 
    { Hash: byte[]
      Salt: byte[] }
type UserId = UserId of Guid
type User = 
    { Id : UserId
      Name : String200
      Email : Email
      PasswordHash : PasswordHash }
type Session = 
    { Token : SecurityToken
      UserId : UserId; Name : String200 }
type ChickenId = ChickenId of Guid
type ImageUrl = ImageUrl of string
type Chicken = 
    { Id: ChickenId
      Name : String200 
      ImageUrl : ImageUrl option 
      Breed : String200 }
type EggCount = EggCount of NaturalNum
type Date = { _Year: int; _Month: int; _Day: int }

module Commands =
    type CreateSession =
        { Email: Email 
          Password: Password }
    type AddEgg =
        { ChickenId : ChickenId
          Date : Date }
    type RemoveEgg =
        { ChickenId : ChickenId
          Date : Date }
      
module Events =
    type EggAdded =
        { ChickenId : ChickenId
          Date: Date }
    type EggRemoved =
        { ChickenId : ChickenId
          Date: Date }
    type ChickenEvent =
        | EggAdded of EggAdded
        | EggRemoved of EggRemoved
    type DomainEvent =
        | ChickenEvent of ChickenEvent
    
type ConnectionString = ConnectionString of string
type AppendEvents = Events.DomainEvent list -> AsyncResult<unit list, DatabaseError>

module Store =
    module User =
        type GetUserByEmail = Email -> AsyncResult<User option, DatabaseError>
    module Chicken =
        type GetChickens = unit -> AsyncResult<Chicken list, DatabaseError>
        type GetTotalEggCount = unit -> AsyncResult<Map<ChickenId, EggCount>, DatabaseError>
        type GetEggCountOnDate = Date -> AsyncResult<Map<ChickenId, EggCount>, DatabaseError>
        type AddEgg = ChickenId * Date -> AsyncResult<unit, DatabaseError>
        type RemoveEgg = ChickenId * Date -> AsyncResult<unit, DatabaseError>
        type GetEggData = unit -> AsyncResult<Map<ChickenId, EggCount> * Map<Date, NaturalNum>, DatabaseError>
    
[<AutoOpen>]
module Helpers =
    let notImplemented() = raise <| System.NotImplementedException "Not implemented"
    let tee f x =
        f x |> ignore
        x
    let raiseOnValidationError createFunc =
        createFunc
        >> function
        | Ok v -> v
        | Error (ValidationError (param, msg)) -> invalidArg param msg

module NaturalNum =
    let create i =
        if i < 0 then ("Natural number", "must be >= 0") |> ValidationError |> Error
        else i |> NaturalNum |> Ok

    let parse (str: string) =
        match Int32.TryParse str with
        | (true, i) -> i |> create
        | (false, _) -> ("natural number", "input is not an int") |> ValidationError |> Error

    let createOrFail = raiseOnValidationError create

    let zero = createOrFail 0

    let map f (NaturalNum num) =
        f num
        |> create
        
    let value (NaturalNum i) = i
    
type NaturalNum with
   member this.Value = this |> NaturalNum.value 

module String200 = 
    let create (name: string) (str: string) =
        if System.String.IsNullOrWhiteSpace str then 
            (name, "cannot be empty") |> ValidationError |> Error
        elif str.Length > 200 then
            (name, "must be less than 200 characters") |> ValidationError |> Error
        else str |> String200 |> Ok
    let value (String200 str) = str
    let createOrFail name = raiseOnValidationError (create name) 

type String200 with
    member this.Value = String200.value this

module String1000 = 
    let create (name: string) (str: string) =
        if System.String.IsNullOrWhiteSpace str then 
            (name, "cannot be empty") |> ValidationError |> Error
        elif str.Length > 1000 then
            (name, "must be less than 1000 characters") |> ValidationError |> Error
        else str |> String1000 |> Ok
    let value (String1000 str) = str
type String1000 with
    member this.Value = String1000.value this
    
module Email =
    let create (str: string) =
        if System.String.IsNullOrWhiteSpace str then
            ("email", "cannot be empty") |> ValidationError |> Error
        elif str.Contains("@") |> not then ("email", "invalid email") |> ValidationError |> Error
        else str |> Email |> Ok
    let value (Email str) = str

type Email with
    member this.Value = Email.value this

module Password =
    let create str =
        if String.IsNullOrWhiteSpace str then ("Password", "Cannot be empty") |> ValidationError |> Error
        else str |> Password |> Ok

    let value (Password str) = str

type Password with
    member this.Value = Password.value this

module PasswordHash =   
    let toByteArray = Convert.FromBase64String
    let toBase64String = System.Convert.ToBase64String
    
module UserId =
    let create guid =
        if guid = Guid.Empty then ("UserId", "empty guid") |> ValidationError |> Error
        else UserId guid |> Ok
    let value (UserId guid) = guid
type UserId with
    member this.Value = UserId.value this
    
module ChickenId =
    let create guid =
        if guid = Guid.Empty then ("ChickenId", "empty guid") |> ValidationError |> Error
        else ChickenId guid |> Ok
    let value (ChickenId guid) = guid
type ChickenId with
    member this.Value = this |> ChickenId.value

module ImageUrl =
    let create (str: string) = 
        if String.IsNullOrWhiteSpace(str) then 
            ("ImageUrl", "Not a valid url") 
            |> ValidationError 
            |> Error
        else 
            ImageUrl str |> Ok
    let value (ImageUrl str) = str

type ImageUrl with
    member this.Value = ImageUrl.value this

module EggCount =
    let zero = NaturalNum.zero |> EggCount

    let increase (EggCount num) =
        num
        |> NaturalNum.map ((+) 1)
        |> Result.map EggCount

    let decrease (EggCount num) =
        if num.Value > 0 then 
            num |> NaturalNum.map (fun value -> value - 1) |> Result.map EggCount
        else 
            NaturalNum.zero |> EggCount |> Ok

    let value (EggCount (NaturalNum num)) = num

    let toString (EggCount num) = num.Value.ToString()
    
type EggCount with
    member this.Value = this |> EggCount.value
    member this.Increase() = this |> EggCount.increase
    member this.Decrease() = this |> EggCount.decrease

module Date =
    let create (date: DateTime) =
        { _Year = date.Year
          _Month = date.Month
          _Day = date.Day }

    let today = create DateTime.Today
    let toDateTime (date: Date) = DateTime(date._Year, date._Month, date._Day)

    let addDays numDays date =
        date
        |> toDateTime
        |> (fun dt -> dt.AddDays numDays)
        |> create

type Date with
    member this.ToDateTime() =
        Date.toDateTime this
        
    member this.Year = this._Year
    member this.Month = this._Month
    member this.Day = this._Day
    
