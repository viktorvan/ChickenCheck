module ChickenCheck.Backend.Views.DatePicker

open ChickenCheck.Backend
open ChickenCheck.Shared
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
    let dateButton (id: string) icon (href: string option) =
        Bulma.button.a [
            prop.id id
            if href.IsSome then color.isPrimary else color.isLight
            button.isOutlined
            button.isLarge
            if href.IsSome then prop.href href.Value
            if href.IsNone then prop.style [ style.custom ("pointer-events", "none") ]
            prop.children [ icon ]
        ]

    let previousDateButton =
        currentDate
        |> NotFutureDate.tryAddDays -1
        |> Result.map Routing.chickensPage
        |> Option.ofResult
        |> dateButton "previous-date" iconLeft 
        
    let nextDateButton =
        currentDate
        |> NotFutureDate.tryAddDays 1
        |> Result.map Routing.chickensPage
        |> Option.ofResult
        |> dateButton "next-date" iconRight
        
    let currentDateAttr = prop.custom (DataAttributes.CurrentDate, currentDate.ToString())
    
    Bulma.level [
        level.isMobile
        prop.children [
            Bulma.levelItem [ previousDateButton ]
            Bulma.levelItem [
                Bulma.field.div [
                    prop.style [ style.width (length.percent 100) ]
                    prop.children [
                        Html.input [
                            prop.id "chickencheck-datepicker"
                            currentDateAttr
                            prop.type'.date
                            prop.value (currentDate.ToDateTime().ToString("yyyy-MM-dd"))
                        ]
                    ]
                ]
            ]
            Bulma.levelItem [ nextDateButton ]
        ]
    ]
