module ChickenCheck.Client.ChickenCard

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
    { Chicken : Chicken
      EggCount : EggCount 
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

let init chicken eggCount date =
    { Chicken = chicken
      EggCount = eggCount
      AddEggStatus = ApiCallStatus.NotStarted
      RemoveEggStatus = ApiCallStatus.NotStarted 
      CurrentDate = date }

let update (chickenApi: IChickenApi) token (msg:Msg) (model: Model) : Model * ComponentMsg =
    match msg with
    | AddEgg -> 
        { model with AddEggStatus = Running }, 
        callSecureApi
            token
            chickenApi.AddEgg
            { AddEgg.ChickenId = model.Chicken.Id; Date = model.CurrentDate }
            (fun _ -> AddedEgg)
            AddEggFailed
        |> Internal

    | AddedEgg -> 
        let model = { model with AddEggStatus = Completed }
        let newEggCount = 
            model.EggCount
            |> EggCount.increase

        match (newEggCount) with
        | Ok newEggCount ->
            { model with EggCount = newEggCount }, 
            ExternalMsg.AddedEgg model.Chicken.Id
            |> External
        | Result.Error (ValidationError (param, msg)) -> 
            model,
            sprintf "could not add egg: %s:%s" param msg
            |> AddEggFailed
            |> Cmd.ofMsg
            |> Internal

    | AddEggFailed msg -> 
        { model with AddEggStatus = Failed msg },
        ExternalMsg.Error msg
        |> External

    | RemoveEgg -> 
        { model with RemoveEggStatus = Running }, 
        callSecureApi
            token
            chickenApi.RemoveEgg
            { RemoveEgg.ChickenId = model.Chicken.Id; Date = model.CurrentDate }
            (fun _ -> RemovedEgg)
            RemoveEggFailed
            |> Internal

    | RemovedEgg -> 
        let model = { model with RemoveEggStatus = Completed }
        let newEggCount = 
            model.EggCount
            |> EggCount.decrease

        match (newEggCount) with
        | Ok newEggCount ->
            { model with EggCount = newEggCount }, 
            ExternalMsg.RemovedEgg model.Chicken.Id
            |> External
        | Result.Error (ValidationError (param, msg)) -> 
            model,
            sprintf "could not remove egg: %s:%s" param msg
            |> RemoveEggFailed
            |> Cmd.ofMsg
            |> Internal

    | RemoveEggFailed msg -> 
        { model with RemoveEggStatus = Failed msg },
        ExternalMsg.Error msg
        |> External


let view model (dispatch: Dispatch<Msg>) =
    let hasImage, imageUrl = 
        match model.Chicken.ImageUrl with
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
                    dispatch RemoveEgg) 
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
        let addedEggs = 
            let isRunning = 
                match model.AddEggStatus, model.RemoveEggStatus with
                | Running, _ | _, Running -> true
                | _ -> false

            if isRunning then 
                [ ViewComponents.loading ]
            else
                [ for i in 1..model.EggCount.Value do
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
            addedEggs

    let card =
        let header =
            span 
                [ ] 
                [ 
                    Heading.h4 
                        [ Heading.Modifiers [ Modifier.TextColor Color.IsWhite ] ] 
                        [ str model.Chicken.Name.Value ]
                    Heading.h6 
                        [ Heading.IsSubtitle;  Heading.Modifiers [ Modifier.TextColor Color.IsWhite ] ]
                        [ str model.Chicken.Breed.Value ] ]

        Card.card 
            [ 
                Props 
                    [ 
                        OnClick (fun _ -> dispatch AddEgg)
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
            Column.Width (Screen.Desktop, Column.Is4)
            Column.Width (Screen.Mobile, Column.Is12)
        ]
        [ card ]   
