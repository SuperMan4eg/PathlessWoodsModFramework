# Pathless Woods Mod Framework 0.1.3

A framework for independent Pathless Woods mods, including an in-game mods menu, configuration editing, mod registration and localization services. This version targets Steam build 25836294, Windows x64 Unity2022.3.62f3 IL2CPP. Complete gameplay compatibility is not claimed.

Requires BepInEx Unity IL2CPP x64 6.0.0-be.788+5b766a3, installed separately. Russian Localization is an optional, separately installed language pack.

Copy the included BepInEx directory into the game directory. In the main menu, click Mods between Settings and Exit, or use the matching Mods button in the pause menu. F6 also opens the menu. Search installed plugins, inspect their versions and descriptions, and edit supported BepInEx settings. Apply saves a value; R restores its default. Values outside declared ranges/lists are rejected. Some mods require a restart to apply settings. Types the menu cannot edit are displayed read-only. The menu discovers plugins even when they do not register with the framework.

The interface supports English and Russian. Closing the menu restores cursor visibility and lock state. The menu does not unload or enable plugins during play.

Modders: see the separate SDK for API documentation and a compilable example plugin. Services include optional metadata registration, a bounded Unity main-thread callback queue, language registration and Cyrillic font support. Save-game and multiplayer APIs are not provided.

To uninstall, remove dependent mods first, then remove BepInEx/plugins/PathlessWoodsModFramework. Original game catalogs are not replaced. Save compatibility and multiplayer behavior have not been tested.

To hide the BepInEx console, set Enabled = false in the [Logging.Console] section of BepInEx/config/BepInEx.cfg and restart the game. File logging remains available in BepInEx/LogOutput.log. Set Enabled = true to restore the console.

Source and SDK example: https://github.com/SuperMan4eg/PathlessWoodsModFramework (MIT license).



## Building from source

Install the .NET 6 SDK and the separately obtained BepInEx loader in your own game installation. Launch the game through Steam once to generate BepInEx/interop, then close it. Run:

```powershell
./build.ps1 -GameRoot "C:\Program Files (x86)\Steam\steamapps\common\Pathless Woods"
```

The script builds the framework and independent example with warnings treated as errors. It creates a runtime ZIP from an explicit file list. Game assemblies, generated interop files and loader libraries are referenced locally and are never included. Install only your own plugin DLLs; the example is optional and is not included in the runtime ZIP.

See [API documentation](docs/API.md) and [example plugin](examples/ExampleMod/ExampleMod.cs). API version 1 covers mod metadata, configuration, the main-thread queue and localization. It does not supply save-game or multiplayer services, hot plugin unloading or a general game event API.

## License

Framework source and example are available under the [MIT license](LICENSE). The game and external libraries retain their own licenses and are not distributed here.
