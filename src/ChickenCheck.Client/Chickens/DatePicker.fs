module ChickenCheck.Client.DatePicker

open ChickenCheck.Client
open ChickenCheck.Domain
open Fable.React
open System
open Feliz
open Feliz.Bulma

type DatePickerProps =
    { CurrentDate : Date
      OnChangeDate : Date -> unit }
      
let private icon faIcon =
    Bulma.icon [
        Html.i [
            prop.classes [ sprintf "fas fa-3x %s" faIcon ]
        ]
    ]
    
let private iconLeft = icon "fa-caret-left"
let private iconRight = icon "fa-caret-right"

let view = Utils.elmishView "DatePicker" (fun (props : DatePickerProps) ->
    let onDateSet date =
        props.OnChangeDate date
    let onDateChange delta =    
        onDateSet (Date.addDays delta props.CurrentDate)
    let previousDate _ = onDateChange -1.
    let nextDate _ = onDateChange 1.

    let parseDate (ev: Browser.Types.Event) =
        ev.Value
        |> DateTime.Parse
        |> Date.create

    let dateButton onClick icon =
        Bulma.button.a [
            color.isLink
            button.isLarge
            prop.onClick onClick
            prop.children [ icon ]
        ]
    Bulma.level [
        level.isMobile
        prop.children [
            Bulma.levelItem [ dateButton previousDate iconLeft ]
            Bulma.levelItem [
                Bulma.field.div [
                    prop.style [ style.width (length.percent 100) ]
                    prop.children [
                        Bulma.input.date [
                            prop.onChange (parseDate >> onDateSet)
                            prop.value (props.CurrentDate.ToDateTime().ToString("yyyy-MM-dd"))
                        ]
                    ]
                ]
            ]
            Bulma.levelItem [ dateButton nextDate iconRight ]
        ]
    ])