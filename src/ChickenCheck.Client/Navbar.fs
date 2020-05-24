module ChickenCheck.Client.Navbar

open Elmish
open Feliz
open Feliz.Bulma


type NavbarProps =
    { Model: Model
      Dispatch: Dispatch<Msg> }

let view = Utils.elmishView "Navbar" (fun props ->
    let dispatch = props.Dispatch
    let model = props.Model
    Bulma.navbar [
        color.isInfo
        prop.children [
            Bulma.navbarBrand.div [
                Bulma.navbarItem.div [
                    prop.text "ChickenCheck"
                ]
                Bulma.navbarBurger [
                    prop.onClick (fun _ -> dispatch ToggleMenu)
                    prop.className [ (model.IsMenuExpanded, "is-active"); (true, "navbar-burger") ]
                    prop.children (List.replicate 3 (Html.span []))
                ]
            ]
            Bulma.navbarMenu [
                Bulma.navbarEnd.a [
                    prop.onClick (fun _ -> dispatch Signout)
                    prop.children [
                        Bulma.navbarItem.div [
                            prop.text "sign out"
                        ]
                    ]
                ]
            ]
        ]
    ])
