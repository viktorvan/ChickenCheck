module ChickenCheck.Client.Chickens

open ChickenCheck.Client.HtmlHelper
open ChickenCheck.Shared


let private showEggLoader (browser: IBrowserService) (id: ChickenId) =
    let selector = sprintf ".egg-icon-loader[%s]" (DataAttributes.chickenIdStr id)
    browser.QuerySelector selector
    |> Option.iter (HtmlHelper.toggleClass "is-hidden")
    
let private hideEggIcon (browser: IBrowserService) (id: ChickenId) =
    let selector = sprintf ".egg-icon[%s]" (DataAttributes.chickenIdStr id)
    browser.QuerySelector selector
    |> Option.iter (HtmlHelper.toggleClass "is-hidden")
    
let private serverErrorMsg = "server error"
let addEgg (api: IChickensApi) (browser: IBrowserService) (turbolinks: ITurbolinks) =
    fun chickenId date ->
        async {
            try 
                browser.StopPropagation()
                showEggLoader browser chickenId
                do! api.AddEgg(chickenId, date)
                browser.SaveScrollPosition()
                turbolinks.Reset(browser.UrlPath + browser.UrlQueryString)
            with exn ->
                eprintf "addEgg failed: %s" exn.Message
        }
        |> Async.StartImmediate
    
let removeEgg (api: IChickensApi) (browser: IBrowserService) (turbolinks: ITurbolinks) =
    fun chickenId date ->
        async {
            try
                browser.StopPropagation()
                hideEggIcon browser chickenId
                showEggLoader browser chickenId
                do! api.RemoveEgg(chickenId, date)
                browser.SaveScrollPosition()
                turbolinks.Reset(browser.UrlPath + browser.UrlQueryString)
            with exn -> 
                eprintf "removeEgg failed: %s" exn.Message
        }
        |> Async.StartImmediate
