namespace ChickenCheck.Client
open ChickenCheck.Domain


type StringInput<'a> =
    | Valid of 'a
    | Invalid of string
    | Empty
type NumberInput<'a> =
    | Valid of 'a
    | Invalid of string
    | Empty
type ApiCallStatus =
    | NotStarted
    | Running
    | Completed
    
type SessionModel =
    { Email : StringInput<Email>
      Password : StringInput<Password>
      LoginStatus : ApiCallStatus 
      Errors : string list }
type SessionMsg =
    | ChangeEmail of string
    | ChangePassword of string
    | LoginCompleted of Session
    | AddError of string
    | ClearErrors
    | Submit
      
type ChickenDetails =
    { Id: ChickenId
      Name : String200 
      ImageUrl : ImageUrl option 
      Breed : String200
      TotalEggCount : EggCount
      EggCountOnDate : EggCount }
type ChickensModel =
    { Chickens : Map<ChickenId, ChickenDetails>
      AddEggStatus : Map<ChickenId, ApiCallStatus>
      RemoveEggStatus : Map<ChickenId, ApiCallStatus>
      CurrentDate : Date
      Errors : string list }
type ChickenMsg =
    | FetchChickens 
    | FetchedChickens of Chicken list
    | FetchTotalCount 
    | FetchedTotalCount of Map<ChickenId, EggCount>
    | FetchEggCountOnDate of Date
    | FetchedEggCountOnDate of Date * Map<ChickenId, EggCount>
    | ChangeDate of Date
    | AddEgg of ChickenId * Date
    | AddedEgg of ChickenId * Date
    | AddEggFailed of ChickenId * string
    | RemoveEgg of ChickenId * Date
    | RemovedEgg of ChickenId * Date
    | RemoveEggFailed of ChickenId * string
    | AddError of string
    | ClearErrors 

[<RequireQualifiedAccess>]
type Page =
    | Signin of SessionModel
    | Chickens of ChickensModel
    | Loading
    | NotFound

type Model =
    { CurrentRoute: Router.Route option
      Session: Session option
      IsMenuExpanded: bool
      ActivePage: Page 
      ShowReleaseNotes: bool }
      
type Msg =
    | ToggleMenu
    | ToggleReleaseNotes
    | SessionMsg of SessionMsg
    | ChickenMsg of ChickenMsg
    | SignedIn of Session
    | Signout

[<AutoOpen>]
module DomainError =
    let getClientErrorMsg error =
        match error with
        | Validation _ -> "Ogiltigt värde"
        | Database _ | Duplicate | NotFound | ConfigMissing _ -> "Serverfel"
        | Authentication _ | Login _ -> "Autentiseringsfel"
    type DomainError with
        member this.ErrorMsg = getClientErrorMsg this
    
module StringInput = 
    let inline tryValid input =
        match input with
        | StringInput.Valid a ->
            let value = (^a : (member Value : string) a)
            true, value
        | StringInput.Invalid value -> 
            false, value
        | StringInput.Empty -> 
            false, ""

    let inline isValid input = input |> tryValid |> fst

module OptionalStringInput =
    let inline tryValid input = 
        match input with
        | StringInput.Valid a ->
            let value = (^a : (member Value : string) a)
            true, value
        | StringInput.Invalid value -> 
            false, value
        | StringInput.Empty -> 
            true, ""

    let inline isValid input = input |> tryValid |> fst

module NumberInput = 
    let inline tryValid input =
        match input with
        | NumberInput.Valid (num : NaturalNum) ->
            true, (num.Value |> string)
        | NumberInput.Invalid value -> 
            false, value
        | NumberInput.Empty -> 
            false, ""

    let inline isValid input = input |> tryValid |> fst

