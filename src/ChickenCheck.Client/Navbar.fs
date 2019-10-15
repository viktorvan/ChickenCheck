module ChickenCheck.Client.Navbar

open Elmish
open Fulma
open Fable.React
open Fable.React.Props

type Model =
    { IsMenuExpanded : bool }

type Msg =
    | ToggleMenu
    | Signout

[<RequireQualifiedAccess>]
type ExternalMsg =
    | NoOp
    | Signout

let init() =
    { IsMenuExpanded = false }

let update (msg: Msg) (model: Model) : Model * ExternalMsg =
    match msg with
    | ToggleMenu -> 
        { model with IsMenuExpanded = not model.IsMenuExpanded }, ExternalMsg.NoOp

    | Signout ->
        model, ExternalMsg.Signout

let view (model: Model) dispatch =
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
                    Navbar.Item.a []
                        [ 
                            str "Mina hönor" 
                        ] 
                    Navbar.burger 
                        [ 
                            Props 
                                [ 
                                    OnClick (fun _ -> (dispatch ToggleMenu) )
                                    classList [ "is-active", model.IsMenuExpanded; "navbar-burger", true ] 
                                ] 
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
                                    Navbar.Item.Props [ OnClick (fun _ -> dispatch Signout) ]
                                ]
                                [
                                    str "sign out" 
                                ]
                        ]
                ]
        ]
