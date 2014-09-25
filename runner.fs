let proc = new System.Diagnostics.Process()
proc.StartInfo.FileName <- "/bin/bash"
proc.StartInfo.Arguments <- "-c \"" + "echo 'hello, world'" + "\""
proc.StartInfo.UseShellExecute <- false 
proc.StartInfo.RedirectStandardOutput <- true
proc.Start()
let sb = new System.Text.StringBuilder()
while not proc.HasExited do
    sb.Append(proc.StandardOutput.ReadToEnd())
sb.Append(proc.StandardOutput.ReadToEnd())  // Do it again in case the process exits so quickly we never enter the loop
printfn "%A" sb
