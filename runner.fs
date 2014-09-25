namespace Runner

[<AutoOpen>]
module Helpers =
    let (@@) fn x = fn x

module CLI =
    type Status =
    | Success of string
    | Failure of (string * int)

    let runCmd (cmd:string) =
        let proc = new System.Diagnostics.Process()

        // Escape quotes in the user command
        let saniCmd = cmd.Replace("\"", "\\\"")

        // Set up and run the command
        proc.StartInfo.FileName <- "/bin/bash"
        proc.StartInfo.Arguments <- "-c \"" + saniCmd + "\""
        proc.StartInfo.UseShellExecute <- false 
        proc.StartInfo.RedirectStandardOutput <- true
        proc.Start() |> ignore

        // Capture output
        let sb = new System.Text.StringBuilder()
        while not proc.HasExited do
            sb.Append(proc.StandardOutput.ReadToEnd()) |> ignore
        sb.Append(proc.StandardOutput.ReadToEnd()) |> ignore  // Do it again in case the process exits so quickly we never enter the loop

        let output = sb.ToString().Trim()

        match proc.ExitCode with
        | 0 -> Success output
        | _ -> Failure (output, proc.ExitCode)

module main =
    [<EntryPoint>]
    let main args =
        CLI.runCmd """echo "Hello, World!" """
        |> printfn "%A"
        0
