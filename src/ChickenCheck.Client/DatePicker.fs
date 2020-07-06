module ChickenCheck.Client.Datepicker

open System
open ChickenCheck.Shared
open Fable.Core.JsInterop

type private DatePicker = {| date : {| start: Nullable<DateTime> |} |}

type private IBulmaCalendar =
    abstract attach : string * obj -> unit
    
let private bulmaCalendar : IBulmaCalendar = importDefault "bulma-calendar"

let private options =
    let tomorrow = DateTime.Today.AddDays(1.) //https://github.com/Wikiki/bulma-calendar/issues/211
    {| ``type`` = "date"
       displayMode = "default"
       dateFormat = "YYYY-MM-DD"
       showHeader = false
       showFooter = false
       weekStart = 1
       maxDate = tomorrow
       disabledDates = [| tomorrow |] |}
       
let init (browser: IBrowserService) (turbolinks: ITurbolinks) =
    fun currentDate ->
        bulmaCalendar.attach("#chickencheck-datepicker", !!options)
        let datepicker : DatePicker = browser.GetElementById("chickencheck-datepicker")?bulmaCalendar
        datepicker?value(currentDate.ToString())
            
        datepicker?on("select", fun _ ->
            let date =
                datepicker.date.start
                |> Option.ofNullable
                |> Option.map NotFutureDate.create
                |> Option.defaultValue NotFutureDate.today
            if date <> currentDate then
                let dateQueryStr = sprintf "?date=%s" (date.ToString())
                turbolinks.Visit(browser.UrlPath + dateQueryStr))
        |> ignore
        
