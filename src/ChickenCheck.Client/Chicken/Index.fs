module ChickenCheck.Client.Chicken.Index

open Fable.React.Props
open Fable.React
open Fable.Core.JsInterop
open ChickenCheck.Client
open ChickenCheck.Domain
open ChickenCheck.Domain.Commands
open Elmish
open System
open Fulma
open Fable.FontAwesome
open Fulma.Elmish

type EggCount = Map<ChickenId, NaturalNum>

type Chickens =
    | Request
    | Result of Chicken list
    | Error of string
    | ClearError

type TotalCount =
    | Request
    | Result of EggCount
    | Error of string
    | ClearError

type EggCountOnDate =
    | Request of Date
    | Result of Date * EggCount
    | Error of string
    | ClearError

type AddEgg =
    | Request of ChickenId * Date
    | Result of ChickenId * Date
    | Error of string
    | ClearError

type RemoveEgg =
    | Request of ChickenId * Date
    | Result of ChickenId * Date
    | Error of string
    | ClearError

type Msg = 
    | Chickens of Chickens
    | TotalCount of TotalCount
    | EggCountOnDate of EggCountOnDate
    | AddEgg of AddEgg
    | RemoveEgg of RemoveEgg
    | DatePicker of Calendar.Msg

let updateDatePicker msg model =
    match msg with

    | Calendar.DatePickerChanged (newState, date) ->
        // Store the new state and the selected date
        { model with Calendar.DatePickerState = newState
                     Calendar.CurrentDate = date }

// Configuration passed to the components
let pickerConfig : DatePicker.Types.Config<Calendar.Msg> =
    let cfg = DatePicker.Types.defaultConfig Calendar.DatePickerChanged
    { cfg with DatePickerStyle = 
                [ Position PositionOptions.Absolute
                  MaxWidth "450px"
                  ZIndex 10. ] }

let datePickerView (model: Calendar.Model) dispatch =
    let onDateChange delta =    
        match model.CurrentDate with
        | None -> ()
        | Some date ->
            Calendar.DatePickerChanged (model.DatePickerState, date.AddDays delta |> Some) |> dispatch
    let previousDate _ = onDateChange -1.
    let nextDate _ = onDateChange 1.
    Level.level []
        [ Level.item []
            [ Button.a [ Button.IsLink; Button.OnClick previousDate ] [ Icon.icon [] [ Fa.i [ Fa.Size Fa.Fa2x; Fa.Solid.CaretLeft ] [] ] ] ]
          Level.item []
            [ Field.div [ Field.Props [ Data ("display-mode", "inline") ] ]
                [ DatePicker.View.root pickerConfig model.DatePickerState model.CurrentDate dispatch ] ]
          Level.item []
            [ Button.a [ Button.IsLink; Button.OnClick nextDate ] [ Icon.icon [] [ Fa.i [ Fa.Size Fa.Fa2x; Fa.Solid.CaretRight ] [] ] ] ] ]

let private getCount chickenId (countMap:EggCount option) = 
    countMap
    |> Option.map (fun c -> c.[chickenId].Value) 

let private getCountStr chickenId (countMap: EggCount option) =
    countMap
    |> getCount chickenId
    |> Option.map string
    |> Option.defaultValue "-"


let update (chickenCheckApi: IChickenCheckApi) (requestBuilder: SecureRequestBuilder) msg (model: ChickenIndexModel) =
    let callApi apiFunc arg successMsg errorMsg =
        let ofSuccess = function
            | Ok res -> res |> successMsg
            | Result.Error (err:DomainError) -> err.ErrorMsg |> errorMsg
        let ofError _ = "Serverfel" |> errorMsg
        Cmd.OfAsync.either apiFunc (requestBuilder.Build arg) ofSuccess ofError

    let handleChickens = function
        | Chickens.Request -> 
            { model with FetchChickensStatus = Running }, 
            (callApi
                chickenCheckApi.GetChickens 
                () 
                (Chickens.Result >> Chickens) 
                (Chickens.Error >> Chickens))

        | Chickens.Result chickens -> 
            match model.Calendar.CurrentDate with
            | Some date ->
                { model with FetchChickensStatus = ApiCallStatus.Completed
                             Chickens = chickens }, 
                             [ Cmd.ofMsg (TotalCount.Request |> TotalCount)
                               Cmd.ofMsg (EggCountOnDate.Request (Date.create date) |> EggCountOnDate) ] 
                               |> Cmd.batch
            | None ->
                model, Cmd.none

        | Chickens.Error msg ->
            { model with FetchChickensStatus = Failed msg }, Cmd.none

        | Chickens.ClearError ->
            { model with FetchChickensStatus = NotStarted }, Cmd.none

    let handleEggCountOnDate = function
        | EggCountOnDate.Request date -> 
            { model with FetchEggCountOnDateStatus = Running }, 
                callApi
                    chickenCheckApi.GetEggCountOnDate 
                    date 
                    ((fun res -> EggCountOnDate.Result (date, res)) >> EggCountOnDate) 
                    (EggCountOnDate.Error >> EggCountOnDate)

        | EggCountOnDate.Result (date, countByChicken) -> 
            match model.Calendar.CurrentDate with
            | Some currentDate when Date.create currentDate = date ->
                { model with FetchEggCountOnDateStatus = Completed
                             EggCountOnDate = Some countByChicken }, Cmd.none
            | _ ->
                model, Cmd.none

        | EggCountOnDate.Error msg -> 
            { model with FetchEggCountOnDateStatus = Failed msg }, Cmd.none

        | EggCountOnDate.ClearError ->
            { model with FetchEggCountOnDateStatus = NotStarted }, Cmd.none

    let handleTotalCount = function
        | TotalCount.Request -> 
            { model with FetchTotalEggCountStatus = Running }, 
            callApi
                chickenCheckApi.GetTotalEggCount
                ()
                (TotalCount.Result >> TotalCount)
                (TotalCount.Error >> TotalCount)

        | TotalCount.Result countByChicken -> 
            { model with FetchTotalEggCountStatus = Completed
                         TotalEggCount = Some countByChicken }, Cmd.none

        | TotalCount.Error msg -> 
            { model with FetchTotalEggCountStatus = Failed msg }, Cmd.none

        | TotalCount.ClearError ->
            { model with FetchTotalEggCountStatus = NotStarted }, Cmd.none

    let handleAddEgg = function
        | AddEgg.Request (chickenId, date) -> 
            { model with AddEggStatus = Running }, 
            callApi
                chickenCheckApi.AddEgg
                { AddEgg.ChickenId = chickenId; Date = date }
                ((fun _ -> AddEgg.Result (chickenId, date)) >> AddEgg) 
                (AddEgg.Error >> AddEgg)

        | AddEgg.Result (chickenId, date) -> 
            match model.TotalEggCount, model.EggCountOnDate, model.Calendar.CurrentDate with
            | None, _, _ | _, None, _ | _, _, None -> model, AddEgg.Error "Could not add egg" |> AddEgg |> Cmd.ofMsg
            | Some totalCount, Some onDateCount, Some currentDate ->
                let newTotal = totalCount.[chickenId].Value + 1 |> NaturalNum.create
                let newOnDate = 
                    if date = Date.create currentDate then
                        onDateCount.[chickenId].Value + 1 |> NaturalNum.create
                    else onDateCount.[chickenId] |> Ok
                match (newTotal, newOnDate) with
                | Ok newTotal, Ok newOnDate ->
                    { model with AddEggStatus = Completed
                                 TotalEggCount = model.TotalEggCount |> Option.map (fun total -> total |> Map.add chickenId newTotal)
                                 EggCountOnDate = model.EggCountOnDate |> Option.map (fun onDate -> onDate |> Map.add chickenId newOnDate) }, Cmd.none
                | Result.Error _, _ | _, Result.Error _ -> model,AddEgg.Error "Could not add egg" |> AddEgg |> Cmd.ofMsg 

        | AddEgg.Error msg -> 
            { model with AddEggStatus = Failed msg }, Cmd.none

        | AddEgg.ClearError ->
            { model with AddEggStatus = NotStarted }, Cmd.none

    let handleRemoveEgg = function
        | RemoveEgg.Request (chickenId, date) -> 
            { model with RemoveEggStatus = Running }, 
            callApi
                chickenCheckApi.RemoveEgg
                { RemoveEgg.ChickenId = chickenId; Date = date }
                ((fun _ -> RemoveEgg.Result (chickenId, date)) >> RemoveEgg) 
                (RemoveEgg.Error >> RemoveEgg)

        | RemoveEgg.Result (chickenId, date) -> 
            match model.TotalEggCount, model.EggCountOnDate, model.Calendar.CurrentDate with
            | None, _, _ | _, None, _ | _, _, None -> model, RemoveEgg.Error "Could not remove egg" |> RemoveEgg |> Cmd.ofMsg
            | Some totalCount, Some onDateCount, Some currentDate when onDateCount.[chickenId].Value > 0 ->
                let newTotal = 
                    let current = totalCount.[chickenId].Value
                    if current < 1 then 0 |> NaturalNum.create
                    else current - 1 |> NaturalNum.create
                let newOnDate = 
                    if date = Date.create currentDate && onDateCount.[chickenId].Value > 0 then
                        onDateCount.[chickenId].Value - 1 |> NaturalNum.create
                    else onDateCount.[chickenId] |> Ok
                match (newTotal, newOnDate) with
                | Ok newTotal, Ok newOnDate ->
                    { model with RemoveEggStatus = Completed
                                 TotalEggCount = model.TotalEggCount |> Option.map (fun total -> total |> Map.add chickenId newTotal)
                                 EggCountOnDate = model.EggCountOnDate |> Option.map (fun onDate -> onDate |> Map.add chickenId newOnDate) }, Cmd.none
                | Result.Error _, _ | _, Result.Error _ -> model, RemoveEgg.Error "Could not add egg" |> RemoveEgg |> Cmd.ofMsg 
            | Some _, Some _, _ -> { model with RemoveEggStatus = Completed }, Cmd.none

        | RemoveEgg.Error msg -> 
            { model with RemoveEggStatus = Failed msg }, Cmd.none

        | RemoveEgg.ClearError ->
            { model with RemoveEggStatus = NotStarted }, Cmd.none

    match msg with
    | Chickens msg -> handleChickens msg
    | EggCountOnDate msg -> handleEggCountOnDate msg
    | TotalCount msg -> handleTotalCount msg
    | AddEgg msg -> handleAddEgg msg
    | RemoveEgg msg -> handleRemoveEgg msg
    | DatePicker msg -> 
        let calendarModel = updateDatePicker msg model.Calendar
        let date = 
            match msg with
            | Calendar.DatePickerChanged (_, dateOption) -> dateOption.Value |> Date.create

        { model with Calendar = calendarModel }, EggCountOnDate.Request date |> EggCountOnDate |> Cmd.ofMsg

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

                let isEggButtonDisabled = 
                    match model.AddEggStatus, model.RemoveEggStatus with 
                    | (Running,_) | (_, Running) -> true 
                    | _ -> false

                let removeEggButton = 
                    Button.a
                        [ Button.IsText
                          Button.IsHovered false
                          Button.Size Size.IsLarge
                          Button.OnClick (fun _ -> onRemoveEgg chicken.Id) 
                          Button.Disabled isEggButtonDisabled ] 
                        [ Icon.icon [ Icon.Modifiers [ Modifier.TextColor Color.IsWhite ] ] [ Fa.i [ Fa.Size Fa.Fa3x; Fa.Solid.Egg ] [] ] ]
                let addEggButton = 
                    Button.a
                        [ Button.IsOutlined
                          Button.Size Size.IsLarge
                        //   (match model.AddEggStatus with | Running -> true | _ -> false) |> Button.IsLoading
                          Button.OnClick (fun _ -> onAddEgg chicken.Id) 
                          Button.Disabled isEggButtonDisabled ] 
                        [ Icon.icon [ Icon.Modifiers [ Modifier.TextColor (Color.IsInfo) ] ] [ Fa.i [ Fa.Size Fa.Fa2x; Fa.Solid.Plus ] [] ] ]

                let eggButtons =
                    let eggCount = 
                        model.EggCountOnDate 
                        |> Option.map (fun c -> c.[chicken.Id].Value) 
                        |> Option.defaultValue 0
                    let currentEggs = [ for i in 1..eggCount do yield Column.column [ Column.Width (Screen.All, Column.Is3) ] [ removeEggButton ] ]  
                    Column.column [ Column.Width (Screen.All, Column.Is3) ] [ addEggButton ] :: currentEggs |> List.rev
                    |> Columns.columns 
                        [ Columns.IsCentered
                          Columns.IsVCentered
                          Columns.Props [ Style [ Height 200 ] ] ] 

                let card =
                    let header =
                        span [] 
                            [ Heading.h4 [ Heading.Modifiers [ Modifier.TextColor Color.IsWhite ] ] [ str chicken.Name.Value ]
                              Heading.h6 
                                  [ Heading.IsSubtitle;  Heading.Modifiers [ Modifier.TextColor Color.IsWhite ] ]
                                  [ str chicken.Breed.Value ] ]

                    let footerSubtitle =
                        [ getCountStr chicken.Id model.TotalEggCount |> sprintf "Totalt: %s" |> str ] 
                            |> Heading.h6 [ Heading.IsSubtitle; Heading.Modifiers [ Modifier.TextColor Color.IsWhite ] ] 

                    Card.card 
                        [ Props 
                            [ Style 
                            [ sprintf "linear-gradient(rgba(0,0,0,0.5), rgba(0,0,0,0)), url(%s)" imageUrl |> box |> BackgroundImage 
                              BackgroundRepeat "no-repeat"
                              BackgroundSize "cover" ] ] ]
                        [ Card.header [] [ header ] 
                          Card.content [] [ eggButtons ] ]

                Column.column
                    [ Column.Width (Screen.Desktop, Column.Is4 ); Column.Width (Screen.Mobile, Column.Is12)]
                    [ card ]   

            chickens 
            |> batchesOf 3 
            |> List.map (fun batch -> Columns.columns [] (batch |> List.map chickenTile))

    let view model onAddEgg onRemoveEgg =
        let fetchStatus = (model.FetchChickensStatus, model.FetchTotalEggCountStatus, model.FetchEggCountOnDateStatus) 

        match fetchStatus with
        | (Completed, _, _) -> chickenTiles model onAddEgg onRemoveEgg
        | _ -> [ ViewComponents.loading ]

module Statistics =
    let chickenEggCount (countMap: EggCount option) (chicken: Chicken) =
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


let view (model: ChickenIndexModel) (dispatch: Msg -> unit) =

    if model.FetchChickensStatus = ApiCallStatus.NotStarted
        then Chickens.Request |> Chickens |> dispatch

    let onChangeDate  = 
        DatePicker >> dispatch

    let onAddEgg chickenId = AddEgg.Request (chickenId, (model.Calendar.CurrentDate.Value |> Date.create)) |> AddEgg |> dispatch
    let onRemoveEgg chickenId = RemoveEgg.Request (chickenId, (model.Calendar.CurrentDate.Value |> Date.create)) |> RemoveEgg |> dispatch
    let content = 
        Section.section []
            [ Section.section []
                [ Container.container []
                    [ Text.p 
                        [ Modifiers 
                            [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered)
                              Modifier.TextSize (Screen.All, TextSize.Is2)] ] 
                        [ str "Vem värpte idag?" ]
                      datePickerView model.Calendar onChangeDate
                      Container.container
                          [] 
                          (ChickenList.view 
                              model
                              onAddEgg 
                              onRemoveEgg) ] ]
              Section.section []
                [ Statistics.view model ] ]

    let clearAction msg = fun _ -> msg |> dispatch

    content