module ChickenCheck.Backend.Views.DatePicker
open ChickenCheck.Backend
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
        
    let dateButton icon isDisabled (href: string option) =
        Bulma.button.a [
            color.isLink
            button.isLarge
            if isDisabled then prop.disabled true
            if href.IsSome then prop.href href.Value
            prop.children [ icon ]
        ]

    let previousDateButton =
        currentDate
        |> NotFutureDate.addDays -1.0
        |> Routing.chickensPage
        |> Some
        |> dateButton iconLeft false
        
    let nextDateButton =
        if currentDate = NotFutureDate.today then
            dateButton iconRight true None
        else
            currentDate
            |> NotFutureDate.addDays 1.0
            |> Routing.chickensPage
            |> Some
            |> dateButton iconRight false 
        
    let currentDateAttr = prop.custom (DataAttributes.CurrentDate, currentDate.ToString())
    
    Bulma.level [
        level.isMobile
        prop.children [
            Bulma.levelItem [ previousDateButton ]
            Bulma.levelItem [
                Bulma.field.div [
                    prop.style [ style.width (length.percent 100) ]
                    prop.children [
                        Bulma.input.date [
                            currentDateAttr
//                            prop.onChange (parseDate >> onDateSet)
                            prop.value (currentDate.ToDateTime().ToString("yyyy-MM-dd"))
                        ]
                    ]
                ]
            ]
            Bulma.levelItem [ nextDateButton ]
        ]
    ]
