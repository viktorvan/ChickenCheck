module ChickenCheck.Client.ChickenCard

open Fable.React.Props
open Fable.React
open ChickenCheck.Domain
open ChickenCheck.Domain.Commands
open Elmish
open Fulma
open Fable.FontAwesome
open ChickenCheck.Client


type Model =
    { Chicken : Chicken
      EggCount : EggCount option
      AddEggStatus : ApiCallStatus 
      RemoveEggStatus : ApiCallStatus 
      CurrentDate : Date }

[<RequireQualifiedAccess>]
type ExternalMsg = 
    | AddedEgg of ChickenId: ChickenId
    | RemovedEgg of ChickenId: ChickenId
    | Error of string

type Msg = 
    | AddEgg
    | AddedEgg
    | AddEggFailed of string
    | RemoveEgg
    | RemovedEgg
    | RemoveEggFailed of string

type ComponentMsg =
    | External of ExternalMsg
    | Internal of Cmd<Msg>

type Api =
    { AddEgg : Commands.AddEgg -> Cmd<Msg>
      RemoveEgg : Commands.RemoveEgg -> Cmd<Msg> }

let init chicken eggCount date =
    { Chicken = chicken
      EggCount = eggCount
      AddEggStatus = ApiCallStatus.NotStarted
      RemoveEggStatus = ApiCallStatus.NotStarted 
      CurrentDate = date }

let update (eggApi: Api) (msg:Msg) (model: Model) : Model * ComponentMsg =
    match msg with
    | AddEgg -> 
        { model with AddEggStatus = Running }, 
        eggApi.AddEgg
            { AddEgg.ChickenId = model.Chicken.Id; Date = model.CurrentDate }
        |> Internal

    | AddedEgg -> 
        let model = { model with AddEggStatus = Completed }
        match model.EggCount with
        | None -> model, Cmd.none |> Internal
        | Some eggCount ->
            let newEggCount = 
                eggCount
                |> EggCount.increase

            match (newEggCount) with
            | Ok newEggCount ->
                { model with EggCount = Some newEggCount }, 
                ExternalMsg.AddedEgg model.Chicken.Id
                |> External
            | Result.Error (ValidationError (param, msg)) -> 
                model,
                sprintf "could not add egg: %s:%s" param msg
                |> AddEggFailed
                |> Cmd.ofMsg
                |> Internal

    | AddEggFailed msg -> 
        model,
        ExternalMsg.Error msg
        |> External

    | RemoveEgg -> 
        { model with RemoveEggStatus = Running }, 
        eggApi.RemoveEgg
            { RemoveEgg.ChickenId = model.Chicken.Id; Date = model.CurrentDate }
            |> Internal

    | RemovedEgg -> 
        let model = { model with RemoveEggStatus = Completed }
        match model.EggCount with 
        | None -> model, Cmd.none |> Internal
        | Some eggCount ->
            let newEggCount = 
                eggCount
                |> EggCount.decrease

            match (newEggCount) with
            | Ok newEggCount ->
                { model with EggCount = Some newEggCount }, 
                ExternalMsg.RemovedEgg model.Chicken.Id
                |> External
            | Result.Error (ValidationError (param, msg)) -> 
                model,
                sprintf "could not remove egg: %s:%s" param msg
                |> RemoveEggFailed
                |> Cmd.ofMsg
                |> Internal

    | RemoveEggFailed msg -> 
        model,
        ExternalMsg.Error msg
        |> External
let view model (dispatch: Dispatch<Msg>) =

    let eggIcons =
        let eggIcon = 
            Icon.icon 
                [ 
                    Icon.Size IsLarge
                    Icon.Modifiers [ Modifier.TextColor Color.IsWhite ] 
                    Icon.Props 
                        [ OnClick 
                            (fun ev ->
                                ev.cancelBubble <- true
                                ev.stopPropagation()
                                dispatch RemoveEgg)]
                ] 
                [ 
                    Fa.i [ Fa.Size Fa.Fa5x; Fa.Solid.Egg ] [] 
                ] 

        let addedEggs = 
            let isLoading = 
                match model.AddEggStatus, model.RemoveEggStatus, model.EggCount with
                | Running, _, _ | _, Running, _ | _, _, None -> true
                | _ -> false

            if isLoading then 
                [ ViewComponents.loading ]
            else
                [ for _ in 1..model.EggCount.Value.Value do
                    yield 
                        Column.column 
                            [ 
                                Column.Width (Screen.All, Column.Is3) 
                            ] 
                            [ 
                                eggIcon 
                            ] 
                ]

        Columns.columns 
            [ 
                Columns.IsCentered
                Columns.IsVCentered
                Columns.IsMobile
                Columns.Props [ Style [ Height 200 ] ]
            ] 
            addedEggs

    let header =
        div 
            [ ] 
            [ 
                Heading.h4 
                    [ Heading.Modifiers [ Modifier.TextColor Color.IsWhite ] ] 
                    [ str model.Chicken.Name.Value ]
                Heading.h6 
                    [ Heading.IsSubtitle;  Heading.Modifiers [ Modifier.TextColor Color.IsWhite ] ]
                    [ str model.Chicken.Breed.Value ] 
            ]

    let cardBackgroundStyle =

        let imageUrlStr = 
            match model.Chicken.ImageUrl with
            | Some (ImageUrl imageUrl) -> imageUrl
            | None -> ""

        Style 
            [ 
                sprintf "linear-gradient(rgba(0,0,0,0.5), rgba(0,0,0,0)), url(%s)" imageUrlStr 
                |> box 
                |> BackgroundImage 
                BackgroundRepeat "no-repeat"
                BackgroundSize "cover" 
            ] 

    Column.column
        [ 
            Column.Width (Screen.Desktop, Column.Is4)
            Column.Width (Screen.Mobile, Column.Is12)
        ]
        [
            Card.card 
                [ 
                    Props 
                        [ 
                            OnClick (fun _ -> dispatch AddEgg)
                            cardBackgroundStyle
                        ] 
                ]
                [ 
                    Card.header [] [ header ] 
                    Card.content [] [ eggIcons ] 
                ]
        ]   
