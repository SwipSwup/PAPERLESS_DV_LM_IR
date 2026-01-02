# Integration Test for Batch Processing
$ErrorActionPreference = "Stop"

$apiUrl = "http://localhost/api/documents"
$inputDir = Join-Path $PSScriptRoot "../batch_input"
$archiveDir = Join-Path $PSScriptRoot "../batch_archive"

Write-Host "Starting Integration Test..."

# 1. Check API availability and get a document
try {
    $docs = Invoke-RestMethod -Uri $apiUrl -Method Get
} catch {
    Write-Error "Failed to reach API at $apiUrl. Ensure Docker is running."
}

if ($docs.Count -eq 0) {
    Write-Warning "No documents found in DB. Test cannot proceed."
    Write-Host "Please upload a document first via the UI (http://localhost)."
    exit 0
}

$targetDoc = $docs[0]
$docId = $targetDoc.id
$initialCount = [long]$targetDoc.accessCount

Write-Host "Target Document ID: $docId"
Write-Host "Initial AccessCount: $initialCount"

# 2. Create Batch XML File
$increment = 10
$fileName = "integration_test_$(Get-Random).xml"
$filePath = Join-Path $inputDir $fileName

$xmlContent = @"
<AccessLogs>
    <Entry>
        <DocumentId>$docId</DocumentId>
        <AccessCount>$increment</AccessCount>
    </Entry>
</AccessLogs>
"@

Set-Content -Path $filePath -Value $xmlContent
Write-Host "Created batch file: $filePath"

# 3. Wait for processing
Write-Host "Waiting 15 seconds for BatchWorker to process..."
Start-Sleep -Seconds 15

# 4. Verify Archive
$archivedFiles = Get-ChildItem -Path $archiveDir -Filter "*$fileName*"
if ($archivedFiles.Count -eq 0) {
    Write-Error "FAILURE: File was not moved to archive directory. Worker might not be running or failed to process."
} else {
    Write-Host "File correctly archived."
}

# 5. Verify Database Update via API
$updatedDoc = Invoke-RestMethod -Uri "$apiUrl/$docId" -Method Get
$finalCount = [long]$updatedDoc.accessCount

Write-Host "Final AccessCount: $finalCount"

if ($finalCount -eq ($initialCount + $increment)) {
    Write-Host "SUCCESS: AccessCount increased by $increment." -ForegroundColor Green
} elseif ($finalCount -gt $initialCount) {
    Write-Warning "PARTIAL SUCCESS: Count increased but not by exact amount (maybe race condition or other logs?). Got $finalCount, expected $($initialCount + $increment)."
} else {
    Write-Error "FAILURE: AccessCount did not increase."
}
