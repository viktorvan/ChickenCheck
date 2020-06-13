module ChickenCheck.Client.Navbar

open ChickenCheck.Shared
open Feliz
open Feliz.Bulma
open Feliz.Router
open Utils

let private logInLink =
    Bulma.navbarEnd.a [
        prop.href (Router.format "/login")
        prop.children [
            Bulma.navbarItem.div [
                prop.text "log in"
            ]
        ]
    ]
    
let private logOutLink =
    Bulma.navbarEnd.a [
        prop.href (Router.format "logout")
        prop.children [
            Bulma.navbarItem.div [
                prop.text "log out"
            ]
        ]
    ]

    
let view = elmishView "Navbar" (fun (props: {| IsMenuExpanded: bool
                                               User: User
                                               ToggleMenu: unit -> unit |}) ->
    Bulma.navbar [
        color.isInfo
        prop.children [
            Bulma.navbarBrand.div [
                Bulma.navbarItem.div [
                    prop.text "ChickenCheck"
                ]
                Bulma.navbarBurger [
                    prop.onClick (fun _ -> props.ToggleMenu())
                    navbarItem.hasDropdown
                    if props.IsMenuExpanded then navbarBurger.isActive
                    prop.children [
                        yield! List.replicate 3 (Html.span []) 
                    ]
                ]
            ]
            Bulma.navbarMenu [
                if props.IsMenuExpanded then navbarMenu.isActive
                prop.children [
                    match props.User with
                    | Anonymous -> logInLink
                    | ApiUser _ -> logOutLink
                ]
            ]
        ]
    ])
