module ChickenCheck.WebTests.Pages.EggsPage

open System
open ChickenCheck.Backend
open OpenQA.Selenium
open canopy.classic

let parseDateQueryString (url: string) =
    DateTime.Parse(url.Split("?date=").[1])
    
let url rootUrl (date: DateTime) =
    rootUrl + "/eggs/" + date.ToString("yyyy-MM-dd")
let parseChickenId (element: IWebElement) = element.GetAttribute(DataAttributes.ChickenId) |> ChickenId.parse
let waitForCurrentDate (date: DateTime) =
    let selector = sprintf "[data-current-date=\"%s\"]" (date.ToString("yyyy-MM-dd"))
    waitForElement selector

module Selectors =
    let nextDateLink = "#next-date"
    let previousDateLink = "#previous-date"
    let chickenCard = ".chicken-card"
    let chickenCardById (id: ChickenId) = sprintf ".chicken-card[%s]" (DataAttributes.chickenIdStr id)
    let eggIcon (id: ChickenId) = sprintf ".egg-icon[%s]" (DataAttributes.chickenIdStr id)
    let datePicker = ".datetimepicker-dummy-wrapper"
    let previousMonth = ".datepicker-nav-previous"
    let dayButton (date: DateTime) = sprintf "[data-date*=\"%s\"]" (date.ToString("MMM dd"))
