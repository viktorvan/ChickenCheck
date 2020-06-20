module ChickenCheck.Client.Chickens.DatePicker

open ChickenCheck.Client
open ChickenCheck.Shared
open System
open Feliz
open Feliz.Bulma

let private icon faIcon =
    Bulma.icon [
        Html.i [
            prop.classes [ sprintf "fas fa-3x %s" faIcon ]
        ]
    ]
    
let private iconLeft = icon "fa-caret-left"
let private iconRight = icon "fa-caret-right"

let render (props: {| CurrentDate: NotFutureDate 
                      ChangeDate: NotFutureDate -> unit |}) =
    let onDateSet date = props.ChangeDate date

    let parseDate ev =
        ev
        |> DateTime.Parse
        |> NotFutureDate.create
        
    let dateButton icon isDisabled onClick =
        Bulma.button.a [
            color.isLink
            button.isLarge
            prop.onClick onClick
            prop.disabled isDisabled
            prop.children [ icon ]
        ]

    let previousDateButton =
        let onClick = 
            let previousDate = props.CurrentDate |> NotFutureDate.addDays -1.
            fun _ -> onDateSet previousDate
        dateButton iconLeft false onClick
        
    let nextDateButton =
        if props.CurrentDate = NotFutureDate.today then
            dateButton iconRight true ignore
        else
            let onClick =
                let nextDate = props.CurrentDate |> NotFutureDate.addDays 1.
                fun _ -> onDateSet nextDate
            dateButton iconRight false onClick
        
    Bulma.level [
        level.isMobile
        prop.children [
            Bulma.levelItem [ previousDateButton ]
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
            Bulma.levelItem [ nextDateButton ]
        ]
    ]
    
let datePicker = Utils.memo "Datepicker" render