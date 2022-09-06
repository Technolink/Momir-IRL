0..20 | ? {-not (Test-Path "xamarin/Momir-IRL/Assets/debug/original/$_")} | % {mkdir "xamarin/Momir-IRL/Assets/debug/original/$_" | Out-Null }
0..20 | ? {-not (Test-Path "xamarin/Momir-IRL/Assets/debug/monochrome/$_")} | % {mkdir "xamarin/Momir-IRL/Assets/debug/monochrome/$_" | Out-Null }
0..20 | ? {-not (Test-Path "xamarin/Momir-IRL/Assets/release/original/$_")} | % {mkdir "xamarin/Momir-IRL/Assets/release/original/$_" | Out-Null }
0..20 | ? {-not (Test-Path "xamarin/Momir-IRL/Assets/release/monochrome/$_")} | % {mkdir "xamarin/Momir-IRL/Assets/release/monochrome/$_" | Out-Null }

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
	
	$path = "xamarin/Momir-IRL/Assets/release/original/$([int]$creature.cmc)/$($creature.id).bmp"
	$target = "xamarin/Momir-IRL/Assets/release/monochrome/$([int]$creature.cmc)/$($creature.id).bmp"
	
	if ($creature.layout -eq "token" -or $creature.layout -eq "augment") {
		continue
	}
	
	if (-not (Test-Path $path)) {
		# download image
		Invoke-RestMethod $creature.image_uris.border_crop -OutFile $path
	}
	
	if (-not (Test-Path $target)) {
		# convert image
		magick convert $path -resize 80% -monochrome $target
	}
	
	$debugPath = "xamarin/Momir-IRL/Assets/debug/original/$([int]$creature.cmc)/$($creature.id).bmp"
	$debugTarget = "xamarin/Momir-IRL/Assets/debug/monochrome/$([int]$creature.cmc)/$($creature.id).bmp"
	if ((ls "xamarin/Momir-IRL/Assets/debug/original/$([int]$creature.cmc)").Count -lt 10) {
		Copy-Item $path $debugPath
		Copy-Item $target $debugTarget
	}
}
