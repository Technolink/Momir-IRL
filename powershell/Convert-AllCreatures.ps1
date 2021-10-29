if (-not (Test-Path "$env:temp/momir")) {
	mkdir "$env:temp/momir" | Out-Null
}
0..16 | ? {-not (Test-Path "xamarin/Momir-IRL/Assets/$_")} | % {mkdir "xamarin/Momir-IRL/Assets/$_" | Out-Null }

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
		# double face card or otherwise doesn't have an image
		continue
	}
	$name = ($creature.name -split " // ")[0]
	$name = $name.Split([IO.Path]::GetInvalidFileNameChars()) -join ''
	
	$path = "$env:temp/momir/$($name).jpg"
	$target = "xamarin/Momir-IRL/Assets/$([int]$creature.cmc)/$($name).bmp"
	
	if ($creature.layout -eq "token" -or $creature.layout -eq "augment" -or (Test-Path $target)) {
		continue
	}
	
	if (-not (Test-Path $path)) {
		# download image
		Invoke-RestMethod $creature.image_uris.border_crop -OutFile $path
	}
	magick convert $path -resize 80% -monochrome $target
}

Remove-Item "$env:temp/momir" -Recurse -Force