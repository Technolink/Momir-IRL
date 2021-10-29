mkdir "$env:temp/momir" | Out-Null
0..16 | % { mkdir "xamarin/Momir-IRL/Assets/$_" | Out-Null }


$url = "https://api.scryfall.com/bulk-data"
$response = Invoke-RestMethod $url
$bulkUrl = ($response.data | ? { $_.type -eq "oracle_cards" }).download_uri

$response = Invoke-RestMethod $bulkUrl

$creatures = $response | ? { $_.type_line -like "*Creature*" }

$i = 0
foreach ($creature in $creatures) {
	$i += 1
	Write-Progress -Activity "Convert" -Status "$($creature.name) $($i/$creatures.Count*100)" -PercentComplete ($i/$creatures.Count*100)

	if (-not $creature.image_uris) {
		continue
	}
	$name = $creature.name -replace " // ", "-" -replace "`"", ""
	
	$path = "$env:temp/momir/$($name).jpg"
	$target = "xamarin/Momir-IRL/Assets/$([int]$creature.cmc)/$($name).bmp"
	
	if ($creature.layout -eq "token" -or $creature.layout -eq "augment" ) {
		continue
	}
	
	if (Test-Path $target) {
		continue
	}
	
	if (-not (Test-Path $path)) {
		Invoke-RestMethod $creature.image_uris.border_crop -OutFile $path
	}
	magick convert $path -resize 80% -monochrome $target
}

Remove-Item "$env:temp/momir" -Recurse -Force