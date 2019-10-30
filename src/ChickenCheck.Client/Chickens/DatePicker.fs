module ChickenCheck.Client.DatePicker

open ChickenCheck.Client
open ChickenCheck.Domain
open Fulma
open Fable.React
open Fable.React.Props
open Fable.FontAwesome
open System

type DatePickerProps =
    { CurrentDate : Date
      OnChangeDate : Date -> unit }
let view = Utils.elmishView "DatePicker" (fun (props : DatePickerProps) ->
    let onDateSet date =
        props.OnChangeDate date
    let onDateChange delta =    
        onDateSet (Date.addDays delta props.CurrentDate)
    let previousDate _ = onDateChange -1.
    let nextDate _ = onDateChange 1.

    let parseDate (ev: Browser.Types.Event) =
        ev.Value
        |> DateTime.Parse
        |> Date.create

    let dateButton onClick icon =
        Button.a 
            [ 
                Button.IsLink
                Button.OnClick onClick
                Button.Size IsLarge 
            ] 
            [ 
                Icon.icon [] 
                    [ Fa.i 
                        [ 
                            Fa.Size Fa.Fa3x
                            icon
                        ] 
                        [] 
                    ] 
            ]

    Level.level [ Level.Level.IsMobile ]
        [ 
            Level.item []
                [ 
                    dateButton previousDate Fa.Solid.CaretLeft
                ]
            Level.item []
                [ 
                    Field.div 
                        [ 
                            Field.Props [ Style [ Width "100%" ] ] 
                        ]
                        [ 
                            Input.date
                                [ 
                                    Input.OnChange (parseDate >> onDateSet) 
                                    props.CurrentDate.ToDateTime().ToString("yyyy-MM-dd") |> Input.Value
                                ] 
                        ] 
                ]
            Level.item []
                [ 
                    dateButton nextDate Fa.Solid.CaretRight
                ] 
        ]
    )