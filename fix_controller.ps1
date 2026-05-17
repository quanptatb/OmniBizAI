$f = "c:\Users\Cua\Desktop\proj\OmniBizAI\Controllers\BusinessControllers.cs"
$lines = [System.IO.File]::ReadAllLines($f)
# Keep only lines 1-1442 (0-indexed: 0..1441), adding closing brace for AiInsightsController
$clean = $lines[0..1441] + "}"
[System.IO.File]::WriteAllLines($f, $clean)
Write-Host "Done. File truncated to $($clean.Length) lines."
