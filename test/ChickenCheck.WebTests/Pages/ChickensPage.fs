module ChickenCheck.WebTests.Pages.ChickensPage

open System
open ChickenCheck.Shared
open OpenQA.Selenium

let rootUrl = "http://localhost:8087/chickens"
let parseDateQueryString (url: string) =
    DateTime.Parse(url.Split("?date=").[1])
    
let url (date: DateTime) =
    rootUrl + "?date=" + date.ToString("yyyy-MM-dd")
let parseChickenId (element: IWebElement) = element.GetAttribute(DataAttributes.ChickenId) |> ChickenId.parse
module Selectors =
    let nextDateLink = "#next-date"
    let previousDateLink = "#previous-date"
    let chickenCard = ".chicken-card"
    let chickenCardById (id: ChickenId) = sprintf ".chicken-card[%s]" (DataAttributes.chickenIdStr id)
    let eggIcon (id: ChickenId) = sprintf ".egg-icon[%s]" (DataAttributes.chickenIdStr id)
    let datePicker = ".datetimepicker-dummy-wrapper"
    let dayButton = ".date-item"
