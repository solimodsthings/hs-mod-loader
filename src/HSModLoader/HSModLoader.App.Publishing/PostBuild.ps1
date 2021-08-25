# [HSModLoader.App Post-Build Script]
# This script cleans up the build folder by
# removing files unnecessary for distribution
# and moves .dll files into their own subfolder.
#
# To change what script is called after
# a build, right-click on HSModLoader.App, choose
# Properties, and select Build Events in the
# nav menu.

param([string]$Target);

# Move .dll files into a subfolder
if(-not (Test-Path "$Target\lib"))
{
	mkdir "$Target\lib"
}

rm "$Target\lib\*.dll"
mv "$Target\*.dll" "$Target\lib\"

if(test-path "$Target\..\..\steam_api64.dll"){
    cp "$Target\..\..\steam_api64.dll" "$Target\lib\steam_api64.dll"
}
elseif(test-path "$Target\..\..\..\steam_api64.dll")
{
    cp "$Target\..\..\..\steam_api64.dll" "$Target\lib\steam_api64.dll"
}

# Remove culture folders
ls "$Target\*-*" | % {

    if(test-path ("$($_.FullName)\ModernWpf.resources.dll")){
        rm $_.FullName -Recurse
    }

}