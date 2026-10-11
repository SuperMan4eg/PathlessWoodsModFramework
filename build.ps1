param([string]$GameRoot='C:\Program Files (x86)\Steam\steamapps\common\Pathless Woods')
$ErrorActionPreference='Stop'
$project="$PSScriptRoot/src/PathlessWoodsModFramework/PathlessWoodsModFramework.csproj"
$example="$PSScriptRoot/examples/ExampleMod/ExampleMod.csproj"
foreach($item in @($project,$example)) {
 dotnet restore $item --configfile "$PSScriptRoot/NuGet.Config" "-p:GameRoot=$GameRoot" --nologo
 if($LASTEXITCODE){throw 'Restore failed'}
 dotnet build $item -c Release --no-restore --nologo -warnaserror "-p:GameRoot=$GameRoot"
 if($LASTEXITCODE){throw 'Build failed'}
}
[xml]$metadata=Get-Content $project -Raw
$version=[string]$metadata.Project.PropertyGroup.Version
$stage="$PSScriptRoot/Artifacts/PathlessWoodsModFramework-$version"
if((Test-Path $stage) -or (Test-Path "$stage.zip")){throw 'Artifact exists; preserve the previous build'}
$files=[ordered]@{
 'src/PathlessWoodsModFramework/bin/Release/net6.0/PathlessWoodsModFramework.dll'='BepInEx/plugins/PathlessWoodsModFramework/PathlessWoodsModFramework.dll'
 'README.md'='README.md'
 'CHANGELOG.md'='CHANGELOG.md'
 'LICENSE'='LICENSE'
}
foreach($entry in $files.GetEnumerator()) {
 $inputFile=Join-Path $PSScriptRoot $entry.Key
 if($inputFile.EndsWith('.md') -and (Get-Content $inputFile -Raw) -match '[\u0400-\u04ff]|\{\{'){throw 'Unfinished/non-English release document'}
 $outputFile=Join-Path $stage $entry.Value
 New-Item -ItemType Directory (Split-Path $outputFile -Parent) -Force|Out-Null
 Copy-Item -LiteralPath $inputFile -Destination $outputFile
}
if(@(Get-ChildItem $stage -Recurse -File).Count -ne $files.Count){throw 'Unexpected package files'}
Compress-Archive -LiteralPath "$stage/BepInEx","$stage/README.md","$stage/CHANGELOG.md","$stage/LICENSE" -DestinationPath "$stage.zip"
Write-Output "$stage.zip (newly built files require testing before distribution)"
