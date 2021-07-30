module ChickenCheck.Backend.Views.DatePicker

open ChickenCheck.Backend
open ChickenCheck.Backend.Extensions
open Giraffe.ViewEngine

let private icon faIcon =
    span 
        [ _class "icon" ]
        [ i [ _class $"fas fa-3x %s{faIcon}" ] [ ] ]
    
let private iconLeft = icon "fa-caret-left"
let private iconRight = icon "fa-caret-right"

let layout (currentDate: NotFutureDate) =
    let dateButton (id: string) icon (href: string option) =
        let buttonColor = if href.IsSome then "is-primary" else "is-light"
        a 
            [ _class $"button %s{buttonColor} is-outlined is-large"
              _id id
              if href.IsSome then _href href.Value
              if href.IsNone then _style "pointer-events: none;"
            ] 
            [ icon ]

    let previousDateButton =
        currentDate
        |> NotFutureDate.tryAddDays -1
        |> Result.map Routing.eggsPage
        |> Option.ofResult
        |> dateButton "previous-date" iconLeft 
        
    let nextDateButton =
        currentDate
        |> NotFutureDate.tryAddDays 1
        |> Option.ofResult
        |> Option.map Routing.eggsPage
        |> dateButton "next-date" iconRight
        
    let currentDateAttr = attr DataAttributes.CurrentDate (currentDate.ToString())
    
    nav [ _class "level is-mobile" ] 
        [
            div [ _class "level-item" ] [ previousDateButton ]
            div 
                [ _class "level-item" ] 
                [
                    div 
                        [ _class "field"; _style "width: 100%;" ] 
                        [
                            input 
                                [
                                    _class "input"
                                    _id "chickencheck-datepicker"
                                    currentDateAttr
                                    _type "date"
                                    _value (currentDate.ToDateTime().ToString("yyyy-MM-dd"))
                                ]
                        ]
                ]
            div [ _class "level-item" ] [ nextDateButton ]
        ]
