module Chickens

open ChickenCheck.Client

open Browser
open ChickenCheck.Shared
open Fable.Core.JsInterop
open Turbolinks

TurbolinksLib.start ()
TurbolinksLib.setProgressBarDelay 100

let initAddEggHandlers currentDate =
    let chickenCards = document.querySelectorAll (sprintf "[%s]" DataAttributes.ChickenCard)
    for i = 0 to chickenCards.length - 1 do
        let card = chickenCards.[i]
        let id: string = card?dataset?chickenCard
        card.onpointerdown <- (fun _ -> CompositionRoot.addEgg id currentDate)

let initRemoveEggHandlers currentDate =
    let eggIcons = document.querySelectorAll (sprintf "[%s]" DataAttributes.EggIcon)
    for i = 0 to eggIcons.length - 1 do
        let egg = eggIcons.[i]
        let id: string = egg?dataset?eggIcon
        egg.onpointerdown <- (fun _ -> CompositionRoot.removeEgg id currentDate)

let init _ =
    let currentDate: string = document.querySelector(sprintf "[%s]" DataAttributes.CurrentDate)?dataset?currentDate
    initAddEggHandlers currentDate
    initRemoveEggHandlers currentDate


document.addEventListener ("turbolinks:load", init)
