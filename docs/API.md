# Framework API v1 (0.1.1)

Target: Pathless Woods Steam25836294, Unity2022.3.62f3 IL2CPP, Windows x64. Use BepInEx Unity IL2CPP6.0.0-be.788+5b766a3 and .NET6. The framework is a foundation for mods, not a general save-game or multiplayer API.

## Plugin dependency

Create an independent BasePlugin with a unique BepInPlugin GUID. Declare [BepInDependency(FrameworkPlugin.Id)] and reference the matching framework DLL with Private=false. Do not bundle the framework, loader, game assemblies or generated interop DLLs inside your mod.

## Mod metadata

In Load, call ModRegistry.Register(new ModDescription(Id, name, numericVersion, description, author)). Registration validates required fields and rejects repeated IDs. Use the plugin's own GUID. Call GetInstalled on the main thread after loader startup; it returns a read-only sorted snapshot. Loader-discovered plugins appear even without registration. Version and Loaded come from loader metadata; Loaded does not certify gameplay correctness. Registered-only entries are explicitly marked as not loaded. API v1 metadata accepts numeric versions such as0.1.0; loader-discovered version strings can include loader-supported suffixes.

## Configuration

Use your inherited Config.Bind methods. The menu supports bool, string, int, float, double and non-flags enums. Other types are read-only. Declared AcceptableValueRange/List constraints are checked before saving; nonfinite numbers and undefined enum values are rejected. Values are persisted through the original ConfigFile, so normal BepInEx SettingChanged handlers still apply. Setting changes do not guarantee a mod supports live reconfiguration.

Optionally include new ModSettingOptions(displayName, requiresRestart) in ConfigDescription.Tags. A restart marker is shown for marked settings. Do not place secrets in player-visible settings. Apply commits a value; R restores its default. The framework does not enable/unload plugins while playing.

## Main thread

FrameworkRuntime.ApiVersion is1. Post(Action) queues short work for Unity's main thread. Up to64 callbacks are drained per frame; excess work remains queued. A callback exception is logged and does not stop subsequent callbacks. Keep callbacks short; the framework cannot preempt a long action. This is a process-lifetime queue, not save persistence. Use normal BepInEx logging in your plugin.

ModMenu.Open/Close are queued on the main thread. Open operates when the main or pause menu is available. Read IsOpen on the main thread; it reports whether the framework menu is open. UI uses native fonts and an independent Unity canvas;closing restores cursor visibility and lock state.

## Localization

RegisterLanguage(modId, displayName, nativeLanguageId, IEnumerable<Translation>) registers a separate pack during Load before the catalog loads. Translation has section, key, value and optional parent. IDs4–9 are custom IDs;5 is used by Russian Localization. Multiple packs may extend a shared language ID;mod IDs and keys within each pack must be unique. Keep rich-text tags, input tokens and placeholders intact. Parent-scoped support has only been validated for the existing approved routes; do not assume every dialog path is covered. Untranslated text falls back to the native English catalog.

FontSupport.EnableCyrillicFallback requests native glyph preparation and safe fallback attachment. Whole-game glyph coverage is not claimed.

## Example and limits

The separate SDK includes an independent compilable example registering metadata, bool/string/bounded-integer/enum settings and a queued callback. It does not change gameplay. Change its GUID/name before using it as the basis for a real mod. Do not install the example as a dependency of your own mod.

No save API, network API, hot plugin unloading, arbitrary game event bus or automatic compatibility certification is provided. The first release targets only the documented game/loader build.
