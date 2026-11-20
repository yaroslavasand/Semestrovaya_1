$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$manifest = Get-Content -LiteralPath (Join-Path $root 'assets.json') -Raw | ConvertFrom-Json
$failed = @()
foreach ($asset in $manifest) {
    $target = Join-Path $root $asset.path
    New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
    try {
        Invoke-WebRequest -UseBasicParsing -Uri $asset.url -OutFile $target -TimeoutSec 60
        if ((Get-Item -LiteralPath $target).Length -eq 0) { throw 'Empty download' }
        Write-Host ('OK: ' + $asset.path)
    } catch {
        $failed += $asset.path
        Write-Warning ('FAILED: ' + $asset.path + ' | ' + $_.Exception.Message)
    }
}
$riotCssFile = Join-Path $root 'Static/css/riot-fonts.css'
try {
    $cssUri = [Uri]'https://lolstatic-a.akamaihd.net/webfonts/live/css/riot/fonts.css'
    $response = Invoke-WebRequest -UseBasicParsing -Uri $cssUri -TimeoutSec 60
    $faces = [regex]::Matches($response.Content, '@font-face\s*\{[^}]+\}')
    $localFaces = @()
    $index = 0
    foreach ($face in $faces) {
        $text = $face.Value
        if ($text -notmatch 'font-family\s*:\s*["'']?Inter["'']?\s*;') { continue }
        $urlMatches = [regex]::Matches($text, 'url\(\s*["'']?([^\)"'']+)["'']?\s*\)')
        $chosen = $null
        foreach ($match in $urlMatches) {
            if ($match.Groups[1].Value -match '\.woff2?(?:\?|$)') {
                $chosen = $match.Groups[1].Value
                break
            }
        }
        if (-not $chosen) { continue }
        $uri = [Uri]::new($cssUri, $chosen)
        $extension = [IO.Path]::GetExtension($uri.AbsolutePath)
        $filename = 'riot-inter-' + $index + $extension
        $index++
        $target = Join-Path $root ('Static/fonts/' + $filename)
        Invoke-WebRequest -UseBasicParsing -Uri $uri.AbsoluteUri -OutFile $target -TimeoutSec 60
        $format = if ($extension -eq '.woff2') { 'woff2' } else { 'woff' }
        $localSrc = 'src: url("/Static/fonts/' + $filename + '") format("' + $format + '");'
        $text = [regex]::Replace($text, 'src\s*:[^;]+;', $localSrc)
        $localFaces += $text
        Write-Host ('OK: Static/fonts/' + $filename)
    }
    if ($localFaces.Count -eq 0) { throw 'No Inter WOFF declarations found in source CSS' }
    [IO.File]::WriteAllText($riotCssFile, ($localFaces -join "`r`n"), [Text.UTF8Encoding]::new($false))
} catch {
    $failed += 'Inter font / riot-fonts.css'
    Write-Warning ('FAILED: Inter | ' + $_.Exception.Message)
}
if ($failed.Count -gt 0) {
    throw ('Some resources were not downloaded: ' + ($failed -join ', '))
}
Write-Host 'All resources downloaded. Restart the C# application.'
