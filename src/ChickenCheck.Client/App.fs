module App

type IExports =
    abstract Log : string -> unit

let say : IExports =
    { new IExports with
        member this.Log(msg) = printf "%s" msg }


printfn "loaded"
say.Log("testing")
