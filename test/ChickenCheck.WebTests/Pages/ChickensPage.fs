module ChickenCheck.WebTests.Pages.ChickensPage

open System
open ChickenCheck.Shared
open OpenQA.Selenium
open canopy.classic

let rootUrl port =
    sprintf "http://localhost:%i/chickens" port
let parseDateQueryString (url: string) =
    DateTime.Parse(url.Split("?date=").[1])
    
let url port (date: DateTime) =
    rootUrl(port) + "?date=" + date.ToString("yyyy-MM-dd")
let parseChickenId (element: IWebElement) = element.GetAttribute(DataAttributes.ChickenId) |> ChickenId.parse
module Selectors =
    let nextDateLink = "#next-date"
    let previousDateLink = "#previous-date"
    let chickenCard = ".chicken-card"
    let chickenCardById (id: ChickenId) = sprintf ".chicken-card[%s]" (DataAttributes.chickenIdStr id)
    let eggIcon (id: ChickenId) = sprintf ".egg-icon[%s]" (DataAttributes.chickenIdStr id)
    let datePicker = ".datetimepicker-dummy-wrapper"
    let dayButton = ".date-item"
