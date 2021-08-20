# Overview
This repository contains a mod loader and management tool for [Himeko Sutori](https://store.steampowered.com/app/669500/Himeko_Sutori/). It is a work in progress and I am currently working towards a first release.

# Quick Start ✨
1. Download the latest release
2. Unzip the release file and run ```HSModLoader.exe```
3. On first launch, you will be prompted to provide the path to Himeko Sutori's installation folder. Type it in or click the ```Autodetect Using Steam``` to try and autofill the path.

# Adding Mod Packages ✨
1. Download your favorite Himeko Sutori mod packages (```.hsmod``` files)
2. Drag-and-drop the ```.hsmod``` files into the mod loader window to add it
3. Enable the mod you just added and the hit ```Apply Mods to Game``` button

*In the future, this mod loader will also detect steam workshop items you've subscribed to and allow you to enable/disable them much like a standalone mod.*

# Creating New Mod Package
Assuming you've already created a mod and wish to publish it as a standalone ```.hsmod``` file for others to use:
1. Run ```HSModPublisher.exe```
2. Click on the ```+``` button at the top-left to start a new mod
3. Provide a name and location for the mod folder to reside in
4. Fill out the mod information fields (version, description, etc.)
5. Place your mod files (.u, .upk, etc.) inside the mod package directory
6. Click on the publish button at the top-left

*In the future, this publishing tool will permit you to upload your mod directly to the Steam Workshop.*

# Notes
This mod loader provides a log of file changes in ```filechanges.log``` whenever you hit the ```Apply Mods to Game``` button

# Dependencies
* [ModernWpf](https://github.com/Kinnara/ModernWpf) ([MIT license](https://github.com/Kinnara/ModernWpf/blob/master/LICENSE))
* [Ookii Dialogs](https://github.com/ookii-dialogs/ookii-dialogs-wpf) ([BSD license](https://github.com/ookii-dialogs/ookii-dialogs-wpf/blob/master/LICENSE))
* [Cethiel's classic gems art](https://opengameart.org/content/gems-classic) ([CC0 1.0 license](https://creativecommons.org/publicdomain/zero/1.0/))

# License
```
MIT License

Copyright (c) 2021 Soli

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
