using AniYa;
using AniYa.UI;
using AniYa.UI.Craft;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace PathlessWoodsModFramework;
// Target changed/activated widgets. No scene discovery in this path.
internal sealed class UIFormattingEvents:MonoBehaviour
{
    public UIFormattingEvents(IntPtr p):base(p){}
    private static readonly Dictionary<int,TMP_Text> Pending=new();
    private static bool processing;
    internal const string NightEnglish="The night has fallen completely! Resting in Bed or by a lit Bonfire can restore your Spirit and allow you to sleep through the night.";
    private static readonly Dictionary<int,string> NightOriginals=new();
    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.PropertySetter(typeof(TMP_Text),"text"),postfix:new HarmonyMethod(typeof(UIFormattingEvents),nameof(TextChanged)));
        foreach(string method in new[]{"OnEnable","OnRectTransformDimensionsChange"})
            h.Patch(AccessTools.Method(typeof(TextMeshProUGUI),method),postfix:new HarmonyMethod(typeof(UIFormattingEvents),nameof(TextChanged)));
        h.Patch(AccessTools.Method(typeof(TextMeshProUGUI),nameof(TextMeshProUGUI.Rebuild)),prefix:new HarmonyMethod(typeof(UIFormattingEvents),nameof(BeforeRender)));
        foreach(string method in new[]{"OnPreRenderCanvas","GenerateTextMesh"})
            h.Patch(AccessTools.Method(typeof(TextMeshProUGUI),method),prefix:new HarmonyMethod(typeof(UIFormattingEvents),nameof(BeforeMesh)));
        foreach(string method in new[]{"OnEnable","OnOpenStart","ShowTabContent"})
            h.Patch(AccessTools.Method(typeof(SettingUI),method),postfix:new HarmonyMethod(typeof(UIFormattingEvents),nameof(SettingsShown)));
        h.Patch(AccessTools.Method(typeof(BuffPreviewItem),nameof(BuffPreviewItem.SetBuff)),postfix:new HarmonyMethod(typeof(UIFormattingEvents),nameof(StatusChanged)));
        foreach(string method in new[]{"SetBuff","SetText","ApplyLeylinePower"})
            h.Patch(AccessTools.Method(typeof(OtherBuffIcon),method),postfix:new HarmonyMethod(typeof(UIFormattingEvents),nameof(HudStatusChanged)));
        h.Patch(AccessTools.Method(typeof(PropertyItem),nameof(PropertyItem.Refresh)),postfix:new HarmonyMethod(typeof(UIFormattingEvents),nameof(PropertyChanged)));
        h.Patch(AccessTools.Method(typeof(EquipmentBuffItem),nameof(EquipmentBuffItem.Set)),postfix:new HarmonyMethod(typeof(UIFormattingEvents),nameof(EquipmentChanged)));
    }
    private static void TextChanged(TMP_Text __instance)=>Schedule(__instance);
    private static void SettingsShown(SettingUI __instance)
    {foreach(var text in __instance.GetComponentsInChildren<TMP_Text>(false))Schedule(text);}
    private static void BeforeRender(TextMeshProUGUI __instance,CanvasUpdate __0)
    {if(__0==CanvasUpdate.PreRender)BeforeMesh(__instance);}
    private static void BeforeMesh(TextMeshProUGUI __instance)
    {
        SourceTextTranslations.PlantMesh(__instance);
        if(processing || !Localization.HasLanguage(Localization.CurrentLanguage) || !ReportedControlLayout.IsTargeted(__instance))return;
        processing=true;
        try{ReportedControlLayout.ApplyEvent(__instance);}
        finally{processing=false;}
    }
    internal static void Schedule(TMP_Text text)
    {
        if(processing || text==null || !text.gameObject.activeInHierarchy)return;
        string value=text.text;
        if(string.IsNullOrEmpty(value))return;
        bool night=value==NightEnglish || NightOriginals.ContainsKey(text.GetInstanceID());
        if(!night && (value.Length>100 || !value.Any(c=>c>='А'&&c<='я'||c is 'Ё' or 'ё') && value is not ("System" or "Setting" or "Escape" or "Save & Quit" or "Quit")))return;
        Pending[text.GetInstanceID()]=text;
    }
    private static void StatusChanged(BuffPreviewItem __instance)
    {if(Localization.HasLanguage(Localization.CurrentLanguage) && __instance.buffNameText is { } text)ReportedControlLayout.FitStatusEffect(text,__instance.transform);}
    private static void HudStatusChanged(OtherBuffIcon __instance)
    {if(Localization.HasLanguage(Localization.CurrentLanguage) && __instance.buffNameText is { } text)ReportedControlLayout.FitStatusEffect(text,__instance.transform);}
    private static void PropertyChanged(PropertyItem __instance)
    {if(Localization.HasLanguage(Localization.CurrentLanguage))ReportedControlLayout.FitProperty(__instance);}
    private static void EquipmentChanged(EquipmentBuffItem __instance)
    {if(__instance.buffNameText is { } text)Schedule(text);}
    internal static void Process(TMP_Text text)
    {
        if(text==null || !text.gameObject.activeInHierarchy)return;
        if(Localization.CurrentLanguage==5 && text.text==NightEnglish && Localization.TryTranslation(5,6,"M1_chb8_soeg_rep_1_1",null,out var russian))
        {NightOriginals[text.GetInstanceID()]=russian;text.text=russian;}
        else if(Localization.CurrentLanguage!=5 && NightOriginals.TryGetValue(text.GetInstanceID(),out var previous) && text.text==previous)text.text=NightEnglish;
        if(!Localization.HasLanguage(Localization.CurrentLanguage))return;
        ReportedControlLayout.ApplyEvent(text);
    }
    public void LateUpdate()=>Flush();
    internal static void Flush()
    {
        if(processing || Pending.Count==0)return;
        var work=Pending.Take(128).ToArray();foreach(var entry in work)Pending.Remove(entry.Key);
        processing=true;
        try{foreach(var entry in work)Process(entry.Value);}
        finally{processing=false;}
    }
}
