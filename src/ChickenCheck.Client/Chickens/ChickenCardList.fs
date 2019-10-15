module ChickenCheck.Client.ChickenCardList

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
    { Chickens : Map<ChickenId, ChickenCard.Model> }

let init date (eggCountMap: Map<ChickenId,EggCount> option) chickens =
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

let private batchesOf size input = 
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

let update (chickenApi: IChickenApi) token (msg: Msg) (model: Model) : Model * ComponentMsg =
    match msg with
    | CardMsg (chickenId, msg) ->
        let (cardModel, result) = ChickenCard.update chickenApi token msg model.Chickens.[chickenId]
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
        |> batchesOf 3 

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
