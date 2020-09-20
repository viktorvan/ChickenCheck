module ChickenCheck.Backend.Views.App

open ChickenCheck.Backend
open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine
open ChickenCheck.Shared
open ChickenCheck.Backend.Extensions

let layout csrfTokenInput basePath domain  content =
//    let logInLink =
//        Bulma.navbarItem.a [
//            prop.disableTurbolinks
//            prop.href "/login"
//            prop.text "log in" ]
//
//    let logOutLink (username: string) =
//        Bulma.navbarItem.div [
//            navbarItem.hasDropdown
//            navbarItem.isHoverable
//            prop.children [
//                Bulma.navbarLink.a username
//                Bulma.navbarDropdown.div [
//                    Bulma.navbarItem.a [
//                        prop.disableTurbolinks
//                        prop.href "/logout"
//                        prop.text "log out"
//                    ]
//                ]
//            ]
//        ]
    
//    let userAttribute =
//        let userStr =
//            match user with
//            | ApiUser { Name = name } -> sprintf "ApiUser:%s" name
//            | Anonymous -> "Anonymous"
//        prop.custom (DataAttributes.User, userStr)

    let githubLink =
        Html.a [
            prop.href "https://github.com/viktorvan/chickencheck"
            prop.children [
                Bulma.icon [
                    prop.children [
                        Html.i [
                            prop.classes [ "fab fa-fw fa-github" ]
                        ]
                    ]
                ]
            ]
        ]
        
    Html.html [
        Html.head [
            Html.base' [
                prop.href basePath
            ]
            Html.meta [
                prop.charset.utf8
            ]
            Html.meta [
                prop.content "width=device-width, initial-scale=1"
                prop.name "viewport"
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
                prop.href "Icons/site.json"
            ]
            Html.script [
                prop.async true
                prop.defer true
                prop.custom ("data-domain", domain)
                prop.src "https://plausible.io/js/plausible.js"
            ]
            Html.script [
                Html.text "window.plausible = window.plausible || function() { (window.plausible.q = window.plausible.q || []).push(arguments) }"
            ]
            yield! Bundle.bundle
        ]
        Html.body [
//            userAttribute
            prop.children [
                csrfTokenInput
                Bulma.navbar [
                    prop.id "chickencheck-navbar"
                    prop.custom ("data-turbolinks-permanent", "")
                    color.isInfo
                    prop.children [ 
                        Bulma.navbarBrand.div [
                            prop.children [
                                Bulma.navbarItem.a [
                                    prop.href "/eggs"
                                    prop.children [
                                        Html.img [
                                            prop.src "/Icons/android-chrome-512x512.png"
                                            prop.alt "Icon"
                                            prop.style [ style.width (length.px 28); style.height (length.px 28)]
                                        ]
                                        Html.text "ChickenCheck"
                                    ]
                                ]
                                Bulma.navbarBurger [
                                    prop.id "chickencheck-navbar-burger"
                                    navbarItem.hasDropdown
                                    prop.children [ yield! List.replicate 3 (Html.span []) ] 
                                ] 
                            ]
                        ]
                        Bulma.navbarMenu [
                            prop.id "chickencheck-navbar-menu"
//                            prop.children [ 
//                                Bulma.navbarEnd.div [
//                                    match user with
//                                    | Anonymous -> logInLink
//                                    | ApiUser { Name = name } -> logOutLink name
//                                ]
//                            ] 
                        ] 
                    ] 
                ]
                content
                Bulma.footer [
                    githubLink
                    Html.span [
                        Html.text "Version: "
                        Html.a [
                            prop.href "https://github.com/viktorvan/ChickenCheck/blob/master/CHANGELOG.md"
                            prop.target.blank
                            prop.text Version.version ]
                        ]
                ]
            ]
        ]
    ]
    |> Render.htmlDocument
        
