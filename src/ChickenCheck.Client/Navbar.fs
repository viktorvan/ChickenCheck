module ChickenCheck.Client.Navbar

let toggleNavbarMenu (browser: IBrowserService) =
    browser.GetElementById("chickencheck-navbar-burger")
    |> Option.iter (HtmlHelper.toggleClass "is-active")
    browser.GetElementById("chickencheck-navbar-menu")
    |> Option.iter (HtmlHelper.toggleClass "is-active")
        
