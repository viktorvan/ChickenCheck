module ChickenCheck.Client.Utils

let private isDevelopment =
    #if DEBUG
    true
    #else
    false
    #endif

module Log =
    let developmentMessage str =
        if isDevelopment
        then Browser.Dom.console.log(str)
    let developmentError str =
        if isDevelopment
        then Browser.Dom.console.error(str)
    let developmentException (error: exn) =
        if isDevelopment
        then Browser.Dom.console.error(error)
