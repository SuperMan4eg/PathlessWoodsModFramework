using AniYa;
using AniYa.Tool;
using TMPro;
using System.Text.RegularExpressions;

namespace PathlessWoodsModFramework;
// Native callers can bypass small localization wrappers. Resolve exact known source
// strings at the renderer boundary, without scanning dictionaries on every frame.
internal static class SourceTextTranslations
{
    private static readonly Dictionary<(int Language,string Source),string> Values=new();
    private static readonly Dictionary<int,(string Original,string Translated)> PlantLabels=new();
    private static bool ready;
    private sealed record Template(Regex Pattern,string Russian,Dictionary<string,string> Groups);
    private static readonly Dictionary<(int Language,string Prefix),List<Template>> Templates=new();
    private static readonly HashSet<int> SeedLengths=new();
    internal static void Invalidate(){ready=false;Values.Clear();PlantSources.Clear();Templates.Clear();SeedLengths.Clear();}
    private static void EnsureIndex()
    {
        if(ready)return;
        var sections=new Dictionary<int,Il2CppSystem.Collections.Generic.Dictionary<string,string>>();
        foreach(int id in new[]{0,4,5,6})
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
                if(entry.Section==6)AddTemplate(pack.LanguageId,source,entry.Value);
            }
        foreach(var key in conflicts)Values.Remove(key);
        ready=true;
    }
    internal static string Resolve(string value)
    {
        if(string.IsNullOrEmpty(value))return value;
        const string emptyPrefix="<noparse></noparse>";
        int start=value.StartsWith(emptyPrefix,StringComparison.Ordinal)?emptyPrefix.Length:0;
        int end=value.Length;
        while(end>start && value[end-1] is '\r' or '\n')end--;
        if(start==0 && end==value.Length)return ResolveCore(value);
        string translated=ResolveCore(value[start..end]);
        return value[..start]+translated+value[end..];
    }
    private static string ResolveCore(string value)
    {
        int language=Localization.CurrentLanguage;
        if(!Localization.HasLanguage(language) || string.IsNullOrEmpty(value))return value;
        EnsureIndex();
        if(Values.TryGetValue((language,value),out var translated))return translated;
        if(value.Length>=3 && value.Length<=8192)
        {
            string? result=null;
            var seen=new HashSet<Template>();
            foreach(int length in SeedLengths)
            for(int offset=0;offset<=value.Length-length;offset++)
            {
                if(!Templates.TryGetValue((language,value.Substring(offset,length)),out var candidates))continue;
                foreach(var template in candidates)
                {
                    if(!seen.Add(template))continue;
                    Match match;
                    try{match=template.Pattern.Match(value);}catch(RegexMatchTimeoutException){continue;}
                    if(!match.Success)continue;
                    string current=template.Russian;
                    foreach(var pair in template.Groups)current=current.Replace(pair.Key,match.Groups[pair.Value].Value,StringComparison.Ordinal);
                    if(result!=null && result!=current)return value;
                    result=current;
                }
            }
            if(result!=null)return result;
        }
        return Localization.ResolveInline(value,value);
    }
    private static void AddTemplate(int language,string source,string russian)
    {
        var tokens=Regex.Matches(source,@"\{(?:id|input_key):[^{}]+\}");
        if(tokens.Count==0)return;
        var translatedTokens=Regex.Matches(russian,@"\{(?:id|input_key):[^{}]+\}").Select(m=>m.Value).OrderBy(x=>x).ToArray();
        if(!tokens.Select(m=>m.Value).Distinct().OrderBy(x=>x).SequenceEqual(translatedTokens.Distinct()))return;
        string pattern=@"\A";int position=0;var groups=new Dictionary<string,string>();
        foreach(Match token in tokens)
        {
            pattern+=Regex.Escape(source[position..token.Index]);
            if(groups.TryGetValue(token.Value,out var existing))pattern+=@"\k<"+existing+">";
            else{string group="v"+groups.Count;groups[token.Value]=group;pattern+="(?<"+group+">.{1,1024}?)";}
            position=token.Index+token.Length;
        }
        pattern+=Regex.Escape(source[position..])+@"\z";
        string? seed=Regex.Split(source,@"\{(?:id|input_key):[^{}]+\}").OrderByDescending(s=>s.Length).FirstOrDefault(s=>s.Length>=3);
        if(seed==null)return;
        int length=Math.Min(8,seed.Length);SeedLengths.Add(length);
        var key=(language,seed[..length]);
        if(!Templates.TryGetValue(key,out var values))Templates[key]=values=new();
        values.Add(new(new Regex(pattern,RegexOptions.CultureInvariant|RegexOptions.Singleline,TimeSpan.FromMilliseconds(20)),russian,groups));
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
