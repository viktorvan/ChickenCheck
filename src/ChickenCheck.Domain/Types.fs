namespace ChickenCheck.Domain

open System

// types

type AsyncResult<'TResult, 'TError> = Async<Result<'TResult, 'TError>> 
type ValidationError = ValidationError of Param : string * Message : string
type LoginError =
    | UserDoesNotExist
    | PasswordIncorrect
type AuthenticationError =
    | UserTokenExpired
    | TokenInvalid of string
type DomainError = 
    | Validation of ValidationError
    | Authentication of AuthenticationError
    | Internal
type NaturalNum = NaturalNum of int
type String200 = String200 of string
type String1000 = String1000 of string
type Email = Email of string
type SecurityToken = SecurityToken of String1000
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
      UserId : UserId 
      Name : String200 }
type ChickenId = ChickenId of Guid
type ImageUrl = ImageUrl of string
type Chicken = 
    { Id: ChickenId
      Name : String200 
      ImageUrl : ImageUrl option 
      Breed : String200 }
type EggCount = EggCount of NaturalNum
type Date = 
    { Year: int
      Month: int
      Day: int }

[<RequireQualifiedAccess>]
module Commands =
    type AddEgg =
        { ChickenId : ChickenId
          Date : Date }
        static member Create(chickenId, date) = { ChickenId = chickenId; Date = date }

    type RemoveEgg =
        { ChickenId : ChickenId
          Date : Date }
        static member Create(chickenId, date) = { ChickenId = chickenId; Date = date }
        
      
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

type GetAllChickensResponse =
    { Chicken: Chicken
      OnDate: EggCount
      Total: EggCount }


type IChickenApi =
    { CreateSession: (Email * Password) -> AsyncResult<Session, LoginError> 
      GetAllChickensWithEggs: SecureRequest<Date> -> AsyncResult<GetAllChickensResponse list, AuthenticationError>
      GetEggCountOnDate: SecureRequest<Date> -> AsyncResult<Map<ChickenId, EggCount>, AuthenticationError>
      AddEgg: SecureRequest<ChickenId * Date> -> AsyncResult<unit, AuthenticationError> 
      RemoveEgg: SecureRequest<ChickenId * Date> -> AsyncResult<unit, AuthenticationError> }
    
// implementation
    
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
        if i < 0 then NaturalNum 0
        else NaturalNum i

    let parse (str: string) =
        match Int32.TryParse str with
        | (true, i) -> i |> create |> Ok
        | (false, _) -> ("natural number", "input is not an int") |> ValidationError |> Error

    let zero = create 0

    let map f (NaturalNum num) =
        f num
        |> create
        
    let value (NaturalNum i) = i

module String200 = 
    let create (name: string) (str: string) =
        if System.String.IsNullOrWhiteSpace str then 
            (name, "cannot be empty") |> ValidationError |> Error
        elif str.Length > 200 then
            (name, "must be less than 200 characters") |> ValidationError |> Error
        else str |> String200 |> Ok
    let value (String200 str) = str
    let createOrFail name = raiseOnValidationError (create name) 


module String1000 = 
    let create (name: string) (str: string) =
        if System.String.IsNullOrWhiteSpace str then 
            (name, "cannot be empty") |> ValidationError |> Error
        elif str.Length > 1000 then
            (name, "must be less than 1000 characters") |> ValidationError |> Error
        else str |> String1000 |> Ok
    let value (String1000 str) = str
    
module SecureRequest =
    let create token content = { Token = token; Content = content }
    
module Email =
    let create (str: string) =
        if System.String.IsNullOrWhiteSpace str then
            ("email", "cannot be empty") |> ValidationError |> Error
        elif str.Contains("@") |> not then ("email", "invalid email") |> ValidationError |> Error
        else str |> Email |> Ok
    let value (Email str) = str


module Password =
    let create str =
        if String.IsNullOrWhiteSpace str then ("Password", "Cannot be empty") |> ValidationError |> Error
        else str |> Password |> Ok

    let value (Password str) = str

module PasswordHash =   
    let toByteArray = Convert.FromBase64String
    let toBase64String = System.Convert.ToBase64String
    
module UserId =
    let create guid =
        if guid = Guid.Empty then ("UserId", "empty guid") |> ValidationError |> Error
        else UserId guid |> Ok
    let value (UserId guid) = guid
    
module ChickenId =
    let create guid =
        if guid = Guid.Empty then ("ChickenId", "empty guid") |> ValidationError |> Error
        else ChickenId guid |> Ok
    let value (ChickenId guid) = guid

module ImageUrl =
    let create (str: string) = 
        if String.IsNullOrWhiteSpace(str) then 
            ("ImageUrl", "Not a valid url") 
            |> ValidationError 
            |> Error
        else 
            ImageUrl str |> Ok
    let value (ImageUrl str) = str

module EggCount =
    let zero = NaturalNum.zero |> EggCount

    let increase (EggCount num) =
        num
        |> NaturalNum.map (fun value -> value + 1)
        |> EggCount

    let decrease (EggCount num) =
        num |> NaturalNum.map (fun value -> value - 1) |> EggCount

    let value (EggCount (NaturalNum num)) = num

    let toString (EggCount num) = 
        let value = NaturalNum.value num 
        value.ToString()
    
module Date =
    let create (date: DateTime) =
        { Year = date.Year
          Month = date.Month
          Day = date.Day }
          
    let today = create DateTime.Today
    
    let toDateTime { Year = year; Month = month; Day = day } = DateTime(year, month, day)

    let addDays numDays date =
        date
        |> toDateTime
        |> (fun dt -> dt.AddDays numDays)
        |> create

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

// Extensions
type NaturalNum with
   member this.Value = this |> NaturalNum.value 
type String200 with
    member this.Value = String200.value this

type String1000 with
    member this.Value = String1000.value this

type Email with
    member this.Value = Email.value this

type Password with
    member this.Value = Password.value this
type UserId with
    member this.Value = UserId.value this

type ChickenId with
    member this.Value = this |> ChickenId.value
type ImageUrl with
    member this.Value = ImageUrl.value this

type EggCount with
    member this.Value = this |> EggCount.value
    member this.Increase() = this |> EggCount.increase
    member this.Decrease() = this |> EggCount.decrease

type Date with
    member this.ToDateTime() = Date.toDateTime this

