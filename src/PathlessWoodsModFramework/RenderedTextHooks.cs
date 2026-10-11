using AniYa;
using AniYa.UI;
using AniYa.UI.Craft;
using AniYa.Template;
using HarmonyLib;
using System.Text.Json;

namespace PathlessWoodsModFramework;
internal static class RenderedTextHooks
{
    internal static void Install(Harmony h)
    {
        UIFormattingEvents.Install(h);
        h.Patch(AccessTools.Method(typeof(TextResize),nameof(TextResize.Refresh)),postfix:new HarmonyMethod(typeof(RenderedTextHooks),nameof(RefreshedCaption)));
        h.Patch(AccessTools.Method(typeof(UserManualContentItem),nameof(UserManualContentItem.RefreshText)),prefix:new HarmonyMethod(typeof(RenderedTextHooks),nameof(ManualText)),finalizer:new HarmonyMethod(typeof(RenderedTextHooks),nameof(RestoreManualSource)));
        h.Patch(AccessTools.Method(typeof(GameStartUI),nameof(GameStartUI.RefreshNoticeText)),prefix:new HarmonyMethod(typeof(RenderedTextHooks),nameof(NoticeLanguage)),postfix:new HarmonyMethod(typeof(RenderedTextHooks),nameof(NoticeText)),finalizer:new HarmonyMethod(typeof(RenderedTextHooks),nameof(RestoreNoticeLanguage)));
        h.Patch(AccessTools.Method(typeof(TechLinkTooltipUI),nameof(TechLinkTooltipUI.Show)),prefix:new HarmonyMethod(typeof(RenderedTextHooks),nameof(ConstructionHint)),postfix:new HarmonyMethod(typeof(RenderedTextHooks),nameof(TooltipLayout)));
        h.Patch(AccessTools.Method(typeof(OperationTipsUI),nameof(OperationTipsUI.Update)),postfix:new HarmonyMethod(typeof(RenderedTextHooks),nameof(OperationHelpPosition)));
    }
    private static void RefreshedCaption(TextResize __instance)
    {
        CaptionLayoutState.Apply(__instance);
        if(__instance.tmpText is not null)ReportedControlLayout.ApplyEvent(__instance.tmpText);
        if(__instance.tmpText is not null)UIFormattingEvents.Schedule(__instance.tmpText);
        if(__instance.text is not null)ReportedControlLayout.ApplyLegacy(__instance.text);
    }
    private sealed record ManualSource(MultiLangString Value,string? English);
    private static void ManualText(UserManualHTMLTemp __0,ref UserLanguage __1,out ManualSource? __state)
    {
        __state=null;
        if(!Localization.HasLanguage((int)__1) || __0?.contentString is null)return;
        var source=__0.contentString;
        if(source.s_chinese is null || !Localization.TryTranslation((int)__1,1,source.s_chinese,null,out var value))return;
        // Let the original renderer process links, input tokens and layout using its normal text path.
        __state=new(source,source.english);source.english=value;__1=(UserLanguage)2;
    }
    private static Exception? RestoreManualSource(Exception? __exception,ManualSource? __state)
    {
        if(__state is not null)__state.Value.english=__state.English;
        return __exception;
    }
    private static void NoticeLanguage(out int __state)
    {
        __state=-1;var data=GameSettingData._Instance_k__BackingField;
        if(data is null || !Localization.HasLanguage(Localization.CurrentLanguage))return;
        __state=(int)data.GameLanguage;data.GameLanguage=(UserLanguage)2;
    }
    private static Exception? RestoreNoticeLanguage(Exception? __exception,int __state)
    {
        if(__state>=0 && GameSettingData._Instance_k__BackingField is { } data)data.GameLanguage=(UserLanguage)__state;
        return __exception;
    }
    private static void NoticeText(GameStartUI __instance,int __state)
    {
        if(__state>=0 && GameSettingData._Instance_k__BackingField is { } data)data.GameLanguage=(UserLanguage)__state;
        if(!Localization.HasLanguage(Localization.CurrentLanguage) || __instance.NoticeText is null || __instance._noticeData is null)return;
        string rendered=__instance.NoticeText.text??"";
        using var json=JsonDocument.Parse(__instance._noticeData.ToString());
        if(json.RootElement.ValueKind!=JsonValueKind.Array)return;
        foreach(var item in json.RootElement.EnumerateArray())
            foreach(string field in new[]{"title","content"})
            {
                if(!item.TryGetProperty(field,out var texts) || texts.ValueKind!=JsonValueKind.Array || texts.GetArrayLength()<3)continue;
                string? cn=texts[0].GetString(),en=texts[2].GetString();
                if(string.IsNullOrEmpty(cn) || string.IsNullOrEmpty(en))continue;
                if(Localization.TryTranslation(Localization.CurrentLanguage,1,cn,null,out var ru))rendered=rendered.Replace(en,ru,StringComparison.Ordinal);
            }
        if(rendered!=__instance.NoticeText.text)__instance.NoticeText.text=rendered;
    }
    private static void ConstructionHint(ref string __1)
    {
        if(Localization.HasLanguage(Localization.CurrentLanguage))__1=Localization.ResolveInline(__1,__1);
    }
    private static void TooltipLayout(TechLinkTooltipUI __instance) => ReportedControlLayout.FitTooltip(__instance);
    private static void OperationHelpPosition(OperationTipsUI __instance)
    {if(Localization.HasLanguage(Localization.CurrentLanguage))ReportedControlLayout.PollHelp(__instance.transform);}
}
