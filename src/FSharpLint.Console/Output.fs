module FSharpLint.Console.Output

open System
open FSharp.Compiler.Text
open FSharpLint.Framework

type IOutput =
    /// Outputs informational text.
    abstract member WriteInfo : string -> unit
    /// Outputs a lint warning.
    abstract member WriteWarning : Suggestion.LintWarning -> unit
    /// Outputs an unexpected error in linting.
    abstract member WriteError : string -> unit

type StandardOutput () =

    let getErrorMessage (range:Range) =
        let error = Resources.GetString("LintSourceError")
        String.Format(error, range.StartLine, range.StartColumn)

    let highlightErrorText (range:Range) (errorLine:string) =
        let highlightColumnLine =
            if String.length errorLine = 0 then "^"
            else
                errorLine
                |> Seq.mapi (fun i _ -> if i = range.StartColumn then "^" else " ")
                |> Seq.reduce (+)
        $"{getErrorMessage range}{Environment.NewLine}{errorLine}{Environment.NewLine}{highlightColumnLine}"

    let writeLine (str:string) (color:ConsoleColor) (writer:IO.TextWriter) =
        let originalColour = Console.ForegroundColor
        Console.ForegroundColor <- color
        writer.WriteLine str
        Console.ForegroundColor <- originalColour

    interface IOutput with

        member _.WriteInfo (info:string) = writeLine info ConsoleColor.White Console.Out
        member this.WriteWarning (warning:Suggestion.LintWarning) =
            let highlightedErrorText = highlightErrorText warning.Details.Range warning.ErrorText
            let ruleUrlHint = $"See https://fsprojects.github.io/FSharpLint/how-tos/rules/%s{warning.RuleIdentifier}.html"
            let str = $"{warning.Details.Message}{Environment.NewLine}{highlightedErrorText}{Environment.NewLine}{ruleUrlHint}"
            writeLine str ConsoleColor.Yellow Console.Out
            String.replicate 80 "-" |> (this :> IOutput).WriteInfo
        member _.WriteError (error:string) =  writeLine error ConsoleColor.Red Console.Error

type MSBuildOutput () =

    interface IOutput with

        member _.WriteInfo (info:string) = Console.Out.WriteLine info
        member _.WriteWarning (warning:Suggestion.LintWarning) =
            fprintf Console.Out "%s(%d,%d,%d,%d):FSharpLint warning %s: %s"
                <| warning.FilePath
                <| warning.Details.Range.StartLine
                <| warning.Details.Range.StartColumn
                <| warning.Details.Range.EndLine
                <| warning.Details.Range.EndColumn
                <| warning.RuleIdentifier
                <| warning.Details.Message
        member _.WriteError (error:string) =
            $"FSharpLint error: {error}"
            |> Console.Error.WriteLine