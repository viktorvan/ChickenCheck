module ChickenCheck.Client.Navbar

open Elmish
open Feliz
open Feliz.Bulma

type NavbarProps =
    { Model: Model
      Dispatch: Dispatch<Msg> }

let view = (fun model dispatch ->
    Bulma.navbar [
        color.isInfo
        prop.children [
            Bulma.navbarBrand.div [
                Bulma.navbarItem.div [
                    prop.text "ChickenCheck"
                ]
                Bulma.navbarBurger [
                    prop.onClick (fun _ -> dispatch ToggleMenu)
                    prop.className [ if model.IsMenuExpanded then "is-active"; "navbar-burger" ]
                    prop.children (List.replicate 3 (Html.span []))
                ]
            ]
            Bulma.navbarMenu [
                Bulma.navbarEnd.a [
                    prop.onClick (fun _ -> dispatch Logout)
                    prop.children [
                        Bulma.navbarItem.div [
                            prop.text "sign out"
                        ]
                    ]
                ]
            ]
        ]
    ])
