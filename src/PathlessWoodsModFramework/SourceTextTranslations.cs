using AniYa;
using AniYa.Tool;
using TMPro;

namespace PathlessWoodsModFramework;
// Native callers can bypass small localization wrappers. Resolve exact known source
// strings at the renderer boundary, without scanning dictionaries on every frame.
internal static class SourceTextTranslations
{
    private static readonly Dictionary<(int Language,string Source),string> Values=new();
    private static readonly Dictionary<int,(string Original,string Translated)> PlantLabels=new();
    private static bool ready;
    internal static void Invalidate(){ready=false;Values.Clear();PlantSources.Clear();}
    private static void EnsureIndex()
    {
        if(ready)return;
        var sections=new Dictionary<int,Il2CppSystem.Collections.Generic.Dictionary<string,string>>();
        foreach(int id in new[]{4,5,6})
        {
            var section=LocalizationBinaryTool.GetSection((LocSectionId)id);
            if(section is null)return;
            sections[id]=section;
        }
        var conflicts=new HashSet<(int Language,string Source)>();
        foreach(var pack in Localization.Packs.Values)
            foreach(var entry in pack.Entries)
            {
                if(entry.Parent is not null || !sections.TryGetValue(entry.Section,out var section) || !section.ContainsKey(entry.Key))continue;
                string source=section[entry.Key];
                if(string.IsNullOrEmpty(source))continue;
                if(entry.Section is 4 or 5)PlantSources.Add(source);
                var key=(pack.LanguageId,source);
                if(Values.TryGetValue(key,out var previous) && previous!=entry.Value)conflicts.Add(key);
                else Values[key]=entry.Value;
            }
        foreach(var key in conflicts)Values.Remove(key);
        ready=true;
    }
    internal static string Resolve(string value)
    {
        int language=Localization.CurrentLanguage;
        if(!Localization.HasLanguage(language) || string.IsNullOrEmpty(value))return value;
        EnsureIndex();
        return Values.TryGetValue((language,value),out var translated)?translated:Localization.ResolveInline(value,value);
    }
    internal static void PlantMesh(TextMeshProUGUI text)
    {
        if(text==null)return;
        int id=text.GetInstanceID();string value=text.text;
        if(!Localization.HasLanguage(Localization.CurrentLanguage))
        {
            if(PlantLabels.Remove(id,out var old) && value==old.Translated)text.text=old.Original;
            return;
        }
        // Only plant-name sources reach this path. Dialogue is translated before animation.
        if(PlantLabels.TryGetValue(id,out var previous) && value==previous.Translated)return;
        PlantLabels.Remove(id);
        EnsureIndex();
        if(!PlantSources.Contains(value))return;
        string translated=Resolve(value);
        if(translated==value)return;
        if(PlantLabels.Count>=2048)PlantLabels.Clear();
        PlantLabels[id]=(value,translated);text.text=translated;
    }
    private static readonly HashSet<string> PlantSources=new(StringComparer.Ordinal);
}
