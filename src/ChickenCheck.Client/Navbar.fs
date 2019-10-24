module ChickenCheck.Client.Navbar

open Elmish
open Fulma
open Fable.React
open Fable.React.Props
open Fulma.Extensions.Wikiki

type Model =
    { IsMenuExpanded : bool }

type Msg =
    | ToggleMenu
    | Signout
    | ToggleReleaseNotes

[<RequireQualifiedAccess>]
type ExternalMsg =
    | Signout
    | ToggleReleaseNotes

type ComponentMsg =
    | External of ExternalMsg
    | Internal of Cmd<Msg>

let init() =
    { IsMenuExpanded = false }

let update (msg: Msg) (model: Model) : Model * ComponentMsg =
    match msg with
    | ToggleMenu -> 
        { model with IsMenuExpanded = not model.IsMenuExpanded }, Cmd.none |> Internal
    | Signout ->
        model, ExternalMsg.Signout |> External
    | ToggleReleaseNotes ->
        model, ExternalMsg.ToggleReleaseNotes |> External


let view (model: Model) dispatch =
    let toggleReleaseNotes _ = dispatch ToggleReleaseNotes 
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
                                [ Navbar.Item.Props [ OnClick toggleReleaseNotes ] ]
                                [ 
                                    sprintf "v%s" ReleaseNotes.version |> str
                                ] 
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
