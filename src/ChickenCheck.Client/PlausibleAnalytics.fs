module ChickenCheck.Client.PlausibleAnalytics

open Fable.Core
    
[<Emit("plausible($0)")>]
let raiseCustomEvent (name: string) : unit = jsNative
