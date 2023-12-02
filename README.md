# SC4Cleanitol
![GitHub all releases](https://img.shields.io/github/downloads/noah-severyn/SC4Cleanitol/total?style=flat-square)

SC4 Cleanitol is a modern implementation of the original [BSC Cleanitol](https://github.com/wouanagaine/BSC-Cleanitol) program released in 2013. This program provides an easy way to clean your plugins folder of all non SC4 related files making the game quicker to load, can remove out of date and obsolete plugins, and can tell you what dependencies you have and which dependencies you need for a given mod.

![Application Screenshot](/SC4CleanitolWPF/images/concise.png)

Two of the major improvements for script creators are the ability to specify **TGIs as dependencies** (instead of just file names, meaning Cleanitol scripts will work correctly if a user renames files or datpacks their plugins), and the introduction **conditional dependencies** (only report `item A` as a dependency if `item B` is present, which can be useful for mods which allow the user to choose certain components to install)

For more information, please refer to the [user documentation](/UserManual.md).

## Downloading
Visit the [releases](https://github.com/noah-severyn/SC4Cleanitol/releases) page or go straight to the [latest release](https://github.com/noah-severyn/SC4Cleanitol/releases/latest).

## Projects
This repository is divided into multiple different projects, each with a different implementation.
- **SC4CleanitolEngine** stores the code that actually implements the Cleanitol functionality. Other implementations in this repository reference this project.
- **SC4CleanitolConsole** is a cross-platform console based implementation.
- **SC4CleanitolWPF** is a Windows-only UI application suitable for Windows operating systems from XP Service Pack 2 through 10 (and possibly 11, though this is untested).
- An additional cross-platform UI implementation is planned, targeting functionality on Windows, Mac, and Linux.
