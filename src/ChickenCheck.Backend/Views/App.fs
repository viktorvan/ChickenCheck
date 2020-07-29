module ChickenCheck.Backend.Views.App

open ChickenCheck.Backend
open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine
open ChickenCheck.Shared

let logInLink =
    Bulma.navbarItem.a [ 
        prop.href "/login"
        prop.text "log in" ]

let logOutLink =
    Bulma.navbarItem.a [ 
        prop.href "/logout"
        prop.text "log out" ]
          
let layout user content =
    Html.html [
        Html.head [
            Html.meta [
                prop.charset.utf8
            ]
            Html.title "ChickenCheck"
            Html.link [
                prop.rel "apple-touch-icon"
                prop.sizes "180x180"
                prop.href "Icons/apple-touch-icon.png"
            ]
            Html.link [
                prop.rel "icon" 
                prop.type' "image/png" 
                prop.sizes "32x32" 
                prop.href "Icons/favicon-32x32.png"
            ]
            Html.link [
                prop.rel "icon"
                prop.type' "image/png"
                prop.sizes "16x16"
                prop.href "Icons/favicon-16x16.png" 
            ]
            Html.link [ 
                prop.rel "manifest" 
                prop.href "Icons/site.webmanifest"
            ]
            Html.link [
                prop.rel "stylesheet" 
                prop.href "https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap"
            ]
            yield! Bundle.bundle
        ]
        Html.body [
            Bulma.navbar [
                prop.id "chickencheck-navbar"
                prop.custom ("data-turbolinks-permanent", "")
                color.isInfo
                prop.children [ 
                    Bulma.navbarBrand.div [ 
                        Bulma.navbarItem.div [ prop.text "ChickenCheck" ]
                        Bulma.navbarBurger [
                            prop.id "chickencheck-navbar-burger"
                            navbarItem.hasDropdown
                            prop.children [ yield! List.replicate 3 (Html.span []) ] 
                        ] 
                    ]
                    Bulma.navbarMenu [
                        prop.id "chickencheck-navbar-menu"
                        prop.children [ 
                            Bulma.navbarEnd.div [
                                Bulma.navbarItem.div [
                                    prop.text Version.version
                                ]
                                match user with
                                | Anonymous -> logInLink
                                | ApiUser _ -> logOutLink
                            ]
                        ] 
                    ] 
                ] 
            ]
            content
        ]
    ]
    |> Render.htmlDocument
        
