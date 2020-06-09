module ChickenCheck.Client.Navbar

open ChickenCheck.Shared
open Feliz
open Feliz.Bulma
open Feliz.Router

type NavbarMsg =
    | ToggleMenu
    | Logout
    
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

let view user isMenuExpanded dispatch = 
    Bulma.navbar [
        color.isInfo
        prop.children [
            Bulma.navbarBrand.div [
                Bulma.navbarItem.div [
                    prop.text "ChickenCheck"
                ]
                Bulma.navbarBurger [
                    prop.onClick (fun _ -> dispatch ToggleMenu)
                    prop.className [ if isMenuExpanded then "is-active"; "navbar-burger" ]
                    prop.children (List.replicate 3 (Html.span []))
                ]
            ]
            Bulma.navbarMenu [
                user
                |> Deferred.map (function
                    | Anonymous -> logInLink
                    | ApiUser _ -> logOutLink)
                |> Deferred.defaultValue logInLink
            ]
        ]
    ]
