module ChickenCheck.Client.HtmlHelper

open Browser.Types

let toggleClass className (e: Element) =
    let newClasses =
        if e.className.Contains(className) then
            e.className.Replace(className, "")
        else
            e.className + " " + className
    e.className <- newClasses
        
