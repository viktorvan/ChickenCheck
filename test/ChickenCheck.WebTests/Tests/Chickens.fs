module ChickenCheck.WebTests.Tests.Chickens

open System
open ChickenCheck.WebTests.Pages
open ChickenCheck.WebTests.Pages.ChickensPage
open canopy.classic
open canopy.runner.classic


let today = DateTime.Today

let all port =
    let rootUrl = (ChickensPage.rootUrl(port))
    let url = ChickensPage.url port
    
    canopy.classic.url rootUrl
    
    "root redirects to chickens page for current date" &&& fun _ ->
        onn (url today)
        
    "chickens page for 'today' does not have next-date link" &&& fun _ ->
        let nextDateLink = element Selectors.nextDateLink
        let href = nextDateLink.GetAttribute("href")
        if not (isNull href) then failwith "href should be null"
        
    "go back to yesterday" &&& fun _ ->
        click Selectors.previousDateLink
        let yesterday = today.AddDays(-1.0)
        onn (url yesterday)
        
    "go forward to today" &&& fun _ ->
        click Selectors.nextDateLink
        onn (url today)
        
    "navigate by datepicker" &&& fun _ ->
        let testNavigateToDay (day: DateTime) =
            click Selectors.datePicker
            let dayButton = elementWithText Selectors.dayButton (day.Day.ToString())
            click dayButton
            waitForCurrentDate day
            onn (url day)
        
        let inAPreviousMonth = today.AddDays(-40.0)
        canopy.classic.url (url inAPreviousMonth)
        
        let firstDayOfMonth = DateTime(inAPreviousMonth.Year, inAPreviousMonth.Month, 1)
        let secondDayOfMonth = DateTime(inAPreviousMonth.Year, inAPreviousMonth.Month, 2)
        
        testNavigateToDay firstDayOfMonth
        testNavigateToDay secondDayOfMonth
        
    "add and remove egg" &&& fun _ ->
        let chicken = first Selectors.chickenCard
        let id = parseChickenId chicken
        
        notDisplayed (Selectors.eggIcon id)
        
        click (Selectors.chickenCardById id)
        waitForElement (Selectors.eggIcon id)
        count (Selectors.eggIcon id) 1
        
        click (Selectors.eggIcon id)
        
        notDisplayed (Selectors.eggIcon id)
        
    "browser navigation works" &&& fun _ ->
        click Selectors.previousDateLink
        let yesterday = today.AddDays(-1.0)
        onn (url yesterday)
        navigate back
        onn (url today)
        navigate forward
        onn (url yesterday)
        
    "browser navigation does not add multiple datepickers" &&& fun _ ->
        click Selectors.previousDateLink
        click Selectors.previousDateLink
        navigate back
        navigate back
        navigate forward
        count Selectors.datePicker 1
