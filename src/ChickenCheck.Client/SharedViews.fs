module ChickenCheck.Client.SharedViews

open Feliz
open Feliz.Bulma

let loading =
    Html.span
        [
            Bulma.icon [
                color.hasTextWhite
                prop.children [
                    Html.i [
                        prop.classes [ "fa-3x fas fa-spinner fa-spin" ]
                    ]
                ]
            ]
        ]

let apiErrorMsg clearError (msg: string) =
    Bulma.notification [
        color.isDanger
        prop.children [
            Bulma.button.button [
                prop.onClick clearError
                prop.classes [ "delete" ]
            ]
            Bulma.title.h6 "Någonting gick fel"
            Html.p msg
        ]
    ]

