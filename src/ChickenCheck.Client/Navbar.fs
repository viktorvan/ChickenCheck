module ChickenCheck.Client.Navbar

let toggleIsActive (classes: string) =
    if classes.Contains("is-active") then
        classes.Replace("is-active", "")
    else
        classes + " is-active"
let toggleNavbarMenu (browser: IBrowserService) =
    let burger = browser.GetElementById("chickencheck-navbar-burger")
    let menu = browser.GetElementById("chickencheck-navbar-menu")
    burger |> Option.iter (fun e -> e.className <- toggleIsActive e.className)
    menu |> Option.iter (fun e -> e.className <- toggleIsActive e.className)
        
