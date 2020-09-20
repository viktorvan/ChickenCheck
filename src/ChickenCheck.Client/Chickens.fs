module ChickenCheck.Client.Chickens

open ChickenCheck.Client.HtmlHelper
open ChickenCheck.Shared


let private toggleEggLoader (browser: IBrowserService) (id: ChickenId) =
    let selector = sprintf ".egg-icon-loader[%s]" (DataAttributes.chickenIdStr id)
    browser.QuerySelector selector
    |> Option.iter (HtmlHelper.toggleClass "is-hidden")
    
let private toggleEggIcon (browser: IBrowserService) (id: ChickenId) =
    let selector = sprintf ".egg-icon[%s]" (DataAttributes.chickenIdStr id)
    browser.QuerySelector selector
    |> Option.iter (HtmlHelper.toggleClass "is-hidden")
    
let private showErrorIcon (browser: IBrowserService) (id: ChickenId) =
    let selector = sprintf ".egg-error[%s]" (DataAttributes.chickenIdStr id)
    browser.QuerySelector selector
    |> Option.iter (HtmlHelper.removeClass "is-hidden")
    
    
//let addEgg (api: IChickensApi) (browser: IBrowserService) (turbolinks: ITurbolinks) (scrollService: IScrollPositionService) =
//    fun chickenId date ->
//        async {
//            try 
//                browser.StopPropagation()
//                toggleEggLoader browser chickenId
//                do! api.AddEgg(chickenId, date)
//                scrollService.Save()
//                turbolinks.Reset(browser.UrlPath + browser.UrlQueryString)
//            with exn ->
//                toggleEggLoader browser chickenId
//                showErrorIcon browser chickenId
//                eprintf "addEgg failed: %s" exn.Message
//        }
//        |> Async.StartImmediate
//    
//let removeEgg (api: IChickensApi) (browser: IBrowserService) (turbolinks: ITurbolinks) (scrollService: IScrollPositionService) =
//    fun chickenId date ->
//        async {
//            try
//                browser.StopPropagation()
//                toggleEggIcon browser chickenId
//                toggleEggLoader browser chickenId
//                do! api.RemoveEgg(chickenId, date)
//                scrollService.Save()
//                turbolinks.Reset(browser.UrlPath + browser.UrlQueryString)
//            with exn ->
//                toggleEggLoader browser chickenId
//                toggleEggIcon browser chickenId
//                showErrorIcon browser chickenId
//                eprintf "removeEgg failed: %s" exn.Message
//        }
//        |> Async.StartImmediate
