module ChickenCheck.Backend.Views.DatePicker
open ChickenCheck.Shared
open System
open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine

let private icon faIcon =
    Bulma.icon [
        Html.i [
            prop.classes [ sprintf "fas fa-3x %s" faIcon ]
        ]
    ]
    
let private iconLeft = icon "fa-caret-left"
let private iconRight = icon "fa-caret-right"

let layout (currentDate: NotFutureDate) =

    let parseDate ev =
        ev
        |> DateTime.Parse
        |> NotFutureDate.create
        
    let dateButton icon isDisabled =
        Bulma.button.a [
            color.isLink
            button.isLarge
//            prop.onClick onClick
            prop.disabled isDisabled
            prop.children [ icon ]
        ]

    let previousDateButton =
//        let onClick = 
//            let previousDate = props.CurrentDate |> NotFutureDate.addDays -1.
//            fun _ -> onDateSet previousDate
        dateButton iconLeft false 
        
    let nextDateButton =
        if currentDate = NotFutureDate.today then
            dateButton iconRight true 
        else
//            let onClick =
//                let nextDate = currentDate |> NotFutureDate.addDays 1.
//                fun _ -> onDateSet nextDate
            dateButton iconRight false 
        
    Bulma.level [
        level.isMobile
        prop.children [
            Bulma.levelItem [ previousDateButton ]
            Bulma.levelItem [
                Bulma.field.div [
                    prop.style [ style.width (length.percent 100) ]
                    prop.children [
                        Bulma.input.date [
//                            prop.onChange (parseDate >> onDateSet)
                            prop.value (currentDate.ToDateTime().ToString("yyyy-MM-dd"))
                        ]
                    ]
                ]
            ]
            Bulma.levelItem [ nextDateButton ]
        ]
    ]
