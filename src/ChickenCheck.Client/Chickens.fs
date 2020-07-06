module ChickenCheck.Client.Chickens

open ChickenCheck.Shared

let private showEggLoader (browser: IBrowserService) (id: ChickenId) =
    browser.GetElementById("egg-icon-loader-" + id.Value.ToString())
    |> Option.iter (HtmlHelper.toggleClass "is-hidden")
    
let private hideEggIcon (browser: IBrowserService) (id: ChickenId) =
    browser.GetElementById("egg-icon-" + id.Value.ToString())
    |> Option.iter (HtmlHelper.toggleClass "is-hidden")
    
let addEgg (api: IChickensApi) (browser: IBrowserService) (turbolinks: ITurbolinks) =
    fun chickenId date ->
        async {
            browser.StopPropagation()
            showEggLoader browser chickenId
            do! api.AddEgg(chickenId, date)
            browser.SaveScrollPosition()
            turbolinks.Reset(browser.UrlPath + browser.UrlQueryString)
        }
        |> Async.StartImmediate
    
let removeEgg (api: IChickensApi) (browser: IBrowserService) (turbolinks: ITurbolinks) =
    fun chickenId date ->
        async {
            browser.StopPropagation()
            hideEggIcon browser chickenId
            showEggLoader browser chickenId
            do! api.RemoveEgg(chickenId, date)
            browser.SaveScrollPosition()
            turbolinks.Reset(browser.UrlPath + browser.UrlQueryString)
        }
        |> Async.StartImmediate
