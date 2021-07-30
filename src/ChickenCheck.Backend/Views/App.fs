module ChickenCheck.Backend.Views.App

open ChickenCheck.Backend
open Giraffe.ViewEngine
open ChickenCheck.Backend.Extensions



let layout csrfToken basePath domain (user: User) content =
    let logInLink =
        a [ _href "/login"; _class "navbar-item" ] [ str "log in" ]

    let logOutLink (username: string) =
        div 
            [ _class "navbar-item has-dropdown is-hoverable" ] 
            [
                a [ _class "navbar-item" ] [ str username ]
                div [ _class "navbar-dropdown" ] [ a [ _class "navbar-item"; _href "/logout" ] [ str "log out" ] ]
            ]
    
    let userAttribute =
        let userStr =
            match user with
            | ApiUser { Name = name } -> sprintf "ApiUser:%s" name
            | Anonymous -> "Anonymous"
        attr DataAttributes.User userStr
        
    let base' = voidTag "base"

    let githubLink =
        a 
            [ _href "https://github.com/viktorvan/chickencheck" ]
            [
                span 
                    [ _class "icon" ]
                    [ i [ _class "fab fa-fw fa-github" ] [ ] ]
            ]
        
    html 
        [] 
        [
            head
                []
                [
                    base' [
                        _href basePath
                    ]
                    meta [
                        _charset "UTF-8"
                    ]
                    meta [
                        _content "width=device-width, initial-scale=1"
                        _name "viewport"
                    ]
                    title [] [ str "ChickenCheck" ]
                    link [
                        _rel "stylesheet"
                        _href "https://cdn.jsdelivr.net/npm/bulma@0.9.3/css/bulma.min.css"
                    ]
                    link [
                        _rel "stylesheet"
                        _href "https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.13.0/css/all.min.css"
                    ]
                    link [
                        _rel "apple-touch-icon"
                        _sizes "180x180"
                        _href "Icons/apple-touch-icon.png"
                    ]
                    link [
                        _rel "icon" 
                        _type "image/png" 
                        _sizes "32x32" 
                        _href "Icons/favicon-32x32.png"
                    ]
                    link [
                        _rel "icon"
                        _type "image/png"
                        _sizes "16x16"
                        _href "Icons/favicon-16x16.png" 
                    ]
                    link [ 
                        _rel "manifest" 
                        _href "Icons/site.json"
                    ]
                    script [
                        _src "https://unpkg.com/htmx.org@1.5.0"
                        _integrity "sha384-oGA+prIp5Vchu6we2YkI51UtVzN9Jpx2Z7PnR1I78PnZlN8LkrCT4lqqqmDkyrvI"
                        _crossorigin "anonymous"
                    ] []
                    script [
                        _async
                        _defer
                        attr "data-domain" domain
                        _src "https://plausible.io/js/plausible.js"
                    ] []
                    script [ ] [str "window.plausible = window.plausible || function() { (window.plausible.q = window.plausible.q || []).push(arguments) }"]
                ]
            body [ userAttribute ]
                [
                    script [] 
                        [ rawText $"""document.body.addEventListener('htmx:configRequest', function(evt) {{ evt.detail.headers['RequestVerificationToken'] = '%s{csrfToken}'; }});""" ]
                    nav [ _class "navbar is-info"; _id "chickencheck-navbar" ]
                        [
                            div 
                                [ _class "navbar-brand" ] 
                                [
                                    a
                                        [ _class "navbar-item"; _href "/eggs" ]
                                        [
                                            img [
                                                _src "/Icons/android-chrome-512x512.png"
                                                _alt "Icon"
                                                _style "width: 28px; height: 28px;"
                                            ]
                                            str "ChickenCheck"
                                        ]
                                    a 
                                        [ _id "chickencheck-navbar-burger" ] 
                                        [ yield! List.replicate 3 (span [] []) ]  
                                ]
                            div [ _class "navbar-menu"; _id "chickencheck-navbar-menu" ]
                                [
                                    div [ _class "navbar-end" ] [
                                        match user with
                                        | Anonymous -> logInLink
                                        | ApiUser { Name = name } -> logOutLink name
                                    ]
                                ] 
                        ] 
                    content
                    footer [] 
                        [
                            githubLink
                            span [] 
                                [
                                    str "Version: "
                                    a 
                                        [ _href "https://github.com/viktorvan/ChickenCheck/blob/master/CHANGELOG.md"; _target "blank" ]
                                        [ str Version.version ]
                                ]
                        ]
                ]
        ]
    |> RenderView.AsString.htmlDocument
        
