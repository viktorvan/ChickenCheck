module ChickenCheck.Backend.Views.Shared

open Giraffe.ViewEngine

let loading =
    span 
        [ _class "icon has-text-white" ]
        [ i [ _class "fa-3x fas fa-spinner fa-spin" ] [ ] ]
        
let error =
    span 
        [ _class "icon has-text-white" ]
        [ i [ _class "fa-3x fas fa-exclamation-circle" ] [ ] ]
