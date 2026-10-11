using AniYa;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace PathlessWoodsModFramework;
public sealed record Translation(int Section, string Key, string Value, string? Parent = null);
internal sealed record LanguagePack(string ModId, string DisplayName, int LanguageId, Translation[] Entries);
public static class Localization
{
    internal static readonly SortedDictionary<string, LanguagePack> Packs = new(StringComparer.Ordinal);
    private static readonly Dictionary<(int Language,string Text),string> InlineTranslations = new();
    internal static ManualLogSource? Log;
    public static void RegisterLanguage(string modId, string displayName, int nativeLanguageId, IEnumerable<Translation> translations)
    {
        if (string.IsNullOrWhiteSpace(modId) || string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Mod ID and language name are required.");
        if (nativeLanguageId < 4 || nativeLanguageId > 9) throw new ArgumentOutOfRangeException(nameof(nativeLanguageId));
        var entries = translations.ToArray();
        if (entries.Any(t => string.IsNullOrEmpty(t.Key) || t.Value is null)) throw new ArgumentException("Invalid translation.");
        if (entries.Select(t => (t.Section,t.Parent,t.Key)).Distinct().Count() != entries.Length) throw new ArgumentException("Duplicate translation key.");
        if (!Packs.TryAdd(modId, new(modId,displayName,nativeLanguageId,entries))) throw new InvalidOperationException($"Already registered: {modId}");
        InlineTranslations.Clear();
        SourceTextTranslations.Invalidate();
        foreach(var pack in Packs.Values)
            foreach(var entry in pack.Entries)
                if(entry.Section==1 && entry.Parent is null)InlineTranslations[(pack.LanguageId,entry.Key)]=entry.Value;
        Log?.LogInfo($"Registered language {displayName} id={nativeLanguageId}: {entries.Length} translations");
    }
    internal static bool HasLanguage(int language) => Packs.Values.Any(p => p.LanguageId == language);
    internal static int CurrentLanguage => GameSettingData._Instance_k__BackingField is null ? -1 : (int)GameSettingData._Instance_k__BackingField.GetUserLanguage();
    internal static bool TryTranslation(int language, int section, string key, string? parent, out string value)
    {
        value = ""; bool found = false;
        foreach (var pack in Packs.Values.Where(p => p.LanguageId == language))
            foreach (var entry in pack.Entries)
                if (entry.Section == section && entry.Key == key && entry.Parent == parent) { value=entry.Value; found=true; }
        return found;
    }
    internal static string ResolveInline(string? chinese, string? english)
    {
        int language = CurrentLanguage;
        if (chinese is not null && InlineTranslations.TryGetValue((language,chinese),out var translated)) return translated;
        if (english is not null && InlineTranslations.TryGetValue((language,english),out var alias)) return alias;
        return english ?? chinese ?? "";
    }
}
[BepInPlugin(Id,"Pathless Woods Mod Framework","0.1.2")]
public sealed class FrameworkPlugin : BasePlugin
{
    public const string Id = "superman4eg.pathlesswoods.framework";
    public override void Load()
    {
        Localization.Log=Log;
        ModRegistry.Register(new(Id,"Pathless Woods Mod Framework","0.1.2","Mod discovery, in-game settings and localization services.","SuperMan4eg"));
        FontSupport.EnableCyrillicFallback();
        AddComponent<FrameworkDispatcher>();
        AddComponent<ModsMenuController>();
        AddComponent<FontMonitor>();
        AddComponent<LanguageUIUpdate>();
        AddComponent<UILayoutCorrections>();
        AddComponent<ReportedControlLayout>();
        AddComponent<UIFormattingEvents>();
        LanguageHooks.Install(new Harmony(Id));
        NativeModsLauncher.Install(new Harmony(Id));
        Log.LogInfo("Framework 0.1.2 initialized.");
    }
}
