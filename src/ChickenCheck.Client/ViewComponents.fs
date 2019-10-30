module ChickenCheck.Client.ViewComponents 

open Fable.React.Props
open Fable.React
open Fulma
open Fable.FontAwesome

let loading = 
    span 
        []
        [ Icon.icon [ Icon.Modifiers [ Modifier.TextColor Color.IsWhite ] ] [ Fa.i [ Fa.Size Fa.Fa3x; Fa.Solid.Spinner; Fa.Spin ] [] ] ]
    
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
