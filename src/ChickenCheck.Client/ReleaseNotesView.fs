module ChickenCheck.Client.ReleaseNotesView

open Fulma.Extensions.Wikiki 
open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome

type ReleaseNotesProps = { IsActive: bool; ToggleReleaseNotes: Browser.Types.Event -> unit }
type ReleaseInfo = { Title: string; Updates: string list }

let private parseNotes (notes:string) =
    let lines = notes.Split System.Environment.NewLine |> Array.toList

    let folder =
        let noteLineChars = [ '-'; '*'; '+' ]
        let headerLineChars = [ '#' ]

        let startsWith (chars: char list) (str: string) = 
            (false, chars) ||> List.fold (fun state (c: char) -> state || str.StartsWith c)

        let trimStart (chars: char list) (line: string) =
            (line, chars) ||> List.fold (fun state (c: char) -> state.TrimStart c)

        let headerParser = startsWith headerLineChars, trimStart headerLineChars
        let noteParser = startsWith noteLineChars, trimStart noteLineChars

        let tryParse (predicate, parser) line =
            if line |> predicate then
                true, line |> parser
            else false, ""

        fun (releases, currentTitle) (line: string) ->
            match tryParse noteParser line with
            | true, note ->
                (releases @ [ currentTitle, note ]), currentTitle
            | false, _ ->
                match tryParse headerParser line with
                | true, newTitle ->
                    releases, newTitle
                | false, _ -> releases, currentTitle

    (([], ""), lines) ||> List.fold folder
    |> fst
    |> List.groupBy fst
    |> List.map (fun (title, notes) -> { Title = title; Updates = notes |> List.map snd })

let private releaseNotesView notes =
    let updates =
        parseNotes notes
        |> List.map 
            (fun info -> 
                let title = Heading.h5 [] [ str info.Title ]
                let updates =
                    info.Updates
                    |> List.map (fun u -> p [] [ str u ])
                [ 
                    Card.header [] [ title ]
                    Card.content [] updates 
                ]
                |> Card.card [])
    updates |> div []

let view { IsActive = isActive; ToggleReleaseNotes = toggleReleaseNotes } =
    Quickview.quickview [ Quickview.IsActive isActive ]
        [ Quickview.header []
            [ Quickview.title [ ] [ Heading.h2 [] [ str "Release Notes" ] ]
              Delete.delete [ Delete.OnClick toggleReleaseNotes ] [ ] 
            ]
          Quickview.body [ ]
            [ releaseNotesView ReleaseNotes.notes ]
          Quickview.footer [ ]
            [] 
        ]