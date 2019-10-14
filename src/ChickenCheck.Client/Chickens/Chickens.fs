module ChickenCheck.Client.Chickens

open Fable.React.Props
open Fable.React
open ChickenCheck.Domain
open ChickenCheck.Domain.Commands
open Elmish
open System
open Fulma
open Fable.FontAwesome
open Fulma.Elmish
open Router
open FsToolkit.ErrorHandling
open ChickenCheck.Client
open ChickenCheck.Client.ApiHelpers
open Fulma.Extensions.Wikiki

type Model =
    { Chickens : Chicken list
      TotalEggCount : Map<ChickenId, EggCount>
      EggCountOnDate : Map<ChickenId, EggCount>
      FetchChickensStatus : ApiCallStatus 
      FetchTotalEggCountStatus : ApiCallStatus 
      FetchEggCountOnDateStatus : ApiCallStatus 
      AddEggStatus : ApiCallStatus * ChickenId list
      RemoveEggStatus : ApiCallStatus * ChickenId list
      CurrentDate : Date }

let init session route =
    match route with
    | ChickenRoute.Chickens ->
        { Chickens = []
          TotalEggCount = Map.empty
          EggCountOnDate = Map.empty
          FetchChickensStatus = NotStarted 
          FetchTotalEggCountStatus = NotStarted 
          FetchEggCountOnDateStatus = NotStarted 
          AddEggStatus = NotStarted, []
          RemoveEggStatus = NotStarted, []
          CurrentDate = Date.today }, Cmd.none


type EggCountMap = Map<ChickenId, EggCount>

[<RequireQualifiedAccess>]
type Chickens =
    | Request
    | Result of Chicken list
    | Error of string
    | ClearError

[<RequireQualifiedAccess>]
type TotalCount =
    | Request
    | Result of EggCountMap
    | Error of string
    | ClearError

[<RequireQualifiedAccess>]
type EggCountOnDate =
    | Request of Date
    | Result of Date * EggCountMap
    | Error of string
    | ClearError

[<RequireQualifiedAccess>]
type AddEgg =
    | Request of ChickenId * Date
    | Result of ChickenId * Date
    | Error of ChickenId * string
    | ClearError

[<RequireQualifiedAccess>]
type RemoveEgg =
    | Request of ChickenId * Date
    | Result of ChickenId * Date
    | Error of ChickenId * string
    | ClearError

type Msg = 
    | Chickens of Chickens
    | TotalCount of TotalCount
    | EggCountOnDate of EggCountOnDate
    | AddEgg of AddEgg
    | RemoveEgg of RemoveEgg
    | ChangeDate of Date

let datePickerView date dispatch =
    let onDateSet date =
        ChangeDate date |> dispatch
    let onDateChange delta =    
        onDateSet (Date.addDays delta date)
    let previousDate _ = onDateChange -1.
    let nextDate _ = onDateChange 1.

    let parseDate (ev: Browser.Types.Event) =
        ev.Value
        |> DateTime.Parse
        |> Date.create

    Level.level [ Level.Level.IsMobile ]
        [ 
            Level.item []
                [ 
                    Button.a 
                        [ Button.IsLink; Button.OnClick previousDate ] 
                        [ Icon.icon [] [ Fa.i [ Fa.Size Fa.Fa2x; Fa.Solid.CaretLeft ] [] ] ] 
                ]
            Level.item []
                [ 
                    Field.div 
                        [ 
                            Field.Props [ Data ("display-mode", "inline") ] 
                        ]
                        [ 
                            Input.date
                                [ 
                                    Input.OnChange (parseDate >> onDateSet) 
                                    date.ToDateTime().ToString("yyyy-MM-dd") |> Input.Value
                                ] 
                        ] 
                ]
            Level.item []
              [ Button.a [ Button.IsLink; Button.OnClick nextDate ] [ Icon.icon [] [ Fa.i [ Fa.Size Fa.Fa2x; Fa.Solid.CaretRight ] [] ] ] ] ]

let private getCountStr chickenId (countMap: EggCountMap) =
    countMap
    |> Map.tryFind chickenId
    |> Option.map EggCount.toString
    |> Option.defaultValue "-"

let update (chickenCheckApi: IChickenCheckApi) apiToken msg (model: Model) =
    let handleChickens = function
        | Chickens.Request -> 
            { model with FetchChickensStatus = Running }, 
            (callSecureApi
                apiToken
                chickenCheckApi.GetChickens 
                () 
                (Chickens.Result >> Chickens) 
                (Chickens.Error >> Chickens))

        | Chickens.Result chickens -> 
            { model with FetchChickensStatus = ApiCallStatus.Completed
                         Chickens = chickens }, 
                         [ Cmd.ofMsg (TotalCount.Request |> TotalCount)
                           Cmd.ofMsg (EggCountOnDate.Request model.CurrentDate |> EggCountOnDate) ] 
                           |> Cmd.batch

        | Chickens.Error msg ->
            { model with FetchChickensStatus = Failed msg }, Cmd.none

        | Chickens.ClearError ->
            { model with FetchChickensStatus = NotStarted }, Cmd.none

    let handleEggCountOnDate = function
        | EggCountOnDate.Request date -> 
            { model with FetchEggCountOnDateStatus = Running }, 
                callSecureApi
                    apiToken
                    chickenCheckApi.GetEggCountOnDate 
                    date 
                    ((fun res -> EggCountOnDate.Result (date, res)) >> EggCountOnDate) 
                    (EggCountOnDate.Error >> EggCountOnDate)

        | EggCountOnDate.Result (date, countByChicken) -> 
            if model.CurrentDate = date then
                { model with FetchEggCountOnDateStatus = Completed
                             EggCountOnDate = countByChicken }, Cmd.none
            else
                model, Cmd.none

        | EggCountOnDate.Error msg -> 
            { model with FetchEggCountOnDateStatus = Failed msg },
                Cmd.none

        | EggCountOnDate.ClearError ->
            { model with FetchEggCountOnDateStatus = NotStarted }, Cmd.none

    let handleTotalCount = function
        | TotalCount.Request -> 
            { model with FetchTotalEggCountStatus = Running }, 
            callSecureApi
                apiToken
                chickenCheckApi.GetTotalEggCount
                ()
                (TotalCount.Result >> TotalCount)
                (TotalCount.Error >> TotalCount)

        | TotalCount.Result countByChicken -> 
            { model with FetchTotalEggCountStatus = Completed
                         TotalEggCount = countByChicken }, Cmd.none

        | TotalCount.Error msg -> 
            { model with FetchTotalEggCountStatus = Failed msg },
                Cmd.none

        | TotalCount.ClearError ->
            { model with FetchTotalEggCountStatus = NotStarted }, Cmd.none

    let removeRunningId (chickenId: ChickenId) ids =
        ids
        |> List.filter (fun i -> i <> chickenId)

    let handleAddEgg = function
        | AddEgg.Request (chickenId, date) -> 
            let (_, runningIds) = model.AddEggStatus
            { model with AddEggStatus = (Running, chickenId :: runningIds) }, 
            callSecureApi
                apiToken
                chickenCheckApi.AddEgg
                { AddEgg.ChickenId = chickenId; Date = date }
                ((fun _ -> AddEgg.Result (chickenId, date)) >> AddEgg) 
                (fun msg -> AddEgg.Error (chickenId, msg) |> AddEgg)

        | AddEgg.Result (chickenId, date) -> 
            let (_, runningIds) = model.AddEggStatus
            let model = { model with AddEggStatus = Completed, runningIds |> removeRunningId chickenId }
            let newTotal = 
                model.TotalEggCount 
                |> Map.tryFind chickenId 
                |> Option.defaultValue EggCount.zero
                |> EggCount.increase
            let newOnDate = 
                model.EggCountOnDate 
                |> Map.tryFind chickenId
                |> Option.defaultValue EggCount.zero
                |> (fun count ->
                        if date = model.CurrentDate then
                            count |> EggCount.increase
                        else 
                            count |> Ok
                        )
            match (newTotal, newOnDate) with
            | Ok newTotal, Ok newOnDate ->
                { model with TotalEggCount = model.TotalEggCount |> Map.add chickenId newTotal
                             EggCountOnDate = model.EggCountOnDate |> Map.add chickenId newOnDate }, Cmd.none
            | Result.Error _, _ | _, Result.Error _ -> 
                model, 
                AddEgg.Error (chickenId, "Could not add egg") 
                |> AddEgg 
                |> Cmd.ofMsg 

        | AddEgg.Error (chickenId, msg) -> 
            let (_, runningIds) = model.AddEggStatus
            { model with AddEggStatus = Failed msg, runningIds |> removeRunningId chickenId },
                Cmd.none

        | AddEgg.ClearError ->
            let (_, runningIds) = model.AddEggStatus
            { model with AddEggStatus = NotStarted, runningIds }, Cmd.none

    let handleRemoveEgg = function
        | RemoveEgg.Request (chickenId, date) -> 
            let (_, runningIds) = model.RemoveEggStatus
            { model with RemoveEggStatus = Running, chickenId :: runningIds }, 
            callSecureApi
                apiToken
                chickenCheckApi.RemoveEgg
                { RemoveEgg.ChickenId = chickenId; Date = date }
                ((fun _ -> RemoveEgg.Result (chickenId, date)) >> RemoveEgg) 
                (fun msg -> RemoveEgg.Error (chickenId, msg) |> RemoveEgg)

        | RemoveEgg.Result (chickenId, date) -> 
            let (_, runningIds) = model.RemoveEggStatus
            let model = { model with RemoveEggStatus = Completed, runningIds |> removeRunningId chickenId }
            let hasEggsToRemove = 
                model.EggCountOnDate 
                |> Map.tryFind chickenId   
                |> Option.map (fun (EggCount num) -> num.Value > 0)
                |> Option.defaultValue false
            if hasEggsToRemove then
                let newTotal = 
                    model.TotalEggCount 
                    |> Map.tryFind chickenId
                    |> Option.defaultValue EggCount.zero
                    |> EggCount.decrease
                     
                let newOnDate = 
                    if date = model.CurrentDate then
                        model.EggCountOnDate
                        |> Map.tryFind chickenId
                        |> Option.defaultValue EggCount.zero
                        |> EggCount.decrease
                    else 
                        model.EggCountOnDate
                        |> Map.tryFind chickenId
                        |> Option.defaultValue EggCount.zero
                        |> Ok
                match (newTotal, newOnDate) with
                | Ok newTotal, Ok newOnDate ->
                    { model with TotalEggCount = model.TotalEggCount |> Map.add chickenId newTotal
                                 EggCountOnDate = model.EggCountOnDate |> Map.add chickenId newOnDate }, Cmd.none
                | Result.Error _, _ | _, Result.Error _ -> 
                    model, RemoveEgg.Error (chickenId, "Could not add egg") |> RemoveEgg |> Cmd.ofMsg 
            else
                model, Cmd.none

        | RemoveEgg.Error (chickenId, msg) -> 
            let (_, runningIds) = model.AddEggStatus
            { model with RemoveEggStatus = Failed msg, runningIds },
                Cmd.none

        | RemoveEgg.ClearError ->
            let (_, runningIds) = model.AddEggStatus
            { model with RemoveEggStatus = NotStarted, runningIds }, Cmd.none

    match msg with
    | Chickens msg -> handleChickens msg
    | EggCountOnDate msg -> handleEggCountOnDate msg
    | TotalCount msg -> handleTotalCount msg
    | AddEgg msg -> handleAddEgg msg
    | RemoveEgg msg -> handleRemoveEgg msg
    | ChangeDate date -> 
        { model with CurrentDate = date }, EggCountOnDate.Request date |> EggCountOnDate |> Cmd.ofMsg

module internal ChickenList =

    let batchesOf size input = 
        // Inner function that does the actual work.
        // 'input' is the remaining part of the list, 'num' is the number of elements
        // in a current batch, which is stored in 'batch'. Finally, 'acc' is a list of
        // batches (in a reverse order)
        let rec loop input num batch acc =
            match input with
            | [] -> 
                // We've reached the end - add current batch to the list of all
                // batches if it is not empty and return batch (in the right order)
                if batch <> [] then (List.rev batch)::acc else acc
                |> List.rev
            | x::xs when num = size - 1 ->
                // We've reached the end of the batch - add the last element
                // and add batch to the list of batches.
                loop xs 0 [] ((List.rev (x::batch))::acc)
            | x::xs ->
                // Take one element from the input and add it to the current batch
                loop xs (num + 1) (x::batch) acc
        loop input 0 [] []

    let chickenTiles model onAddEgg onRemoveEgg =  
        match model.Chickens with
        | [] ->
            h6 [] [ str "Har du inga hönor?" ] |> List.singleton
        | chickens ->
            let chickenTile (chicken: Chicken) =
                let hasImage, imageUrl = 
                    match chicken.ImageUrl with
                    | Some (ImageUrl imageUrl) -> true, imageUrl
                    | None -> false, ""

                let removeEggButton = 
                    Button.a
                        [ 
                              Button.IsText
                              Button.IsHovered false
                              Button.Size Size.IsLarge
                              Button.OnClick (fun ev -> 
                                ev.cancelBubble <- true
                                ev.stopPropagation()
                                onRemoveEgg chicken.Id) 
                        ] 
                        [ 
                            Icon.icon 
                                [ 
                                    Icon.Modifiers [ Modifier.TextColor Color.IsWhite ] 
                                ] 
                                [ 
                                    Fa.i [ Fa.Size Fa.Fa3x; Fa.Solid.Egg ] [] 
                                ] 
                        ]

                let eggButtons =
                    let eggCount = 
                        model.EggCountOnDate 
                        |> Map.tryFind chicken.Id
                        |> Option.defaultValue EggCount.zero
                        |> EggCount.value

                    let isRunning = 
                        match model.FetchEggCountOnDateStatus, model.AddEggStatus, model.RemoveEggStatus with
                        | Running, _, _  -> true
                        | _, (Running, addIds), _ when addIds |> List.contains chicken.Id -> true
                        | _, _, (Running, removeIds) when removeIds |> List.contains chicken.Id -> true
                        | _ -> false
                    let currentEggs = 
                        if isRunning then [ ViewComponents.loading ]
                        else
                            [ for i in 1..eggCount do 
                                yield 
                                    Column.column 
                                        [ 
                                            Column.Width (Screen.All, Column.Is3) 
                                        ] 
                                        [ 
                                            removeEggButton 
                                        ] 
                            ]  
                    Columns.columns 
                        [ 
                            Columns.IsCentered
                            Columns.IsVCentered
                            Columns.IsMobile
                            Columns.Props [ Style [ Height 200 ] ] 
                        ] 
                        currentEggs

                let card =
                    let header =
                        span 
                            [ ] 
                            [ 
                                Heading.h4 
                                    [ Heading.Modifiers [ Modifier.TextColor Color.IsWhite ] ] 
                                    [ str chicken.Name.Value ]
                                Heading.h6 
                                    [ Heading.IsSubtitle;  Heading.Modifiers [ Modifier.TextColor Color.IsWhite ] ]
                                    [ str chicken.Breed.Value ] ]

                    Card.card 
                        [ 
                            Props 
                                [ 
                                    OnClick (fun _ -> onAddEgg chicken.Id)
                                    Style 
                                        [ 
                                            sprintf "linear-gradient(rgba(0,0,0,0.5), rgba(0,0,0,0)), url(%s)" imageUrl |> box |> BackgroundImage 
                                            BackgroundRepeat "no-repeat"
                                            BackgroundSize "cover" 
                                        ] 
                                ] 
                        ]
                        [ 
                            Card.header [] [ header ] 
                            Card.content [] [ eggButtons ] 
                        ]

                Column.column
                    [ 
                        Column.Width (Screen.Desktop, Column.Is4 ); Column.Width (Screen.Mobile, Column.Is12)
                    ]
                    [ card ]   

            chickens 
            |> batchesOf 3 
            |> List.map (fun batch -> Columns.columns [] (batch |> List.map chickenTile))

    let view model onAddEgg onRemoveEgg =
        chickenTiles model onAddEgg onRemoveEgg

module Statistics =
    let chickenEggCount (countMap: EggCountMap) (chicken: Chicken) =
        let totalCount = getCountStr chicken.Id countMap
        Level.item []
            [ div []
                [ Level.heading [] [ chicken.Name.Value |> str ] 
                  Level.title [] [ str totalCount ] ] ]

    let allCounts model =
        model.Chickens |> List.map (chickenEggCount model.TotalEggCount)
        |> Level.level []

    let view model =
        Container.container []
            [ Text.p 
                [ Modifiers 
                    [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered)
                      Modifier.TextSize (Screen.All, TextSize.Is2)] ] 
                [ str "Hur mycket har de värpt totalt?" ] 
              allCounts model ]

let view (model: Model) (dispatch: Msg -> unit) =

    if model.FetchChickensStatus = ApiCallStatus.NotStarted
        then Chickens.Request |> Chickens |> dispatch


    let onAddEgg chickenId = AddEgg.Request (chickenId, model.CurrentDate) |> AddEgg |> dispatch
    let onRemoveEgg chickenId = RemoveEgg.Request (chickenId, model.CurrentDate) |> RemoveEgg |> dispatch

    let clearAction msg = fun _ -> msg |> dispatch

    let header =
        Text.p 
            [ 
                Modifiers 
                    [ 
                        Modifier.TextAlignment (Screen.All, TextAlignment.Centered)
                        Modifier.TextSize (Screen.All, TextSize.Is2)
                    ] 
            ] 
            [ str "Vem värpte idag?" ]

    let hasError (item) =
        match item with | Failed _ -> true | _ -> false 

    let errorView items =
        
        let getErrorMsg item =
            match item with
            | Failed msg -> msg
            | _ -> ""

        let errorFor (item, clearMsg) = 
            if hasError item then
                item
                |> getErrorMsg
                |> ViewComponents.apiErrorMsg (fun _ -> dispatch clearMsg) 
                |> Some
            else None

        (items |> List.choose errorFor)
        


    let isLoaded = 
        match model.FetchChickensStatus with
        | (Completed) -> true
        | NotStarted | Running | Failed _ -> false

    let items = 
        [
            model.FetchChickensStatus, (Chickens.ClearError |> Chickens)
            model.FetchTotalEggCountStatus, (TotalCount.ClearError |> TotalCount)
            model.FetchEggCountOnDateStatus, (EggCountOnDate.ClearError |> EggCountOnDate)
            model.AddEggStatus |> fst, (AddEgg.ClearError |> AddEgg)
            model.RemoveEggStatus |> fst, (RemoveEgg.ClearError |> RemoveEgg)
        ]

    let hasErrors =
        items 
        |> List.exists (fun (item, _) -> hasError item)

                
    div []
        [
            yield PageLoader.pageLoader 
                [ 
                    // PageLoader.Props [ Style [ ZIndex 1 ] ]
                    PageLoader.Color IsInfo
                    PageLoader.IsActive (not isLoaded)  
                ] 
                [ ] 
            if hasErrors then yield Section.section [ ] ( errorView items )
            if isLoaded then
                yield 
                    Section.section 
                        [ ]
                        [ 
                            Section.section 
                                [ ]
                                [ 
                                    Container.container 
                                        [ ]
                                        [ 
                                            header
                                            datePickerView model.CurrentDate dispatch
                                            Container.container
                                                [ ] 
                                                (ChickenList.view model onAddEgg onRemoveEgg) ] ]
                            Section.section 
                                [ ]
                                [ Statistics.view model ] ]
        ]