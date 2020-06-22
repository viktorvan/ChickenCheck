module ChickenCheck.Client.Chickens

open ChickenCheck.Shared

let addEgg (api: IChickensApi) (browser: IBrowserService) (turbolinks: ITurbolinks) =
    fun chickenId date ->
        async {
            browser.StopPropagation()
            browser.ShowEggLoader chickenId
            do! api.AddEgg(chickenId, date)
            turbolinks.Reset(browser.FullPath)
        }
        |> Async.StartImmediate
    
let removeEgg (api: IChickensApi) (browser: IBrowserService) (turbolinks: ITurbolinks) =
    fun chickenId date ->
        async {
            browser.StopPropagation()
            browser.HideEggIcon chickenId
            browser.ShowEggLoader chickenId
            do! api.RemoveEgg(chickenId, date)
            turbolinks.Reset(browser.FullPath)
        }
        |> Async.StartImmediate
