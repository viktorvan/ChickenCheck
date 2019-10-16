module ChickenCheck.Client.ChickenCardList

open ChickenCheck.Domain
open Elmish
open Fulma
open FsToolkit.ErrorHandling
open ChickenCheck.Client

type Model =
    { Chickens : Map<ChickenId, ChickenCard.Model> }

let init date (eggCountMap: EggCountMap option) chickens =
    let toCardModel chicken eggCount date =
        ChickenCard.init chicken eggCount date

    let cardModels =
        chickens
        |> List.map
            (fun chicken ->
                let eggCount =
                    eggCountMap
                    |> Option.map (fun map -> map |> Map.find chicken.Id)
                chicken.Id, ChickenCard.init chicken eggCount date)
        |> Map.ofList

    { Chickens = cardModels }

type Msg = 
    CardMsg of ChickenId * ChickenCard.Msg

type ComponentMsg =
    | External of ChickenCard.ExternalMsg
    | Internal of Cmd<Msg>

let update eggApi (msg: Msg) (model: Model) : Model * ComponentMsg =
    match msg with
    | CardMsg (chickenId, msg) ->
        let (cardModel, result) = ChickenCard.update eggApi msg model.Chickens.[chickenId]
        let model = { model with Chickens = model.Chickens |> Map.add cardModel.Chicken.Id cardModel }
        let cmd =
            match result with
            | ChickenCard.Internal cmd -> 
                cmd |> Cmd.map (fun msg -> CardMsg(chickenId, msg)) |> Internal

            | ChickenCard.External msg -> 
                // we don't need to handle any child messages at this level, we pass them to our parent
                External msg

        model, cmd

let view model (dispatch: Dispatch<Msg>) =
    let dispatch chickenId msg = dispatch (CardMsg (chickenId, msg))

    let batchedCardModels =
        model.Chickens 
        |> Map.toList
        |> List.map snd
        |> List.sortBy (fun c -> c.Chicken.Name)
        |> List.batchesOf 3 

    let batchedCardViews =
        batchedCardModels
        |> List.map
            (fun batch ->
                batch
                |> List.map
                    (fun cardModel ->
                        ChickenCard.view cardModel (dispatch cardModel.Chicken.Id)))

    batchedCardViews
    |> List.map (Columns.columns []) 
    |> Container.container []
