module ChickenCheck.Client.Navbar

open ChickenCheck.Shared
open Feliz
open Feliz.Bulma
open Feliz.UseElmish
open ChickenCheck.Client.Contexts
open Elmish

type Model =
    { IsMenuExpanded: bool }
type Msg =
    | ToggleMenu

let logInLink =
    Bulma.navbarEnd.a
        [ prop.href (Url.LogIn None).Href
          prop.children [ Bulma.navbarItem.div [ prop.text "log in" ] ] ]

let logOutLink =
    Bulma.navbarEnd.a
        [ prop.href Url.LogOut.Href
          prop.children [ Bulma.navbarItem.div [ prop.text "log out" ] ] ]
  
let init() = { IsMenuExpanded = false }, Cmd.none
        
let update msg model =
    match msg with
    | ToggleMenu -> { model with IsMenuExpanded = not model.IsMenuExpanded }, Cmd.none

let navbar =
    React.functionComponent("Navbar",
        fun () ->
            let user = React.useContext (userContext)
            let model, dispatch = React.useElmish (init, update, [| |])
            Bulma.navbar [ 
                color.isInfo
                prop.children [ 
                    Bulma.navbarBrand.div [ 
                        Bulma.navbarItem.div [ prop.text "ChickenCheck" ]
                        Bulma.navbarBurger [ 
                            prop.onClick (fun _ -> dispatch ToggleMenu)
                            navbarItem.hasDropdown
                            if model.IsMenuExpanded then navbarBurger.isActive
                            prop.children [ yield! List.replicate 3 (Html.span []) ] 
                        ] 
                    ]
                    Bulma.navbarMenu [ 
                        if model.IsMenuExpanded then navbarMenu.isActive
                        prop.children [ 
                            match user with
                            | Anonymous -> logInLink
                            | ApiUser _ -> logOutLink 
                        ] 
                    ] 
                ] 
            ])
