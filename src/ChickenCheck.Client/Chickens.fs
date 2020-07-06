module ChickenCheck.Client.Chickens

open ChickenCheck.Shared

let showEggLoader (browser: IBrowserService) (id: ChickenId) =
    let eggIconLoader = browser.GetElementById("egg-icon-loader-" + id.Value.ToString())
    eggIconLoader |> Option.iter (fun l -> l.className <- l.className.Replace("is-hidden", ""))
    
let hideEggIcon (browser: IBrowserService) (id: ChickenId) =
    let eggIconLoader = browser.GetElementById("egg-icon-" + id.Value.ToString())
    eggIconLoader |> Option.iter (fun l -> l.className <- l.className + " is-hidden")
    
let addEgg (api: IChickensApi) (browser: IBrowserService) (turbolinks: ITurbolinks) =
    fun chickenId date ->
        async {
            browser.StopPropagation()
            showEggLoader browser chickenId
            do! api.AddEgg(chickenId, date)
            turbolinks.Reset(browser.FullUrlPath)
        }
        |> Async.StartImmediate
    
let removeEgg (api: IChickensApi) (browser: IBrowserService) (turbolinks: ITurbolinks) =
    fun chickenId date ->
        async {
            browser.StopPropagation()
            hideEggIcon browser chickenId
            showEggLoader browser chickenId
            do! api.RemoveEgg(chickenId, date)
            turbolinks.Reset(browser.FullUrlPath)
        }
        |> Async.StartImmediate
        
