module ChickenCheck.Client.Chickens.Render

open ChickenCheck.Client
open ChickenCheck.Shared
open FsToolkit.ErrorHandling
open Feliz
open Feliz.Bulma
open Feliz.Bulma.PageLoader
open ChickenCheck.Client.Chickens
open Elmish
open Contexts
open Feliz.UseElmish
open ChickenCard
open Statistics

type ChickenDetails =
    { Id: ChickenId
      Name : string
      ImageUrl : ImageUrl option 
      Breed : string
      TotalEggCount : EggCount
      EggCountOnDate : EggCount
      IsLoading : bool }
type Model =
    { Chickens : Deferred<Map<ChickenId, ChickenDetails>>
      CurrentDate : NotFutureDate
      Errors : string list }
      
module Model =
    let init date =
        { Chickens = InProgress
          Errors = []
          CurrentDate = date }

let setIsLoading model state id =
    let c =
        model.Chickens
        |> Deferred.map (fun chickens ->
            chickens
            |> Map.change id (Option.map (fun c -> { c with IsLoading = state }))
            )
    { model with Chickens = c }
    
let setStartLoading id model  = setIsLoading model true id
let setStopLoading id model = setIsLoading model false id
    
let changeEggCount model id f =
    let newChickens =
        model.Chickens
        |> Deferred.map (fun chickens ->
            chickens
            |> Map.change id (Option.map (fun c ->
                { c with 
                    EggCountOnDate = f c.EggCountOnDate
                    TotalEggCount = f c.TotalEggCount  } ))
        )
    { model with Chickens = newChickens }
        
let increaseEggCount id model = changeEggCount model id EggCount.increase
    
let decreaseEggCount id model = changeEggCount model id EggCount.decrease

let update (api: IChickenApiCmds) (msg: Msg) (model: Model) : Model * Cmd<Msg>=
    match msg with
    | GetAllChickens (Start date) ->
        model, api.GetAllChickens(date)
        
    | GetAllChickens (Finished (Ok chickens)) ->
        let buildModel { Chicken = chicken; Count = (_, onDateCount); TotalCount = totalCount } =
            chicken.Id,
            { Id = chicken.Id
              Name = chicken.Name
              ImageUrl = chicken.ImageUrl
              Breed = chicken.Breed
              TotalEggCount = totalCount
              EggCountOnDate  = onDateCount
              IsLoading = false }
            
        let newChickens = chickens |> List.map buildModel |> Map.ofList
            
        { model with 
            Chickens = Deferred.Resolved newChickens }, Cmd.none
            
    | GetAllChickens (Finished (Error err)) -> notImplemented()
        
    | GetEggCount (Start (date, chickens)) ->
        model, api.GetEggCount(date, chickens)

    | GetEggCount (Finished (Ok (date, countByChicken))) -> 
        if model.CurrentDate = date then
            if countByChicken |> Map.isEmpty then
                model, 
                "Count by date map was empty" |> AddError |> Cmd.ofMsg
            else
                let updateEggCount id (model:ChickenDetails) =
                    match countByChicken |> Map.tryFind id with
                    | Some newCount ->
                        { model with EggCountOnDate = newCount }
                    | None -> model
                    
                model.Chickens
                |> Deferred.map (fun chickens ->
                    { model with Chickens = chickens |> Map.map updateEggCount |> Resolved }, Cmd.none
                    )
                |> Deferred.defaultValue (model, Cmd.none)
        else
            model, Cmd.none
            
    | GetEggCount (Finished (Error err)) -> notImplemented()

    | ChangeDate date ->
        match model.Chickens with
        | Deferred.InProgress ->
            model, Cmd.none
        | Deferred.Resolved chickens ->
            let chickenIds = chickens |> Map.keys
            let clearEggCount chickens = chickens |> Map.map (fun _ c -> { c with EggCountOnDate = EggCount.zero })
            let newChickens = clearEggCount chickens
            { model with CurrentDate = date; Chickens = Resolved newChickens }, api.GetEggCount(date, chickenIds)
        | Deferred.HasNotStartedYet ->
            { model with CurrentDate = date }, api.GetAllChickens(date) 
        
    | AddEgg (Start (id, date)) ->
        model
        |> setStartLoading id, api.AddEgg (id, date)

    | AddEgg (Finished (Ok (id, date))) ->
        model
        |> setStopLoading id
        |> increaseEggCount id, Cmd.none
        
    | AddEgg (Finished (Error (id, err))) ->
        printfn "%s" err
        model
        |> setStopLoading id, Cmd.none

    | RemoveEgg (Start (id, date)) -> 
        model
        |> setStartLoading id, api.RemoveEgg (id, date)

    | RemoveEgg (Finished (Ok (id, date))) ->
        model
        |> setStopLoading id
        |> decreaseEggCount id, Cmd.none
        
    | RemoveEgg (Finished (Error (id, date))) -> 
        model
        |> setStopLoading id, Cmd.none
        
    | AddError msg ->
        { model with Errors = msg :: model.Errors }, Cmd.none
    | ClearErrors -> 
        { model with Errors = [] }, Cmd.none
        
let init date = fun () -> Model.init date, Start date |> GetAllChickens |> Cmd.ofMsg

let private render initDate api () =
    let user = React.useContext (userContext)
    let model, dispatch = React.useElmish(init initDate, update api, [| |])
    let header =
        Bulma.subtitle.h2 [
            text.hasTextCentered
            prop.text "Vem värpte idag?"
        ]

    let errorView =
        let errorFor item = 
            item
            |> SharedViews.apiErrorMsg (fun _ -> ClearErrors |> dispatch) 

        model.Errors |> List.map errorFor

    let hasErrors = model.Errors |> List.isEmpty |> not
    
    let runIfLoggedIn user f =
        match user with
        | (ApiUser _) -> f
        | _ -> ignore
        
    let chickenListView = 
        let cardViewRows (chickens: ChickenDetails list) = 
            let cardView (chicken: ChickenDetails) =
                let addEgg = runIfLoggedIn user (fun () -> Start (chicken.Id, model.CurrentDate) |> AddEgg |> dispatch)
                let removeEgg = runIfLoggedIn user (fun () -> Start (chicken.Id, model.CurrentDate) |> RemoveEgg |> dispatch)
 
                let model = 
                    { Id = chicken.Id
                      Name = chicken.Name
                      Breed = chicken.Breed
                      ChickenCard.Model.CurrentDate = model.CurrentDate
                      ChickenCard.Model.EggCount = chicken.EggCountOnDate
                      ChickenCard.Model.ImageUrl = chicken.ImageUrl
                      ChickenCard.Model.IsLoading = chicken.IsLoading }
                let cardProps =
                    {| Model = model
                       AddEgg = addEgg
                       RemoveEgg = removeEgg |}
                let card = ChickenCard.chickenCard cardProps
                
                Bulma.column [
                    column.is4
                    prop.children [ card ] 
                ]
                
            let cardViewRow rowChickens = List.map cardView rowChickens
            
            chickens
            |> List.sortBy (fun c -> c.Name)
            |> List.batchesOf 3
            |> List.map cardViewRow

        model.Chickens
        |> Deferred.map (fun chickens ->
            chickens
            |> Map.values
            |> cardViewRows
            |> List.map Bulma.columns 
            |> Bulma.container)
        |> Deferred.defaultValue Html.none

    let statistics =
        model.Chickens
        |> Deferred.map (fun chickens ->
            let props =
                let chickens = chickens |> Map.values
                {| Chickens = chickens |> List.map (fun c -> { Name = c.Name 
                                                               EggCount = c.TotalEggCount }) |}
            Statistics.statistics props)
        |> Deferred.defaultValue Html.none 
        
    let view =
        Bulma.section [
            header
            DatePicker.datePicker {| CurrentDate = model.CurrentDate; ChangeDate = ChangeDate >> dispatch |}
            chickenListView
            statistics
        ]
            
    Html.div [
        PageLoader.pageLoader [
            pageLoader.isInfo
            if not (model.Chickens |> Deferred.resolved) then pageLoader.isActive 
        ]
        if hasErrors then Bulma.section errorView
        view
    ]
    
let chickens initDate api = React.memo("Chickens", render initDate api)
