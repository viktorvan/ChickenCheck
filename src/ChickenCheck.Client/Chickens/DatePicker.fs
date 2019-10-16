module ChickenCheck.Client.DatePicker

open ChickenCheck.Domain
open Fulma
open Fable.React
open Fable.React.Props
open Fable.FontAwesome
open System

let view date onChangeDate =
    let onDateSet date =
        onChangeDate date
    let onDateChange delta =    
        onDateSet (Date.addDays delta date)
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
                                    date.ToDateTime().ToString("yyyy-MM-dd") |> Input.Value
                                ] 
                        ] 
                ]
            Level.item []
                [ 
                    dateButton nextDate Fa.Solid.CaretRight
                ] 
        ]