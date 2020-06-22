module ChickenCheck.Backend.Bundle

    // Autogenerated from build.fsx, do not change!

    open Feliz.ViewEngine
    let bundle =
        [
            Html.script [ prop.src "ChickenCheck.runtime.js" ]
            Html.script [ prop.src "ChickenCheck.vendor.base64-js.js" ]
            Html.script [ prop.src "ChickenCheck.vendor.buffer.js" ]
            Html.script [ prop.src "ChickenCheck.vendor.core-js.js" ]
            Html.script [ prop.src "ChickenCheck.vendor.css-loader.js" ]
            Html.script [ prop.src "ChickenCheck.vendor.fortawesome.js" ]
            Html.script [ prop.src "ChickenCheck.vendor.ieee754.js" ]
            Html.script [ prop.src "ChickenCheck.vendor.isarray.js" ]
            Html.script [ prop.src "ChickenCheck.vendor.style-loader.js" ]
            Html.script [ prop.src "ChickenCheck.vendor.turbolinks.js" ]
            Html.script [ prop.src "ChickenCheck.vendor.webpack.js" ]
            Html.script [ prop.src "ChickenCheck.app.js" ]
            Html.script [ prop.src "ChickenCheck.style.js" ]
        ]
