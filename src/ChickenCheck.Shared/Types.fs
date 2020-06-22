namespace ChickenCheck.Shared

open System

// types

type AsyncResult<'TResult, 'TError> = Async<Result<'TResult, 'TError>> 
type Email = Email of string
type Password = Password of string
type UserId = UserId of Guid
type ApiUser =
    { Id : UserId
      Name : string
      Email : Email }
type User =
    | Anonymous
    | ApiUser of ApiUser
type ChickenId = ChickenId of Guid
type ImageUrl = ImageUrl of string
type Chicken = 
    { Id: ChickenId
      Name : string 
      ImageUrl : ImageUrl option 
      Breed : string }
type EggCount = EggCount of int
type NotFutureDate = 
    { Year: int
      Month: int
      Day: int }
    with override this.ToString() = sprintf "%i-%02i-%02i" this.Year this.Month this.Day

type ConnectionString = ConnectionString of string

type ChickenWithEggCount =
    { Chicken: Chicken
      Count: NotFutureDate * EggCount
      TotalCount: EggCount }
    
type AuthenticationSettings =
    { Domain: string
      Audience: string }
    
type IChickensApi =
    { AddEgg: ChickenId * NotFutureDate -> Async<unit>
      RemoveEgg: ChickenId * NotFutureDate -> Async<unit> }
      
// types - implementation
        
// domain - implementation
[<AutoOpen>]
module Helpers =
    let notImplemented() = raise <| System.NotImplementedException "Not implemented"
    let tee f x =
        f x |> ignore
        x
        
    
module Email =
    let create (str: string) =
        if System.String.IsNullOrWhiteSpace str then
            Error "cannot be empty"
        elif str.Contains("@") |> not then Error "invalid email"
        else str |> Email |> Ok
    let value (Email str) = str

module UserId =
    let create guid =
        if guid = Guid.Empty then ("UserId", "empty guid") ||> invalidArg |> raise
        else UserId guid
    let parse = Guid.Parse >> create
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
        
module NotFutureDate =
    let create (date: DateTime) =
        let tomorrow = DateTime.UtcNow.AddDays(1.)
        if (date >= tomorrow) then 
            invalidArg "date" "cannot be in the future"
        else 
            { Year = date.Year
              Month = date.Month
              Day = date.Day }
            
    let tryParse dateStr =
        match DateTime.TryParse dateStr with
        | true, date -> Some date
        | false,_ -> None
        |> Option.map create
        
    let parse dateStr =
        dateStr
        |> tryParse
        |> function
            | Some d -> d
            | None -> invalidArg "date" "Invalid date"
        
    let toDateTime { Year = year; Month = month; Day = day } = DateTime(year, month, day)
    let today = create DateTime.Today
    let addDays days date =
        date
        |> toDateTime
        |> (fun dt -> dt.AddDays(days))
        |> create

// Extensions
type Email with
    member this.Val = Email.value this

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

type NotFutureDate with
    member this.ToDateTime() = NotFutureDate.toDateTime this

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName
    
module DataAttributes =
    let [<Literal>] EggIcon = "data-egg-icon"
    let [<Literal>] ChickenCard = "data-chicken-card"
    let [<Literal>] CurrentDate = "data-current-date"
