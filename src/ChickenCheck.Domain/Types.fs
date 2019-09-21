namespace ChickenCheck.Domain

open System

type AsyncResult<'TResult, 'TError> = Async<Result<'TResult, 'TError>> 

type ValidationError = ValidationError of Param : string * Message : string
type DatabaseError = DatabaseError of Message : string
type LoginError =
    | UserDoesNotExist
    | PasswordIncorrect
type AuthenticationError =
    | UserTokenExpired
    | TokenInvalid
    | UserDoesNotHaveAccess
type DomainError = 
    | Validation of ValidationError
    | Database of DatabaseError
    | Login of LoginError
    | Authentication of AuthenticationError
    | Duplicate
    | NotFound
    | ConfigMissing of Key: string

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

type NaturalNum = NaturalNum of int

module NaturalNum =
    let create i =
        if i < 0 then ("Naturaln number", "must be >= 0") |> ValidationError |> Error
        else i |> NaturalNum |> Ok

    let parse (str: string) =
        match Int32.TryParse str with
        | (true, i) -> i |> create
        | (false, _) -> ("natural number", "input is not an int") |> ValidationError |> Error

    let createOrFail = raiseOnValidationError create
        
    let value (NaturalNum i) = i
    
type NaturalNum with
   member this.Value = this |> NaturalNum.value 

type String200 = String200 of string
module String200 = 
    let create (name: string) (str: string) =
        if System.String.IsNullOrWhiteSpace str then 
            (name, "cannot be empty") |> ValidationError |> Error
        elif str.Length > 200 then
            (name, "must be less than 200 characters") |> ValidationError |> Error
        else str |> String200 |> Ok
    let value (String200 str) = str
type String200 with
    member this.Value = String200.value this

type String1000 = String1000 of string
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

type Email = Email of string
module Email =
    let create (str: string) =
        if System.String.IsNullOrWhiteSpace str then
            ("email", "cannot be empty") |> ValidationError |> Error
        elif str.Contains("@") |> not then ("email", "invalid email") |> ValidationError |> Error
        else str |> Email |> Ok
    let value (Email str) = str

type Email with
    member this.Value = Email.value this

type SecurityToken = SecurityToken of string
type SecureRequest<'T> = { Token: SecurityToken; Content: 'T }
type SecureRequestBuilder(token) =
    member __.Build(content) = { Token = token; Content = content }

type Password = Password of string

module Password =
    let create str =
        if String.IsNullOrWhiteSpace str then ("Password", "Cannot be empty") |> ValidationError |> Error
        else str |> Password |> Ok

    let value (Password str) = str

type Password with
    member this.Value = Password.value this

type PasswordHash = {
    Hash: byte[]
    Salt: byte[]
}

module PasswordHash =   
    let toByteArray = Convert.FromBase64String
    let toBase64String = System.Convert.ToBase64String

type UserId = UserId of Guid
module UserId =
    let value (UserId guid) = guid
type UserId with
    member this.Value = UserId.value this
type User = { Id : UserId; Name : String200; Email : Email; PasswordHash : PasswordHash }

module Session = 
    type Session = { Token : SecurityToken; UserId : UserId; Name : String200 }

type ChickenId = ChickenId of Guid
module ChickenId =
    let create guid =
        if guid = Guid.Empty then ("ChickenId", "empty guid") |> ValidationError |> Error
        else ChickenId guid |> Ok
    let value (ChickenId guid) = guid
type ChickenId with
    member this.Value = this |> ChickenId.value

type ImageUrl = ImageUrl of string
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

type Chicken = 
    { Id: ChickenId
      Name : String200 
      ImageUrl : ImageUrl option 
      Breed : String200 }

type Date = { Year: int; Month: int; Day: int }
module Date =
    let create (date: DateTime) =
        { Year = date.Year
          Month = date.Month
          Day = date.Day }

    let toDateTime (date: Date) = DateTime(date.Year, date.Month, date.Day)
