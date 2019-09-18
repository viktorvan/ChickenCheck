module View


open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
open Fable.MaterialUI.MaterialDesignIcons
open Fable.MaterialUI.Icons
open ChickenCheck.Client
open Messages
open ChickenCheck.Client.Pages
open Elmish.React.Common
open ChickenCheck.Domain.Helpers
open Elmish
open Fulma

let view model dispatch =

    let pageHtml (page : Page) =
        match page with
        | SigninPage pageModel -> lazyView2 Session.Signin.view pageModel (SigninMsg >> dispatch)
        | ChickensPage pageModel -> lazyView2 Chicken.Index.view pageModel (IndexMsg >> ChickenMsg >> dispatch)

    let isLoggedIn, loggedInUsername =
        match model.Session with
        | None -> false, ""
        | Some session -> true, session.Name.Value

    let navbar =
        Navbar.navbar [ Navbar.Color IsInfo ]
            [ Navbar.Brand.div [ ]
                [ Navbar.Item.a [ Navbar.Item.Props [ Href "#" ] ]
                    [ img [ Style [ Width "2.5em" ] // Force svg display
                            Src "Icons/android-chrome-192x192.png" ] ]  
                  Navbar.Item.a [ Navbar.Item.Props [ Href "#" ] ]
                    [ str "Mina hönor" ] ] ]

    div [] 
        [ if isLoggedIn then
              yield navbar 
          yield div [ ] [ pageHtml model.CurrentPage ] ] 
