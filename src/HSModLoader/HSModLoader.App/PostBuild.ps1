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

# Remove culture folders
ls "$Target\*-*" | % {

    if(test-path ("$($_.FullName)\ModernWpf.resources.dll")){
        rm $_.FullName -Recurse
    }

}

<#
if(test-path "$Target\HSModLoader.App.exe"){
    
    if(test-path "$Target\HSModLoader.exe"){
        rm "$Target\HSModLoader.exe"
    }

    mv "$Target\HSModLoader.App.exe" "$Target\HSModLoader.exe"
}

if(test-path "$Target\HSModLoader.App.exe.config"){

    if(test-path "$Target\HSModLoader.exe.config"){
        rm "$Target\HSModLoader.exe.config"
    }

    mv "$Target\HSModLoader.App.exe.config" "$Target\HSModLoader.exe.config"
}
#>