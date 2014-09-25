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

module DriveManager =
    open NLog
    let logger = LogManager.GetLogger("DriveManager")

    type Partition = {device:string; size:string; filesystem:string}
    type Drive = {device:string; size:string; name:string; partitions:Partition list}

    let parseDrive (lines:string list) =
        let lines' =
            match lines with  // We don't need the first line of the parted output
            | x::xs when x = "BYT;" -> xs
            | _ -> lines

        let driveLine = List.head lines'
        let partitionLines = List.tail lines'

        logger.Debug(sprintf "Parsing drive: %s" driveLine)

        let drive =
            let splits = driveLine.Split(':')
            {
                Drive.device = splits.[0];
                size = splits.[1];
                name = splits.[6];
                partitions = []
            }

        let partitions =
            partitionLines
            |> List.map (fun partitionLine ->
                logger.Debug(sprintf "Parsing partition: %s" partitionLine)
                let splits = partitionLine.Split(':')
                {
                    Partition.device = drive.device + splits.[0];
                    size = splits.[3];
                    filesystem = splits.[4]
                }
            )

        {drive with partitions=partitions}

    let parseDrives (driveList:string) =
        driveList.Split([|System.Environment.NewLine|], System.StringSplitOptions.None)
        |> List.ofArray
        |> List.fold (fun drives line ->
            let (current, done_) =
                match drives with
                | [] -> ([], [])
                | x::xs -> (x,xs)

            match line with
            | "" -> []::current::done_  // End of current drive. Prepend a new one to the list.
            | _ -> (List.append current [line])::done_
        ) []
        |> List.map parseDrive

    let getDrives () =
        CLI.runCmd "parted -lm"
        |> (fun res ->
            match res with
            | CLI.Failure(err,_) ->
                logger.Fatal(sprintf "Could not list drives!\n\n%s" err)
                []
            | CLI.Success output ->
                parseDrives output
        )

module main =
    open NLog
    LoggerConfig.setLogLevel LogLevel.Debug
    let logger = LogManager.GetLogger("main")

    [<EntryPoint>]
    let main args =
        VirtualBox.createVM "runner"
        DriveManager.getDrives()
        |> printfn "%A"
        VirtualBox.deleteVM "runner"
        0
