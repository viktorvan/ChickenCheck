module ChickenCheck.Client.Navbar

open ChickenCheck.Client.Utils
open Elmish
open Fulma
open Fable.React
open Fable.React.Props

type NavbarProps =
    { Model: Model
      Dispatch: Dispatch<Msg> }

let view = elmishView "Navbar" (fun (props: NavbarProps) ->
    let dispatch = props.Dispatch
    let model = props.Model
    Navbar.navbar 
        [ 
            Navbar.Color IsInfo 
        ]
        [ 
            Navbar.Brand.div [ ]
                [ 
                    Navbar.Item.a []
                        [ 
                            img 
                                [ 
                                    Style [ Width "2.5em" ] // Force svg display
                                    Src "https://chickencheck.z6.web.core.windows.net/Icons/android-chrome-192x192.png" 
                                ] 
                        ]  
                    Navbar.burger 
                        [ 
                            // Props 
                            //     [ 
                            //         OnClick (fun _ -> (dispatch ToggleMenu) )
                            //         classList [ "is-active", model.IsMenuExpanded; "navbar-burger", true ] 
                            //     ] 
                        ] 
                        [
                            span [] []
                            span [] []
                            span [] []
                        ]
                ] 
            Navbar.menu [ Navbar.Menu.IsActive model.IsMenuExpanded ]
                [
                    Navbar.End.div []
                        [
                            Navbar.Item.a 
                                [
                                    Navbar.Item.Props [ OnClick (fun _ -> Signout |> dispatch) ]
                                ]
                                [
                                    str "sign out" 
                                ]
                        ]
                ]
        ] )
