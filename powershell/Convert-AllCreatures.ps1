0..16 | ? {-not (Test-Path "xamarin/Momir-IRL/Assets/original/$_")} | % {mkdir "xamarin/Momir-IRL/Assets/original/$_" | Out-Null }
0..16 | ? {-not (Test-Path "xamarin/Momir-IRL/Assets/monochrome/$_")} | % {mkdir "xamarin/Momir-IRL/Assets/monochrome/$_" | Out-Null }

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
	
	$path = "xamarin/Momir-IRL/Assets/original/$([int]$creature.cmc)/$($creature.id).bmp"
	$target = "xamarin/Momir-IRL/Assets/monochrome/$([int]$creature.cmc)/$($creature.id).bmp"
	
	if ($creature.layout -eq "token" -or $creature.layout -eq "augment" -or ((Test-Path $path) -and (Test-Path $target))) {
		continue
	}
	
	if (-not (Test-Path $path)) {
		# download image
		Invoke-RestMethod $creature.image_uris.border_crop -OutFile $path
	}
	magick convert $path -resize 80% -monochrome $target
}
