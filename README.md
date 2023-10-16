# SC4Cleanitol
![GitHub all releases](https://img.shields.io/github/downloads/noah-severyn/SC4Cleanitol/total?style=flat-square)

SC4 Cleanitol is a modern implementation of the original [BSC Cleanitol](https://www.sc4devotion.com/csxlex/lex_filedesc.php?lotGET=97) program released in 2013. This program provides an easy way to clean your plugins folder of all non SC4 related files making the game quicker to load, can remove out of date and obsolete plugins, and can tell you what dependencies you have and which dependencies you need for a given mod.

![Application Screenshot](/SC4CleanitolWPF/images/concise.png)

Two of the major improvements for script creators are the ability to specify **TGIs as dependencies** (instead of just file names, meaning Cleanitol scripts will work correctly if a user renames files or datpacks their plugins), and the introduction **conditional dependencies** (only report `item A` as a dependency if `item B` is present, which can be useful for mods which allow the user to choose certain components to install)

For more information, please refer to the [user documentation](https://github.com/noah-severyn/SC4Cleanitol/wiki). 
