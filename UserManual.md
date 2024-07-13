# About
SC4 Cleanitol is a modern implementation of the original [BSC Cleanitol](https://github.com/wouanagaine/BSC-Cleanitol) program released in 2013. This program provides an easy way to clean your plugins folder of all non SC4 related files making the game quicker to load, can remove out of date and obsolete plugins, and can tell you what dependencies you have and which dependencies you need for a given mod.

![Application Screenshot](/SC4CleanitolWPF/images/concise.png)

This new version is designed to be backwards compatible with old Cleanitol scripts, but nevertheless includes numerous improvements, two of the major ones being:
1. **Ability to specify TGIs in addition to file names as a dependency**. One of the drawbacks of using filenames only is if a user renames a file or datpacks their plugins folder, the old Cleanitol program will report a missing dependency even if that asset might *actually* be there.
1. **Ability to specify conditional dependencies** - i.e. only report `item A` as a dependency if `item B` is present. This is very useful for mods which may allow the user to choose certain components to install. This way the user will not be prompted for a missing dependency for a component they did not install.

---

# Installation Instructions
Visit the [releases](https://github.com/noah-severyn/SC4Cleanitol/releases) page or go straight to the [latest release](https://github.com/noah-severyn/SC4Cleanitol/releases/latest).

## Requirements
* **Windows 10 or higher**
* **.NET7 runtime version > 7.0.0**
  * To determine which version is installed, open Command Prompt and type `dotnet --list-runtimes``. If the version is less than 7.0.0 installed, download the most recent version [here](https://dotnet.microsoft.com/en-us/download). Alternatively, if you run the executable without the correct .NET runtime, you will receive an error message. Follow the prompts to download the correct version.

---

# For Players (Running Scripts)
This program works by analyzing the contents of a script (commonly referred to as a Cleanitol file or Cleanitol script). These scripts are text files and may be included with certain mods or dependency packs. These scripts can:
* Search for outdated and obsolete plugin files and give you the option to remove them from your plugins folder,
* Search for non SC4 files and give you the option to remove them from your plugins folder, and
* Search your plugins folder for dependencies and notify you of missing dependencies and where to download them.

This program also includes an updated version of `CleanupList.txt`, which is a nearly comprehensive list of all non SC4 files which may be in your plugins folder. Running this script to find and remove these files from plugins will result in a quicker loading time when playing the game - the fewer files in Plugins the game has to parse the quicker the game will load. Contact me if I am missing any extensions and I will add them to this file for future distributions.

If you enable the option to update TGIs, you have an option to **Export TGIs**, which exports the internal TGI index to a CSV file for analysis in external programs. This file is saved in the Cleanitol output directory.

## Using the Program

![Application Screenshot](/SC4CleanitolWPF/images/concise.png)

First, select a script to run, then run the script. The results of the script are shown in the output window. If there are files marked for removal after running the script, pressing the **Move files** button will move the designated files to a designated location outside of the plugins folder. Each time files are moved a new directory will be created here with the date/time the script is run. Also included in each directory is an HTML file summarizing the actions taken and an `undo.bat` file which will copy these files back into their original location in the Plugins folder if executed. To execute the undo script, double click the file.

### Script Execution Options
* **Verbose Output** - Toggle to show the outcome of every rule in the script (verbose is checked), or only the important messages (verbose is unchecked).
* **Update TGI Database** - Scan all files in the plugins folder to create the list of all TGIs present. This will dramatically increase the time taken to execute the script, so unless you are running a mostly vanilla game with few plugins it is recommended to use this option occasionally or after many new plugins are added.

### Program Options

![Settings Panel Screenshot](/SC4CleanitolWPF/images/settings.png)

To change options, open the Settings panel.
* **User Plugins** - Adjust to the plugins directory in your user directory
* **System Plugins** - Adjust to the plugins directory in the game install directory
  * **Scan system Plugins folder too** - Select this option to include the system plugins directory when scanning for files and dependencies. It is generally recommended that most plugins be installed in the user plugins folder, but certain mods *may* be installed in the systems plugin directory instead.
  * **Cleanitol Output** - Adjust the location that the program will move files to if a Cleanitol script finds files to remove. The default location is adjacent to the user plugins directory in `\My Documents\SimCity 4\BSC_Cleanitol\...`.
  * **Scan extra folders** - If you have other folders outside of your Plugins folder you wish to scan the contents of, add them here. Use the plus (`+`) button to add a folder, and use the minus (`-`) button to remove the currently selected folder. This option can be useful if you frequently move files in and out of Plugins, or have multiple Plugins folders that you swap in and out. Toggle the checkbox to scan or skip these folders you added without removing them from the list.

---

# For Script Creators
This program was designed with full backwards compatibility for old Cleanitol scripts. While the [old instructions](https://www.sc4devotion.com/forums/index.php?topic=3797.0) for script creation are still valid, there are multiple improvements which allow for more features and flexibility when creating scripts.

## Automatically creating a removal script
Use the **Create Script** button on the main screen to automatically create a script with all files in a chosen folder. Choose the folder containing the files you want to add to the script (note that contents of all sub folders within the chosen folder **will** be included), and then choose the name and location of the output script to create.

## Writing a script
The scripts are simple text files composed of one or more rules, with each rule on its own line. There are six types of rules.

### 1. Script Comments
Begin the line with a semicolon `;` to create a script comment. These comments will not be shown to users in the output window and can be useful for personal notes documenting the script.
```
;This is a script comment. This comment will not be visible to users.
```

### 2. User Comments
Begin the line with a right angle bracket `>`. These comments will be shown to the user in the output window. Use these comments for any message you wish to show to the user. ***This is a new feature.***
```
>This is a user comment. This comment will be shown to users in the script output window.
```

### 3. User Comment Headings
Begin the line with a right angle bracket and immediately follow with a pound sign `>#`. This will add a line break and heading text to the output window, which can be useful for visually grouping or segmenting the results of different parts of a script. ***This is a new feature.***
```
>#This Is A Heading
```

### 4. Removal Rule
Simply list a file name and its file extension for it to be removed. Wildcards are supported through use of asterisks `*`.
```
BSC Flag Pack 1 Test Lot.SC4Lot
*.jpeg
*._Loose*
```
> **Note**
> You cannot specify specific TGIs to remove. This would necessitate opening and modifying DBPF to remove them, which at this time is outside of the scope of this program.

### 5. Dependency Rule
To specify a required dependency, supply a filename and extension. Follow with a semicolon `;` and then the HTTP URL of where to download the file from.
```
JRJ_TC_Aesculus_01a-0x6534284a-0x0f55ca9c-0xb0f8ec8b.SC4Desc; http://sc4devotion.com/csxlex/lex_filedesc.php?lotGET=167
```
You can include a "friendly" name to show in place of the base URL. Add this text between the semicolon and the url.
```
JRJ_TC_Aesculus_01a-0x6534284a-0x0f55ca9c-0xb0f8ec8b.SC4Desc; BSC Props Jeronij Vol 02 Trees http://sc4devotion.com/csxlex/lex_filedesc.php?lotGET=167
```

Instead of using a file name, you can now also specify a TGI set as a required dependency. These follow the same rules as the filename-based dependency rules. ***This is a new feature.***
```
0x6534284a 0x0f55ca9c 0xb0f8ec8b;BSC Props Jeronij Vol 02 Trees http://sc4devotion.com/csxlex/lex_filedesc.php?lotGET=167
```

If you specify a TGI dependency, each Type, Group, and Instance number must be prefaced with `0x` and be in hexadecimal format. The separator between the Type, Group, and Instance values is not important. As an example, all of the following are valid:
```
0x6534284a-0x0f55ca9c-0xb0f8ec8b; http://sc4devotion.com/csxlex/lex_filedesc.php?lotGET=167
0x6534284a 0x0f55ca9c 0xb0f8ec8b; http://sc4devotion.com/csxlex/lex_filedesc.php?lotGET=167
0x6534284a, 0x0f55ca9c, 0xb0f8ec8b; http://sc4devotion.com/csxlex/lex_filedesc.php?lotGET=167
```

> [!WARNING]  
> If you are writing a script and one of your dependency rules is showing as an `Unchecked Dependency`, that is an indication that something is wrong. Unchecked dependencies are a feature included to provide compatibility with very legacy Cleanitol scripts that had typos or used them for cascading dependencies. They should not be used in modern scripts. If you encounter one, please check the rule syntax, making sure the dependency has either a correct file extension, or is a correctly formatted TGI.


### 6. Conditional Dependency Rule
In addition to standard dependency rules, you can also specify conditional dependency rules. These rules will trigger only if another file or TGI is present. These rules may be useful for more complex mods that allow users to choose certain components to use or install. With a correctly designed script these will result in a cleaner user experience as the user will not be prompted for missing dependencies for components the user did not install. ***This is a new feature.***

To create a conditional dependency, specify two filenames or TGIs, separated by two question marks `??`. The first item will trigger as a dependency only if the second item is present in plugins.
```
Item A ?? Item B; Name
```

In other words, `Item A` is a dependency only if `Item B` is found. In the below example, the model file is only required if the descriptor file is present in plugins.
```
jes_AcmeBoilerFactory-0x5ad0e817_0x5283112c_0x30000.SC4Model ?? IRM W2W jes_AcmeBoilerFactory1-0x6534284a-0xbf3fbe81-0xe1278c85_x4.sc4desc; The Acme Boiler Factory https://www.sc4devotion.com/csxlex/lex_filedesc.php?lotGET=66
```

TGIs and filenames can be intermixed within a given rule. All combinations of filenames and TGIs as the conditional item and the dependency item are supported.