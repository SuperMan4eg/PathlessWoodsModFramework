using AniYa;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
namespace PathlessWoodsModFramework;
// Keep the supplied caption separately from native TextResize's formatted output.
internal static class CaptionLayoutState
{
    private sealed record Caption(string Text,float Maximum);
    private static readonly Dictionary<int,Caption> Supplied=new();
    private static readonly Dictionary<string,string> Known=new(StringComparer.Ordinal);
    private static int knownCount;
    private static string Compact(string s)=>Regex.Replace(Regex.Replace(s,"<[^>]*>",""),@"\s+","");
    internal static string Canonical(string value)
    {
        int count=Localization.Packs.Values.Sum(p=>p.Entries.Length);
        if(count!=knownCount)
        {
            Known.Clear();knownCount=count;
            foreach(var pack in Localization.Packs.Values.Where(p=>p.LanguageId==Localization.CurrentLanguage))
                foreach(var row in pack.Entries)
                    if(row.Value.Length<=100 && !row.Value.Contains('\n') && Regex.IsMatch(row.Value,"[А-Яа-яЁё]"))Known.TryAdd(Compact(row.Value),row.Value);
        }
        return Known.TryGetValue(Compact(value),out var canonical)?canonical:value;
    }
    internal static void Remember(TextResize resize,string value)
    {
        if(!Localization.HasLanguage(Localization.CurrentLanguage)||string.IsNullOrWhiteSpace(value)||value.Length>100)return;
        if(!Regex.IsMatch(value,"[А-Яа-яЁё]"))return;
        int id=resize.GetInstanceID();
        if(value.Contains('\n') && (!Supplied.TryGetValue(id,out var old)||Compact(old.Text)!=Compact(value)))return;
        if(!value.Contains('\n'))Supplied[id]=new(value,Math.Clamp(resize.targetSize,22,40));
        Apply(resize);
    }
    internal static void Apply(TextResize resize)
    {
        if(!Localization.HasLanguage(Localization.CurrentLanguage)||!Supplied.TryGetValue(resize.GetInstanceID(),out var saved))return;
        if(resize.tmpText is { } tmp)
        {
            if(Compact(tmp.text)!=Compact(saved.Text))return;
            tmp.text=saved.Text;Fit(tmp,saved.Maximum);
        }
        if(resize.text is { } text)
        {
            if(Compact(text.text)!=Compact(saved.Text))return;
            text.text=saved.Text;text.resizeTextMaxSize=(int)saved.Maximum;text.resizeTextMinSize=12;
            text.resizeTextForBestFit=true;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
        }
    }
    internal static void Fit(TMP_Text text,float maximum,bool wrap=false)
    {
        // Never derive the maximum from the already fitted (possibly tiny) size.
        text.fontSizeMax=maximum;text.fontSizeMin=Math.Min(14,maximum);text.enableAutoSizing=true;
        text.enableWordWrapping=wrap;text.overflowMode=TextOverflowModes.Ellipsis;
        var m=text.margin;text.margin=new Vector4(Math.Min(4,Math.Max(0,m.x)),Math.Min(2,Math.Max(0,m.y)),Math.Min(4,Math.Max(0,m.z)),Math.Min(2,Math.Max(0,m.w)));
    }
}
