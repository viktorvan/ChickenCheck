module ChickenCheck.Client.ViewComponents 

open Fable.MaterialUI
module Mui = Fable.MaterialUI.Core
open Fable.React.Props
open Fable.MaterialUI.Icons
open Fable.MaterialUI.MaterialDesignIcons
open Fable.React
open Fable.Core.JsInterop
open ChickenCheck.Client
open ChickenCheck.Domain
open Elmish
open ChickenCheck.Client.ApiHelpers
open ChickenCheck.Client.Pages

let loading = Mui.circularProgress [] 

let apiErrorMsg clearAction classNames status =
    let (isOpen, msg) = 
        match status with
        | NotStarted | ApiCallStatus.Completed | Running -> false, ""
        | Failed msg -> true, msg
    Mui.snackbar 
        [ Open isOpen
          SnackbarProp.AnchorOrigin { vertical = SnackbarVerticalOrigin.Bottom; horizontal = SnackbarHorizontalOrigin.Right } ] 
        [ Mui.snackbarContent 
            [ Message (str msg)
              Class classNames
              SnackbarProp.Action (Mui.iconButton [ OnClick clearAction ] [ closeIcon [] ]) ] [] ]

let centered content =
    Mui.grid 
        [ Container true 
          GridProp.Direction GridDirection.Column
          GridProp.AlignItems GridAlignItems.Center
          Justify GridJustify.Center ] 
        (List.map (fun item -> Mui.grid [ Item true ] [ item ]) content)

let private textField' name label autoFocus onChange (isValid, value) =
    Mui.textField 
        [ OnChange onChange
          TextFieldProp.Variant TextFieldVariant.Outlined 
          MaterialProp.Margin FormControlMargin.Normal
          Required true
          FullWidth true
          Id name 
          Label label
          Name name
          AutoFocus autoFocus
          MaterialProp.Error (not isValid)
          Value value
          ] []

let shortTextField name label autoFocus onChange (inputValue: StringInput<String200>) = 
    StringInput.tryValid inputValue
    |> textField' name label autoFocus onChange

let optionalLongTextField name label autoFocus onChange (inputValue: StringInput<String1000>) =
    OptionalStringInput.tryValid inputValue
    |> textField' name label autoFocus onChange

let datePicker name onChange (date: System.DateTime) =
    Mui.textField 
        [ OnChange (fun ev -> ev.Value |> onChange)
          Id name
          Label name
          Type "date" 
          Value (date.ToString("yyyy-MM-dd")) ] []
