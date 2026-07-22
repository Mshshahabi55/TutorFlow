#Requires -Version 5.1
<#
.SYNOPSIS
    TutorFlow's authoritative "is the build green" gate.

.DESCRIPTION
    Per docs/adr/ADR-018-regional-deployment-and-market-scope.md, this
    project cannot depend on GitHub (or any external CI) being reachable.
    This script is the real source of truth for whether the codebase is
    releasable, runnable entirely offline against the local checkout:

      1. dotnet restore + dotnet build TutorFlow.sln  — 0 warnings, 0 errors
      2. dotnet test TutorFlow.sln, run TWICE          — both runs must pass
         (a determinism guard: a suite that only passes once is not proven
         green, per docs/phases/PHASE-01-REPORT.md). Excludes the
         Postgres-backed integration tests (Category!=Postgres) — those run
         as their own step below, since they need a real PostgreSQL
         instance neither this default run nor CI can assume is present.
      3. the Postgres-backed integration tests (Category=Postgres) against
         TUTORFLOW_TEST_CONNECTION (see README.md "Database setup") — these
         fail loudly, not skip, if that variable isn't set
         (docs/phases/PHASE-02-REPORT.md Task 4)
      4. frontend: npm ci, npm run lint, npm run build, npm test -- --run
      5. a PASS/FAIL summary, per-step and total elapsed time, and a
         non-zero exit code on any failure

    .github/workflows/*.yml mirror this pipeline for convenience, but per
    ADR-018 they are best-effort only — this script is authoritative.

.EXAMPLE
    ./scripts/verify.ps1
#>

# Deliberately NOT 'Stop'. PowerShell 5.1 wraps a native command's stderr
# lines (e.g. npm's routine deprecation warnings) as NativeCommandError
# objects whenever the invocation's output streams are redirected/merged —
# including transitively, if whoever runs this script pipes its own output
# to a log file (". .\verify.ps1 *>&1 | Tee-Object -FilePath run.log", a
# very likely way to invoke a verification script). Under 'Stop', that
# turns a harmless warning into a script-aborting exception. Every step
# below is judged by its own $LASTEXITCODE, not by whether PowerShell
# raised an error, so this script does not need 'Stop' to detect failure
# correctly — and is actively broken by it.
$ErrorActionPreference = 'Continue'

$repoRoot = Split-Path -Parent $PSScriptRoot
$backendDir = Join-Path $repoRoot 'backend'
$frontendDir = Join-Path $repoRoot 'frontend'

$totalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$stepResults = New-Object System.Collections.Generic.List[PSCustomObject]

function Write-StepHeader {
    param([string]$Name)
    Write-Host ""
    Write-Host "=== $Name ===" -ForegroundColor Cyan
}

function Add-StepResult {
    param(
        [string]$Name,
        [string]$Status,
        [double]$Seconds
    )
    $stepResults.Add([PSCustomObject]@{
        Name    = $Name
        Status  = $Status
        Seconds = [math]::Round($Seconds, 1)
    })
}

function Get-WarningCount {
    param([string[]]$Output)
    $matches = $Output | Select-String -Pattern '(\d+)\s+Warning\(s\)'
    if ($matches.Count -eq 0) {
        return -1
    }
    $last = $matches[$matches.Count - 1]
    return [int]$last.Matches[0].Groups[1].Value
}

# --- Backend: restore ---
Write-StepHeader 'Backend: dotnet restore'
$stepWatch = [System.Diagnostics.Stopwatch]::StartNew()
Push-Location $backendDir -ErrorAction Stop
try {
    dotnet restore TutorFlow.sln
    $restoreExitCode = $LASTEXITCODE
} finally {
    Pop-Location
}
$stepWatch.Stop()
if ($restoreExitCode -eq 0) {
    Add-StepResult -Name 'dotnet restore' -Status 'PASS' -Seconds $stepWatch.Elapsed.TotalSeconds
} else {
    Add-StepResult -Name 'dotnet restore' -Status 'FAIL' -Seconds $stepWatch.Elapsed.TotalSeconds
}

# --- Backend: build (0 warnings, 0 errors) ---
$buildOk = $false
if ($restoreExitCode -eq 0) {
    Write-StepHeader 'Backend: dotnet build TutorFlow.sln'
    $stepWatch = [System.Diagnostics.Stopwatch]::StartNew()
    Push-Location $backendDir -ErrorAction Stop
    try {
        # No 2>&1 here: the "N Warning(s)" summary this step parses for is
        # printed on dotnet's stdout, not stderr, so merging streams would
        # only add NativeCommandError risk (see the note above) for no
        # benefit.
        $buildOutput = dotnet build TutorFlow.sln | Tee-Object -Variable buildOutputLive
        $buildExitCode = $LASTEXITCODE
    } finally {
        Pop-Location
    }
    $stepWatch.Stop()
    $warningCount = Get-WarningCount -Output ($buildOutput | ForEach-Object { $_.ToString() })
    if ($buildExitCode -eq 0 -and $warningCount -eq 0) {
        $buildOk = $true
        Add-StepResult -Name 'dotnet build (0 warnings)' -Status 'PASS' -Seconds $stepWatch.Elapsed.TotalSeconds
    } else {
        Add-StepResult -Name 'dotnet build (0 warnings)' -Status "FAIL (exit=$buildExitCode, warnings=$warningCount)" -Seconds $stepWatch.Elapsed.TotalSeconds
    }
} else {
    Add-StepResult -Name 'dotnet build (0 warnings)' -Status 'SKIPPED (restore failed)' -Seconds 0
}

# --- Backend: test, twice (determinism guard) ---
# Category!=Postgres: the Postgres-backed integration tests run as their
# own, separately-labelled step below (Task 4/Task 7,
# docs/phases/PHASE-02-REPORT.md) — neither this default run nor CI can
# assume a real PostgreSQL instance is present.
for ($run = 1; $run -le 2; $run++) {
    $stepName = "Backend: dotnet test TutorFlow.sln (run $run of 2)"
    if ($buildOk) {
        Write-StepHeader $stepName
        $stepWatch = [System.Diagnostics.Stopwatch]::StartNew()
        Push-Location $backendDir -ErrorAction Stop
        try {
            dotnet test TutorFlow.sln --no-build --filter "Category!=Postgres"
            $testExitCode = $LASTEXITCODE
        } finally {
            Pop-Location
        }
        $stepWatch.Stop()
        if ($testExitCode -eq 0) {
            Add-StepResult -Name $stepName -Status 'PASS' -Seconds $stepWatch.Elapsed.TotalSeconds
        } else {
            Add-StepResult -Name $stepName -Status 'FAIL' -Seconds $stepWatch.Elapsed.TotalSeconds
        }
    } else {
        Add-StepResult -Name $stepName -Status 'SKIPPED (build failed)' -Seconds 0
    }
}

# --- Backend: Postgres-backed integration tests (own step, not part of the
# determinism-guard runs above) — requires TUTORFLOW_TEST_CONNECTION
# (README.md "Database setup"). PostgresTestFixture's constructor throws a
# clear message if it's unset, so this step FAILS rather than silently
# passing/skipping when Postgres isn't configured (docs/phases/PHASE-02-REPORT.md
# Task 4) — that failure is real signal, not a false negative to work around.
$stepName = 'Backend: Postgres integration tests'
if ($buildOk) {
    Write-StepHeader $stepName
    $stepWatch = [System.Diagnostics.Stopwatch]::StartNew()
    Push-Location $backendDir -ErrorAction Stop
    try {
        dotnet test TutorFlow.sln --no-build --filter "Category=Postgres"
        $postgresTestExitCode = $LASTEXITCODE
    } finally {
        Pop-Location
    }
    $stepWatch.Stop()
    if ($postgresTestExitCode -eq 0) {
        Add-StepResult -Name $stepName -Status 'PASS' -Seconds $stepWatch.Elapsed.TotalSeconds
    } else {
        Add-StepResult -Name $stepName -Status 'FAIL' -Seconds $stepWatch.Elapsed.TotalSeconds
    }
} else {
    Add-StepResult -Name $stepName -Status 'SKIPPED (build failed)' -Seconds 0
}

# --- Frontend: npm ci ---
Write-StepHeader 'Frontend: npm ci'
$stepWatch = [System.Diagnostics.Stopwatch]::StartNew()
Push-Location $frontendDir -ErrorAction Stop
try {
    npm ci
    $npmCiExitCode = $LASTEXITCODE
} finally {
    Pop-Location
}
$stepWatch.Stop()
$npmCiOk = $npmCiExitCode -eq 0
if ($npmCiOk) {
    Add-StepResult -Name 'npm ci' -Status 'PASS' -Seconds $stepWatch.Elapsed.TotalSeconds
} else {
    Add-StepResult -Name 'npm ci' -Status 'FAIL' -Seconds $stepWatch.Elapsed.TotalSeconds
}

# --- Frontend: lint / build / test (each independent of the others, all require npm ci) ---
$frontendSteps = @(
    @{ Name = 'Frontend: npm run lint';       Command = { npm run lint } },
    @{ Name = 'Frontend: npm run build';      Command = { npm run build } },
    @{ Name = 'Frontend: npm test -- --run';  Command = { npm test -- --run } }
)

foreach ($step in $frontendSteps) {
    if ($npmCiOk) {
        Write-StepHeader $step.Name
        $stepWatch = [System.Diagnostics.Stopwatch]::StartNew()
        Push-Location $frontendDir -ErrorAction Stop
        try {
            & $step.Command
            $stepExitCode = $LASTEXITCODE
        } finally {
            Pop-Location
        }
        $stepWatch.Stop()
        if ($stepExitCode -eq 0) {
            Add-StepResult -Name $step.Name -Status 'PASS' -Seconds $stepWatch.Elapsed.TotalSeconds
        } else {
            Add-StepResult -Name $step.Name -Status 'FAIL' -Seconds $stepWatch.Elapsed.TotalSeconds
        }
    } else {
        Add-StepResult -Name $step.Name -Status 'SKIPPED (npm ci failed)' -Seconds 0
    }
}

$totalStopwatch.Stop()

# --- Summary ---
Write-Host ""
Write-Host "=================== VERIFY SUMMARY ===================" -ForegroundColor Yellow
$anyFailure = $false
foreach ($result in $stepResults) {
    $color = 'Green'
    if ($result.Status -like 'FAIL*') {
        $color = 'Red'
        $anyFailure = $true
    } elseif ($result.Status -like 'SKIPPED*') {
        $color = 'DarkYellow'
        $anyFailure = $true
    }
    $line = "{0,-45} {1,-30} {2,6}s" -f $result.Name, $result.Status, $result.Seconds
    Write-Host $line -ForegroundColor $color
}
Write-Host "-------------------------------------------------------"
Write-Host ("Total elapsed: {0:N1}s" -f $totalStopwatch.Elapsed.TotalSeconds)

if ($anyFailure) {
    Write-Host ""
    Write-Host "RESULT: FAIL" -ForegroundColor Red
    exit 1
} else {
    Write-Host ""
    Write-Host "RESULT: PASS" -ForegroundColor Green
    exit 0
}
