#Requires -Version 7.0
<#
.SYNOPSIS
    AspireProcessManager - Cross-platform PowerShell module for managing .NET Aspire App Host processes.

.DESCRIPTION
    Provides functions to find, inspect, and stop .NET Aspire App Host processes.
    Works on macOS, Linux, and Windows.
#>

#region Private Helpers

function Get-ProcessCommandLine {
    <#
    .SYNOPSIS
        Gets the full command line string for a given process, cross-platform.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [System.Diagnostics.Process]$Process
    )

    try {
        if ($IsWindows) {
            $wmi = Get-CimInstance Win32_Process -Filter "ProcessId = $($Process.Id)" -ErrorAction Stop
            return $wmi.CommandLine
        }
        elseif ($IsMacOS) {
            $result = & ps -p $Process.Id -o args= 2>$null
            return $result -join ' '
        }
        elseif ($IsLinux) {
            $cmdlineFile = "/proc/$($Process.Id)/cmdline"
            if (Test-Path $cmdlineFile) {
                $raw = [System.IO.File]::ReadAllText($cmdlineFile)
                # Arguments are null-byte separated
                return $raw.Replace("`0", " ").Trim()
            }
        }
    }
    catch {
        Write-Verbose "Could not read command line for PID $($Process.Id): $_"
    }

    return $null
}

function Get-ChildProcesses {
    <#
    .SYNOPSIS
        Returns all direct and transitive child processes of the given PID.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [int]$ParentId
    )

    $allProcesses = Get-Process -ErrorAction SilentlyContinue
    $children = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()

    foreach ($proc in $allProcesses) {
        try {
            $parentPid = $null

            if ($IsWindows) {
                $wmi = Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.Id)" -ErrorAction Stop
                $parentPid = $wmi.ParentProcessId
            }
            elseif ($IsMacOS) {
                $ppidStr = & ps -p $proc.Id -o ppid= 2>$null
                $parentPid = [int]($ppidStr.Trim())
            }
            elseif ($IsLinux) {
                $statFile = "/proc/$($proc.Id)/stat"
                if (Test-Path $statFile) {
                    $stat = [System.IO.File]::ReadAllText($statFile)
                    # Format: pid (name) state ppid ...
                    $parts = $stat -split '\s+'
                    $parentPid = [int]$parts[3]
                }
            }

            if ($parentPid -eq $ParentId) {
                $children.Add($proc)
                # Recurse for grandchildren
                $grandChildren = Get-ChildProcesses -ParentId $proc.Id
                $children.AddRange($grandChildren)
            }
        }
        catch {
            # Skip processes we can't inspect
        }
    }

    return $children
}

#endregion

#region Public Functions

function Find-AspireAppHost {
    <#
    .SYNOPSIS
        Finds running .NET Aspire App Host processes.

    .DESCRIPTION
        Searches all running 'dotnet' processes (and optionally other names) for those
        whose command line matches the Aspire AppHost pattern (*AppHost*.dll or similar).

    .PARAMETER NameFilter
        Optional substring to further filter by project/assembly name (e.g. "MyApp").

    .PARAMETER ProcessName
        The OS process name to search. Defaults to 'dotnet'. Use '*' to search all.

    .OUTPUTS
        [PSCustomObject[]] with Id, ProcessName, CommandLine, StartTime properties.

    .EXAMPLE
        Find-AspireAppHost

    .EXAMPLE
        Find-AspireAppHost -NameFilter "MyApp" -Verbose
    #>
    [CmdletBinding()]
    param(
        [string]$NameFilter = '',
        [string]$ProcessName = 'dotnet'
    )

    Write-Verbose "Searching for Aspire AppHost processes (ProcessName='$ProcessName', NameFilter='$NameFilter')..."

    $candidates = if ($ProcessName -eq '*') {
        Get-Process -ErrorAction SilentlyContinue
    }
    else {
        Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
    }

    if (-not $candidates) {
        Write-Verbose "No '$ProcessName' processes found."
        return
    }

    $results = foreach ($proc in $candidates) {
        $cmdLine = Get-ProcessCommandLine -Process $proc
        Write-Verbose "PID $($proc.Id): $cmdLine"

        $isAppHost = $cmdLine -match 'AppHost'

        if ($isAppHost -and ($NameFilter -eq '' -or $cmdLine -match [regex]::Escape($NameFilter))) {
            [PSCustomObject]@{
                Id          = $proc.Id
                ProcessName = $proc.ProcessName
                StartTime   = $proc.StartTime
                CommandLine = $cmdLine
                Process     = $proc
            }
        }
    }

    if (-not $results) {
        Write-Warning "No Aspire AppHost processes found. Try -Verbose to see all candidates, or -ProcessName '*' to search all processes."
    }

    return $results
}

function Get-AspireProcessInfo {
    <#
    .SYNOPSIS
        Displays detailed information about running Aspire AppHost processes,
        including their child processes and network ports.

    .PARAMETER NameFilter
        Optional substring to filter by project/assembly name.

    .EXAMPLE
        Get-AspireProcessInfo

    .EXAMPLE
        Get-AspireProcessInfo -NameFilter "MyApp"
    #>
    [CmdletBinding()]
    param(
        [string]$NameFilter = ''
    )

    $hosts = Find-AspireAppHost -NameFilter $NameFilter -Verbose:$false

    if (-not $hosts) {
        Write-Host "No Aspire AppHost processes are currently running." -ForegroundColor Yellow
        return
    }

    foreach ($appHost in $hosts) {
        Write-Host "`n=== Aspire AppHost ===" -ForegroundColor Cyan
        Write-Host "  PID        : $($appHost.Id)"
        Write-Host "  Started    : $($appHost.StartTime)"
        Write-Host "  CommandLine: $($appHost.CommandLine)"

        # Show child processes
        $children = Get-ChildProcesses -ParentId $appHost.Id
        if ($children) {
            Write-Host "`n  Child Processes:" -ForegroundColor DarkCyan
            foreach ($child in $children) {
                $childCmd = Get-ProcessCommandLine -Process $child
                Write-Host "    PID $($child.Id) [$($child.ProcessName)]: $childCmd"
            }
        }
        else {
            Write-Host "  No child processes detected."
        }

        # Show network connections (cross-platform via netstat)
        Write-Host "`n  Network Ports (listening):" -ForegroundColor DarkCyan
        try {
            if ($IsWindows) {
                $netstat = & netstat -ano 2>$null | Select-String "LISTENING"
                $netstat | Where-Object { $_ -match "\s$($appHost.Id)$" } | ForEach-Object {
                    Write-Host "    $_"
                }
            }
            else {
                # macOS / Linux
                $netstat = & netstat -tlnp 2>$null
                $netstat | Where-Object { $_ -match $appHost.Id } | ForEach-Object {
                    Write-Host "    $_"
                }

                # lsof is more reliable on macOS
                if (Get-Command lsof -ErrorAction SilentlyContinue) {
                    $lsof = & lsof -Pan -p $appHost.Id -i 2>$null | Where-Object { $_ -match 'LISTEN' }
                    if ($lsof) {
                        $lsof | ForEach-Object { Write-Host "    $_" }
                    }
                }
            }
        }
        catch {
            Write-Host "    (Could not determine ports: $_)"
        }
    }
}

function Stop-AspireAppHost {
    <#
    .SYNOPSIS
        Stops running .NET Aspire App Host processes and optionally their children.

    .DESCRIPTION
        Finds Aspire AppHost processes and terminates them. On Unix-like systems,
        sends SIGTERM first (graceful), then SIGKILL if the process doesn't exit
        within the grace period.

    .PARAMETER NameFilter
        Optional substring to filter by project/assembly name (e.g. "MyApp").

    .PARAMETER Force
        Skip the confirmation prompt and kill immediately.

    .PARAMETER IncludeChildren
        Also terminate child processes spawned by the AppHost.

    .PARAMETER GracePeriodSeconds
        How long to wait for graceful shutdown before force-killing. Default: 5.

    .EXAMPLE
        Stop-AspireAppHost

    .EXAMPLE
        Stop-AspireAppHost -Force -IncludeChildren

    .EXAMPLE
        Stop-AspireAppHost -NameFilter "MyApp" -GracePeriodSeconds 10
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [string]$NameFilter = '',
        [switch]$Force,
        [switch]$IncludeChildren,
        [int]$GracePeriodSeconds = 5
    )

    $hosts = Find-AspireAppHost -NameFilter $NameFilter -Verbose:$false

    if (-not $hosts) {
        Write-Host "No Aspire AppHost processes found — nothing to stop." -ForegroundColor Yellow
        return
    }

    foreach ($appHost in $hosts) {
        $description = "PID $($appHost.Id): $($appHost.CommandLine)"

        if (-not $Force -and -not $PSCmdlet.ShouldProcess($description, "Stop-AspireAppHost")) {
            Write-Host "Skipping PID $($appHost.Id) (user cancelled)." -ForegroundColor DarkGray
            continue
        }

        # Collect children before killing the parent (parent death may reparent them)
        $children = if ($IncludeChildren) {
            Get-ChildProcesses -ParentId $appHost.Id
        }
        else {
            @()
        }

        Write-Host "Stopping AppHost PID $($appHost.Id)..." -ForegroundColor Yellow

        Invoke-GracefulStop -Process $appHost.Process -GracePeriodSeconds $GracePeriodSeconds

        if ($IncludeChildren -and $children) {
            Write-Host "  Stopping $($children.Count) child process(es)..." -ForegroundColor DarkYellow
            foreach ($child in $children) {
                Invoke-GracefulStop -Process $child -GracePeriodSeconds $GracePeriodSeconds
            }
        }

        Write-Host "Done." -ForegroundColor Green
    }
}

#endregion

#region Private Stop Helper

function Invoke-GracefulStop {
    <#
    .SYNOPSIS
        Attempts graceful termination (SIGTERM on Unix, CloseMainWindow on Windows),
        then force-kills if the process doesn't exit within the grace period.
    #>
    [CmdletBinding()]
    param(
        [System.Diagnostics.Process]$Process,
        [int]$GracePeriodSeconds = 5
    )

    if ($Process.HasExited) {
        Write-Verbose "PID $($Process.Id) already exited."
        return
    }

    if ($IsWindows) {
        # Try graceful close first
        $Process.CloseMainWindow() | Out-Null
    }
    else {
        # SIGTERM = 15
        & kill -TERM $Process.Id 2>$null
    }

    Write-Verbose "Waiting up to ${GracePeriodSeconds}s for PID $($Process.Id) to exit..."
    $exited = $Process.WaitForExit($GracePeriodSeconds * 1000)

    if (-not $exited) {
        Write-Warning "PID $($Process.Id) did not exit gracefully. Force-killing..."
        try {
            $Process.Kill()
        }
        catch {
            Write-Warning "Could not force-kill PID $($Process.Id): $_"
        }
    }
    else {
        Write-Verbose "PID $($Process.Id) exited cleanly."
    }
}

#endregion

Export-ModuleMember -Function Find-AspireAppHost, Get-AspireProcessInfo, Stop-AspireAppHost
