module ChickenCheck.WebTests.Tests.Chickens

open System
open ChickenCheck.WebTests
open ChickenCheck.WebTests.Pages
open ChickenCheck.WebTests.Pages.EggsPage
open canopy.classic
open canopy.runner.classic


let today = DateTime.Today

let login() =
    try
        displayed "log in"
        click "log in"
        displayed "#auth0-lock-container-1"
        "[name=email]" << Configuration.config.Value.Username
        "[name=password]" << Configuration.config.Value.Password
        click "[name=submit]"
        displayed "Test-User"
    with exn ->
        describe ("Failed to login: " + exn.Message)
        describe "check if user is already logged in"
        displayed "Test-User"

let all rootUrl =
    let eggsUrl = EggsPage.url rootUrl
    
    url (rootUrl + "/eggs")
    
    "root redirects to eggs page for current date" &&& fun _ ->
        onn (eggsUrl today)
        
    "eggs page for 'today' does not have next-date link" &&& fun _ ->
        let nextDateLink = element Selectors.nextDateLink
        let href = nextDateLink.GetAttribute("href")
        if not (isNull href) then failwith "href should be null"
        
    "go back to yesterday" &&& fun _ ->
        click Selectors.previousDateLink
        let yesterday = today.AddDays(-1.0)
        onn (eggsUrl yesterday)
        
    "go forward to today" &&& fun _ ->
        click Selectors.nextDateLink
        onn (eggsUrl today)
        
    "navigate by datepicker" &&& fun _ ->
        let testNavigateToDay (day: DateTime) =
            describe ("navigating by datepicker to: " + day.ToString())
            click Selectors.datePicker
            click Selectors.previousMonth
            click (Selectors.dayButton day)
            onn (eggsUrl day)
            waitForCurrentDate day
        
        let inAPreviousMonth = today.AddMonths(-1)
        url (eggsUrl inAPreviousMonth)
        
        let firstDayOfMonth = DateTime(inAPreviousMonth.Year, inAPreviousMonth.Month, 1)
        let twentiethDayOfMonth = DateTime(inAPreviousMonth.Year, inAPreviousMonth.Month, 20)
        
        testNavigateToDay firstDayOfMonth
        url (rootUrl + "/eggs")
        testNavigateToDay twentiethDayOfMonth
        
    "add and remove egg" &&& fun _ ->
        login()
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
        onn (eggsUrl yesterday)
        navigate back
        onn (eggsUrl today)
        navigate forward
        onn (eggsUrl yesterday)
        
    "navigation by url to future date defaults to 'today'" &&& fun _ ->
        let tomorrow = today.AddDays(1.)
        url (eggsUrl tomorrow)
        onn (eggsUrl today)
        
    "browser navigation does not add multiple datepickers" &&& fun _ ->
        click Selectors.previousDateLink
        click Selectors.previousDateLink
        navigate back
        navigate back
        navigate forward
        count Selectors.datePicker 1
