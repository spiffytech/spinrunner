namespace Runner

[<AutoOpen>]
module Helpers =
    let (@@) fn x = fn x

module LoggerConfig =
    open NLog
    open NLog.Config
    open NLog.Targets

    let setLogLevel level =
        let config = new LoggingConfiguration()
        let consoleTarget = new ColoredConsoleTarget()
        config.AddTarget("console", consoleTarget)
        let rule = new LoggingRule("*", level, consoleTarget)
        config.LoggingRules.Add rule
        LogManager.Configuration <- config

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
        proc.StartInfo.RedirectStandardError <- true
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

module VirtualBox =
    open NLog
    let logger = LogManager.GetLogger("VirtualBox")

    let createVM name =
        CLI.runCmd @@ sprintf "VBoxManage createvm --name '%s' --register" name
        |> printfn "%A"

        logger.Info(sprintf "Created VM %s" name)

    let deleteVM name =
        CLI.runCmd @@ sprintf "VBoxManage unregistervm '%s' --delete" name
        |> printfn "%A"

        logger.Info(sprintf "Deleted VM %s" name)

module main =
    open NLog
    LoggerConfig.setLogLevel LogLevel.Debug
    let logger = LogManager.GetLogger("main")

    [<EntryPoint>]
    let main args =
        VirtualBox.createVM "runner"
        VirtualBox.deleteVM "runner"
        0
