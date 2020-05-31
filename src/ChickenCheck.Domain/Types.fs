namespace ChickenCheck.Domain

open System

// types

type AsyncResult<'TResult, 'TError> = Async<Result<'TResult, 'TError>> 
type LoginError =
    | UserDoesNotExist
    | PasswordIncorrect
type AuthenticationError =
    | UserTokenExpired
    | TokenInvalid of string
type Email = Email of string
type SecurityToken = SecurityToken of string
type SecureRequest<'T> = { Token: SecurityToken; Content: 'T }
type Password = Password of string
type PasswordHash = 
    { Hash: byte[]
      Salt: byte[] }
type UserId = UserId of Guid
type User =
    { Id : UserId
      Name : string
      Email : Email
      PasswordHash : PasswordHash }
type Session = 
    { Token : SecurityToken
      UserId : UserId 
      Name : string }
type ChickenId = ChickenId of Guid
type ImageUrl = ImageUrl of string
type Chicken = 
    { Id: ChickenId
      Name : string 
      ImageUrl : ImageUrl option 
      Breed : string }
type EggCount = EggCount of int
type Date = 
    { Year: int
      Month: int
      Day: int }

type ConnectionString = ConnectionString of string

type GetAllChickensResponse =
    { Chicken: Chicken
      OnDate: EggCount
      Total: EggCount }

type IChickenApi =
    { CreateSession: (Email * Password) -> AsyncResult<Session, LoginError> 
      GetAllChickensWithEggs: SecureRequest<Date> -> AsyncResult<GetAllChickensResponse list, AuthenticationError>
      GetEggCount: SecureRequest<Date * ChickenId list> -> AsyncResult<Map<ChickenId, EggCount>, AuthenticationError>
      AddEgg: SecureRequest<ChickenId * Date> -> AsyncResult<unit, AuthenticationError> 
      RemoveEgg: SecureRequest<ChickenId * Date> -> AsyncResult<unit, AuthenticationError> }
    
// types - implementation
        
module SecurityToken =
    let create str =
        if String.IsNullOrEmpty str then
            invalidArg "SecurityToken" "SecurityToken cannot be empty"
        else
            SecurityToken str
            
module Session =
    let create (user: User) token =
        { Token = token
          UserId = user.Id
          Name = user.Name }
    
// domain - implementation
[<AutoOpen>]
module Helpers =
    let notImplemented() = raise <| System.NotImplementedException "Not implemented"
    let tee f x =
        f x |> ignore
        x
        
module SecureRequest =
    let create token content = { Token = token; Content = content }
    
module Email =
    let create (str: string) =
        if System.String.IsNullOrWhiteSpace str then
            Error "cannot be empty"
        elif str.Contains("@") |> not then Error "invalid email"
        else str |> Email |> Ok
    let value (Email str) = str


module Password =
    let create str =
        if String.IsNullOrWhiteSpace str then Error "Cannot be empty"
        else str |> Password |> Ok

    let value (Password str) = str

module PasswordHash =   
    let toByteArray = Convert.FromBase64String
    let toBase64String = System.Convert.ToBase64String
    
module UserId =
    let create guid =
        if guid = Guid.Empty then ("UserId", "empty guid") ||> invalidArg |> raise
        else UserId guid
    let value (UserId guid) = guid
    
module ChickenId =
    let create guid =
        if guid = Guid.Empty then ("ChickenId", "empty guid") ||> invalidArg |> raise
        else ChickenId guid
        
    let parse = Guid.Parse >> create
        
    let value (ChickenId guid) = guid

module ImageUrl =
    let create (str: string) = 
        if String.IsNullOrWhiteSpace(str) then 
            Error "Not a valid url" 
        else 
            ImageUrl str |> Ok
    let value (ImageUrl str) = str

module EggCount =
    let zero = EggCount 0
    let create num =
        if num < 1 then zero else EggCount num

    let increase (EggCount num) =
        EggCount (num + 1)

    let decrease (EggCount num) =
        if num < 1 then zero
        else EggCount (num - 1)

    let value (EggCount num) = num

    let toString (EggCount num) = num.ToString()
    
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
type Email with
    member this.Val = Email.value this

type Password with
    member this.Val = Password.value this
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

