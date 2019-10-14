module ChickenCheck.Client.ViewComponents 

open Fable.React.Props
open Fable.React
open Fulma

let loading = str "loading"

let apiErrorMsg clearError msg =
    Notification.notification 
        [
            Notification.Color IsDanger
        ]
        [
            Button.button 
                [ 
                    Button.OnClick clearError
                    Button.Props [ Class "delete" ] 
                ]
                [] 
            Heading.h6 [] [ str "Någonting gick fel" ]
            Text.p [] [ str msg ]
        ]

let centered content =
    div []
        content
