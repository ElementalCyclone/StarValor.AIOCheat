# StarValor AIOCheat

An BepInEx plugin for [Star Valor](https://store.steampowered.com/app/833360/Star_Valor/).

Can be used for spawning items, 'freezing' ships stat, infinite credit, multiply gained XP and such.

------------------------------

## User : How to install and Use + Current Limitation

**1. Reminder !**

- **! Always backup your game saves before applying any plugin or modifications to your game files !**

**2. Requirement :**

- [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html) and [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) installed to your game

> (as-per Star Valor 2.0.2b, you will need 32-bit version of BepInEx)

**1. Installation :**

- Download the latest .dll file from [Releases](https://github.com/ElementalCyclone/StarValor.AIOCheat/releases) and put it inside `BepInEx/Plugin` folder inside your Star Valor installation folder.

**2. Using :**

- Load or start a new game, open ConfigurationManager window (Default key is F1), open `StarValor.AIOCheat` section then choose any cheat you want to enable or value you want to change

**3. Current Limitation, Behavior:**

- Resetting/disabling "Ship" modifiers only restore the modded stat(s) into its base value, without the ship, character or crew perks modifier applied.

  - Won't persist on save, will be re applied on game reload

  - "XP Modifier" is not affected by that, as it works by different way.

- Default value for "Ship" modifier won't adapt/refresh to changed equipment or new crew perk(s) effect. It will only refresh on ship change or game save reload.

- The cheat state(s) won't persist across game restart, **this is by design**, as it is used to clear the cheat internal state.

- The only cheat/behavior that will persist into your game save are :

  - The amount set by "Unlimited Credit" & "Unlimited Skill Points & Reset" and not the 'freezed' state.

  - Any spawned floating object that have been picked/interacted and no longer floating.

------------------------------

## Developer : How to Build & Customize

**Minimum Requirement :**

- Visual Studio that Supports .NET Standard 2.0 and .NET library project, w/ dependencies fulfilled.

- [BepInEx NuGet repo](https://nuget.bepinex.dev/) as one of your Visual Studio NuGet source (https://nuget.bepinex.dev/v3/index.json)

**Dependencies**

This library project depends on several NuGet packages and several directly referred assemblies/.dll files. While the NuGet packages are auto-resolved and auto-restored by Visual Studio, the assemblies are not. Those assemblies are/are from :

- The game's `Managed` folder
- BepInEx.ConfigurationManager's .dll
- BepInEx.ConfigurationManager's [`ConfigurationManagerAttributes.cs`](https://github.com/BepInEx/BepInEx.ConfigurationManager/blob/master/ConfigurationManagerAttributes.cs) source file ([About the file](https://github.com/BepInEx/BepInEx.ConfigurationManager#overriding-default-configuration-manager-behavior))

The project is configured to looks for the required assemblies inside `<.csproj root folder>/Libs` folder. You can fulfill those dependencies simply by copying the files into it or creating symbolic link inside the folder.

Consult the build/Visual Studio error message or the `.csproj` file for the full list of the required dependencies.

------------------------------

This mods and especially its Unlimited Skill Points & Reset feature is heavily inspired by Steam User's, Mortichar, [mod](https://steamcommunity.com/app/833360/discussions/4/3177855357754392918/)
