using AniYa;
using AniYa.UI;
using AniYa.UI.Craft;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using System.Text.RegularExpressions;
namespace PathlessWoodsModFramework;
internal static class CraftingCaptionLayout
{
    private static readonly HashSet<int> HelpLabels=new();
    private static readonly HashSet<int> EvaluationLabels=new();
    private static readonly HashSet<int> OptionLabels=new();
    internal static void Install(Harmony h)
    {
        foreach(var method in typeof(CraftItemInfoPanel).GetMethods().Where(m=>m.Name is "ShowHelpPanel" or "Refresh"))
            h.Patch(method,postfix:new HarmonyMethod(typeof(CraftingCaptionLayout),nameof(CraftHelpRefreshed)));
        h.Patch(AccessTools.Method(typeof(KeyboardMouseHelpContent),nameof(KeyboardMouseHelpContent.RefreshPage)),postfix:new HarmonyMethod(typeof(CraftingCaptionLayout),nameof(HelpRefreshed)));
        h.Patch(AccessTools.Method(typeof(QuickTimeEventChopUI),nameof(QuickTimeEventChopUI.Hit)),postfix:new HarmonyMethod(typeof(CraftingCaptionLayout),nameof(ChopHit)));
        h.Patch(AccessTools.Method(typeof(QuickTimeEventDigUI),nameof(QuickTimeEventDigUI.Hit)),postfix:new HarmonyMethod(typeof(CraftingCaptionLayout),nameof(DigHit)));
        h.Patch(AccessTools.Method(typeof(DialogueOptionItem),nameof(DialogueOptionItem.Set)),postfix:new HarmonyMethod(typeof(CraftingCaptionLayout),nameof(OptionSet)));
    }
    private static void CraftHelpRefreshed(CraftItemInfoPanel __instance)
    {
        if(__instance.HelpPanelRect is null)return;
        foreach(var text in __instance.HelpPanelRect.GetComponentsInChildren<TMP_Text>(true))
        {HelpLabels.Add(text.GetInstanceID());Apply(text);}
    }
    private static string ResolveHelpCaption(string value)
    {
        string plain=Regex.Replace(value,"<[^>]*>","").Trim();
        if(plain is not ("Lock" or "Hold to mark" or "Craft" or "Hold to batch-craft"))return Localization.ResolveInline(value,value);
        string translated=Localization.ResolveInline(plain,plain);
        // Translate only the known caption; preserve its surrounding color/style tags and spacing.
        int start=value.IndexOf(plain,StringComparison.Ordinal);
        return start>=0 ? value[..start]+translated+value[(start+plain.Length)..] : translated;
    }
    private static void HelpRefreshed(KeyboardMouseHelpContent __instance)
    {
        if(__instance._rows==null)return;
        foreach(var row in __instance._rows)if(row?.description is { } text){HelpLabels.Add(text.GetInstanceID());Apply(text);}
    }
    private static void ChopHit(QuickTimeEventChopUI __instance)=>BindEvaluation(__instance.EvaluateText);
    private static void DigHit(QuickTimeEventDigUI __instance)=>BindEvaluation(__instance.EvaluateText);
    private static void BindEvaluation(TMP_Text? text)
    {if(text!=null){EvaluationLabels.Add(text.GetInstanceID());Apply(text);}}
    private static void OptionSet(DialogueOptionItem __instance)
    {if(__instance.optionText is { } text){OptionLabels.Add(text.GetInstanceID());if(Localization.HasLanguage(Localization.CurrentLanguage))text.text=SourceTextTranslations.Resolve(text.text);}}
    private static bool Craft(Component text)=>text.GetComponentInParent<CraftUI>(true)!=null || text.GetComponentInParent<ProductionUI>(true)!=null || text.GetComponentInParent<PlayerStateUI>(true)!=null || text.GetComponentInParent<OperationTipsUI>(true)!=null;
    private static bool Evaluation(Component text)=>text.GetComponentInParent<QuickTimeEventChopUI>(true)!=null || text.GetComponentInParent<QuickTimeEventDigUI>(true)!=null;
    private static bool Action(string s)=>s is "Изготовить" or "Отметить" or "Массовое изготовление";
    internal static void Apply(TMP_Text text)
    {
        if(text==null || !Localization.HasLanguage(Localization.CurrentLanguage))return;
        if(text.GetComponentInParent<DialogueOptionItem>(true)!=null || OptionLabels.Contains(text.GetInstanceID()))
        {string option=SourceTextTranslations.Resolve(text.text);if(option!=text.text)text.text=option;}
        if(Craft(text) || text.GetComponentInParent<KeyboardMouseHelpContent>(true)!=null || HelpLabels.Contains(text.GetInstanceID()))
        {
            string translated=ResolveHelpCaption(text.text);
            if(translated!=text.text)text.text=translated;
            if((HelpLabels.Contains(text.GetInstanceID()) || text.GetComponentInParent<KeyboardMouseHelpContent>(true)!=null) && Regex.Replace(text.text,"<[^>]*>","").Trim() is "Закрепить" or "Удерживайте для отметки" or "Изготовить" or "Удерживайте для массового изготовления")
            {CaptionLayoutState.Fit(text,26,true);text.alignment=TextAlignmentOptions.Left;return;}
            if(!Action(text.text))return;
            var button=text.GetComponentInParent<Button>(true);
            if(button==null)return;
            ReportedControlLayout.ExpandButtonCaption(text);
            CaptionLayoutState.Fit(text,30,text.text=="Массовое изготовление");
            text.alignment=TextAlignmentOptions.Center;
        }
        else if(Evaluation(text) || EvaluationLabels.Contains(text.GetInstanceID()))
        {
            string plain=Regex.Replace(text.text,"<[^>]*>","");
            if(plain is not ("Отлично" or "тлично" or "Нормально" or "ормально"))return;
            string caption=plain is "Нормально" or "ормально" ? "Нормально" : "Отлично";
            if(text.text!=caption)text.text=caption;
            EvaluationFontSupport.Apply(text);
            text.firstVisibleCharacter=0;text.maxVisibleCharacters=int.MaxValue;text.maxVisibleWords=int.MaxValue;text.maxVisibleLines=int.MaxValue;
            text.maskable=false;
            float width=Math.Max(text.rectTransform.rect.width,Math.Max(160,text.GetPreferredValues(text.text).x+16));
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);
            text.margin=new Vector4(8,0,8,0);text.alignment=TextAlignmentOptions.Center;
            text.overflowMode=TextOverflowModes.Overflow;
        }
    }
    internal static bool Apply(Text text)
    {
        if(text==null || !Localization.HasLanguage(Localization.CurrentLanguage))return false;
        if(Craft(text))
        {
            text.text=Localization.ResolveInline(text.text,text.text);
            if(!Action(text.text) || text.GetComponentInParent<Button>(true) is null)return false;
            text.resizeTextForBestFit=true;text.resizeTextMinSize=10;text.resizeTextMaxSize=30;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            return true;
        }
        if(Evaluation(text) && text.text=="Отлично")
        {
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Math.Max(text.rectTransform.rect.width,Math.Max(160,text.preferredWidth+16)));
            text.horizontalOverflow=HorizontalWrapMode.Overflow;return true;
        }
        return false;
    }
}
