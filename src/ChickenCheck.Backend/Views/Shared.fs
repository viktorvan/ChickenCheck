module ChickenCheck.Backend.Views.Shared

open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine

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
        
[<AutoOpen>]
module MyExtensions =
    type prop with
        static member onClick(value) = prop.custom("onclick", value)

