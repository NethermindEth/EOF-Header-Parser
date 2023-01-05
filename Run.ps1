Function Execute-Command ($commandTitle, $commandPath, $commandArguments)
{
  Try {
    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = $commandPath
    $pinfo.WorkingDirectory = $PSScriptRoot
    $pinfo.RedirectStandardError = $true
    $pinfo.RedirectStandardOutput = $true
    $pinfo.UseShellExecute = $false
    $pinfo.Arguments = $commandArguments
    $p = New-Object System.Diagnostics.Process
    $p.StartInfo = $pinfo
    $p.Start() | Out-Null
    [pscustomobject]@{
        commandTitle = $commandTitle
        stdout = $p.StandardOutput.ReadToEnd()
        stderr = $p.StandardError.ReadToEnd()
        ExitCode = $p.ExitCode
    }
    $p.WaitForExit()
  }
  Catch {
     exit
  }
}

Function Show-Notification ($ToastTitle, $ToastText) {
    [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] > $null
    $Template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)

    $RawXml = [xml] $Template.GetXml()
    ($RawXml.toast.visual.binding.text|where {$_.id -eq "1"}).AppendChild($RawXml.CreateTextNode($ToastTitle)) > $null
    ($RawXml.toast.visual.binding.text|where {$_.id -eq "2"}).AppendChild($RawXml.CreateTextNode($ToastText)) > $null

    $SerializedXml = New-Object Windows.Data.Xml.Dom.XmlDocument
    $SerializedXml.LoadXml($RawXml.OuterXml)

    $Toast = [Windows.UI.Notifications.ToastNotification]::new($SerializedXml)
    $Toast.Tag = "PowerShell"
    $Toast.Group = "PowerShell"
    $Toast.Priority = [Windows.UI.Notifications.ToastNotificationPriority]::High
    $Toast.ExpirationTime = [DateTimeOffset]::Now.AddHours(1)

    $Notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier("PowerShell")
    $Notifier.Show($Toast);
}

Function Run-Periodic-Task ($action, $interval) {
    while (1) {
        $action.Invoke()
        Start-Sleep -Seconds $interval
    }
}

Function Setup-Monitor ($action, $interval) {
    $VerificationResult = Execute-Command -commandTitle "Run All.input" -commandPath "dotnet" -commandArguments "run --no-build -c Release --Inputs https://raw.githubusercontent.com/holiman/txparse/main/eofparse/all.input"
    $VerificationResult.stdout | Out-File -FilePath "all.output"
    $DiffsProcessResult = Execute-Command -commandTitle "Run All.input" -commandPath "dotnet" -commandArguments "run --no-build -c Release --DiffFiles all.output https://raw.githubusercontent.com/holiman/txparse/main/eofparse/all.output"
    if($DiffsProcessResult.ExitCode -ne 0) {
        echo $DiffsProcessResult.stderr
        return
    }
    Show-Notification -ToastTitle "Diffing Result" -ToastText $DiffsProcessResult.stdout
}

Execute-Command -commandTitle "Run Build" -commandPath "dotnet" -commandArguments "build -c Release"
Run-Periodic-Task -action {Setup-Monitor} -interval 3600