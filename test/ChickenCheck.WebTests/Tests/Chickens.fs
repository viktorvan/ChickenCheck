module ChickenCheck.WebTests.Tests.Chickens

open System
open ChickenCheck.WebTests.Pages
open ChickenCheck.WebTests.Pages.ChickensPage
open canopy.classic
open canopy.runner.classic


let today = DateTime.Today

let all() =
    url ChickensPage.rootUrl
    
    "root redirects to chickens page for current date" &&& fun _ ->
        onn (ChickensPage.url today)
        
    "chickens page for 'today' does not have next-date link" &&& fun _ ->
        let nextDateLink = element Selectors.nextDateLink
        let href = nextDateLink.GetAttribute("href")
        if not (isNull href) then failwith "href should be null"
        
    "go back to yesterday" &&& fun _ ->
        click Selectors.previousDateLink
        let yesterday = today.AddDays(-1.0)
        onn (ChickensPage.url yesterday)
        
    "go forward to today" &&& fun _ ->
        click Selectors.nextDateLink
        onn (ChickensPage.url today)
        
    "navigate by datepicker" &&& fun _ ->
        let testNavigateToDay (day: DateTime) =
            click Selectors.datePicker
            let dayButton = elementWithText Selectors.dayButton (day.Day.ToString())
            click dayButton
            onn (ChickensPage.url day)
        
        let inAPreviousMonth = today.AddDays(-40.0)
        url (ChickensPage.url inAPreviousMonth)
        
        let firstDayOfMonth = DateTime(inAPreviousMonth.Year, inAPreviousMonth.Month, 1)
        let secondDayOfMonth = DateTime(inAPreviousMonth.Year, inAPreviousMonth.Month, 2)
        
        testNavigateToDay firstDayOfMonth
        testNavigateToDay secondDayOfMonth
        
    "add and remove egg" &&& fun _ ->
        let chicken = first Selectors.chickenCard
        let id = parseChickenId chicken
        
        notDisplayed (Selectors.eggIcon id)
        
        click (Selectors.chickenCardById id)
        displayed (Selectors.eggIcon id)
        count (Selectors.eggIcon id) 1
        
        click (Selectors.eggIcon id)
        notDisplayed (Selectors.eggIcon id)
        
    "browser navigation works" &&& fun _ ->
        click Selectors.previousDateLink
        let yesterday = today.AddDays(-1.0)
        onn (ChickensPage.url yesterday)
        navigate back
        onn (ChickensPage.url today)
        navigate forward
        onn (ChickensPage.url yesterday)
        
    "browser navigation does not add multiple datepickers" &&& fun _ ->
        click Selectors.previousDateLink
        click Selectors.previousDateLink
        navigate back
        navigate back
        navigate forward
        count Selectors.datePicker 1
