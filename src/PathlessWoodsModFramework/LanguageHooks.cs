using AniYa;
using AniYa.Tool;
using AniYa.UI;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;

namespace PathlessWoodsModFramework;
internal static class LanguageHooks
{
    internal static void Install(Harmony harmony)
    {
        void Patch(Type type,string name,string hook,bool prefix=false) => harmony.Patch(AccessTools.Method(type,name),
            prefix:prefix ? new HarmonyMethod(typeof(LanguageHooks),hook) : null,
            postfix:prefix ? null : new HarmonyMethod(typeof(LanguageHooks),hook));
        Patch(typeof(LocalizationBinaryTool),nameof(LocalizationBinaryTool.Load),nameof(BaseCatalog),true);
        Patch(typeof(GameSettingData),nameof(GameSettingData.GetUserLanguage),nameof(SelectedLanguage),true);
        Patch(typeof(MultiLangManager),nameof(MultiLangManager.GetText),nameof(TextLookup),true);
        Patch(typeof(MultiLangManager),nameof(MultiLangManager.GetPopup),nameof(PopupLookup),true);
        Patch(typeof(MultiLangManager),nameof(MultiLangManager.GetUnitName),nameof(UnitNameLookup),true);
        Patch(typeof(MultiLangManager),nameof(MultiLangManager.GetRandomName),nameof(RandomNameLookup),true);
        Patch(typeof(MultiLangManager),nameof(MultiLangManager.GetNpcDialog),nameof(NpcGroupDialogLookup),true);
        Patch(typeof(MultiLangManager),nameof(MultiLangManager.GetUnitDescript),nameof(UnitDescriptionLookup),true);
        Patch(typeof(MultiLangManager),nameof(MultiLangManager.GetBuffName),nameof(BuffNameLookup),true);
        Patch(typeof(MultiLangManager),nameof(MultiLangManager.GetBuffDesc2),nameof(BuffReferenceLookup),true);
        harmony.Patch(AccessTools.Method(typeof(MultiLangManager),nameof(MultiLangManager.GetBuffDesc),new[]{typeof(string)}),prefix:new HarmonyMethod(typeof(LanguageHooks),nameof(BuffDescriptionLookup)));
        harmony.Patch(AccessTools.Method(typeof(MultiLangManager),nameof(MultiLangManager.GetBuffDesc),new[]{typeof(string),typeof(UserLanguage)}),prefix:new HarmonyMethod(typeof(LanguageHooks),nameof(BuffDescriptionLanguageLookup)));
        Patch(typeof(MultiLangManager),nameof(MultiLangManager.TryBytes),nameof(SectionLookup),true);
        harmony.Patch(AccessTools.Method(typeof(MultiLangManager),nameof(MultiLangManager.GetDialog),new[]{typeof(string)}),prefix:new HarmonyMethod(typeof(LanguageHooks),nameof(DialogLookup)));
        harmony.Patch(AccessTools.Method(typeof(MultiLangManager),nameof(MultiLangManager.GetDialog),new[]{typeof(string),typeof(UserLanguage)}),prefix:new HarmonyMethod(typeof(LanguageHooks),nameof(DialogLanguageLookup)));
        Patch(typeof(MultiLangString),nameof(MultiLangString.GetByUserLanguage),nameof(LanguageString),true);
        Patch(typeof(MultiLangString),"get_V",nameof(CachedString),true);
        Patch(typeof(MultiLangTMP),nameof(MultiLangTMP.OnLanguageChange),nameof(TmpInline));
        Patch(typeof(MultiLanguage),nameof(MultiLanguage.OnLanguageChange),nameof(LegacyInline));
        Patch(typeof(TextResize),nameof(TextResize.SetText),nameof(ShortCaptionResize));
        Patch(typeof(MultiLangManager),nameof(MultiLangManager.GetRoleDescription),nameof(RoleDescriptionLookup),true);
        Patch(typeof(MultiLangManager),nameof(MultiLangManager.GetCategory),nameof(CategoryLookup),true);
        RenderedTextHooks.Install(harmony);
    }
    private static void ShortCaptionResize(TextResize __instance,string __0)
    {
        CaptionLayoutState.Remember(__instance,__0);
        if(!Localization.HasLanguage(Localization.CurrentLanguage) || string.IsNullOrWhiteSpace(__0) || __0.Length>50 || __0.Contains('\n') || !__0.Any(c=>c>='А' && c<='я' || c is 'Ё' or 'ё')) return;
        if(__instance.tmpText is not null)
        {
            var text=__instance.tmpText;
            text.text=__0;
            text.enableAutoSizing=true;
            text.fontSizeMax=Math.Max(22,__instance.targetSize);
            text.fontSizeMin=14;
            text.enableWordWrapping=false;
            text.overflowMode=TextOverflowModes.Ellipsis;
        }
        if(__instance.text is not null)
        {
            var text=__instance.text;
            text.text=__0;
            text.resizeTextMaxSize=Math.Max(22,__instance.targetSize);
            text.resizeTextMinSize=14;
            text.resizeTextForBestFit=true;
            text.horizontalOverflow=UnityEngine.HorizontalWrapMode.Overflow;
            text.verticalOverflow=UnityEngine.VerticalWrapMode.Truncate;
        }
    }
    private static void BaseCatalog(ref UserLanguage __0) { if(Localization.HasLanguage((int)__0)) __0=(UserLanguage)2; }
    private static bool SelectedLanguage(GameSettingData __instance,ref UserLanguage __result)
    {
        if(!Localization.HasLanguage((int)__instance.GameLanguage)) return true;
        __result=__instance.GameLanguage;return false;
    }
    private static bool TextLookup(string __0,ref string __result)
    {
        int language=Localization.CurrentLanguage;
        if(!Localization.HasLanguage(language)) return true;
        if(Localization.TryTranslation(language,1,__0,null,out var translated)) {__result=translated;return false;}
        var section=LocalizationBinaryTool.GetSection((LocSectionId)1);
        __result=section?.ContainsKey(__0)==true ? section[__0] : __0;return false;
    }
    private static bool PopupLookup(string __0,ref string __result)
    {
        int language=Localization.CurrentLanguage;
        if(!Localization.HasLanguage(language) || !Localization.TryTranslation(language,7,__0,null,out var translated)) return true;
        __result=translated;return false;
    }
    private static bool UnitNameLookup(string __0,ref string __result)
    {
        int language=Localization.CurrentLanguage;
        if(!Localization.HasLanguage(language) || !Localization.TryTranslation(language,2,__0,null,out var translated)) return true;
        __result=translated;return false;
    }
    private static bool UnitDescriptionLookup(string __0,ref string __result)
    {
        int language=Localization.CurrentLanguage;
        if(!Localization.HasLanguage(language) || !Localization.TryTranslation(language,3,__0,null,out var translated)) return true;
        __result=translated;return false;
    }
    private static bool RoleDescriptionLookup(string __0,ref string __result) => LookupRegisteredSection(9,__0,ref __result);
    private static bool CategoryLookup(string __0,ref string __result) => LookupRegisteredSection(10,__0,ref __result);
    private static bool LookupRegisteredSection(int section,string key,ref string result)
    {
        int language=Localization.CurrentLanguage;
        if(!Localization.HasLanguage(language) || !Localization.TryTranslation(language,section,key,null,out var translated))return true;
        result=translated;return false;
    }
    private static bool NpcGroupDialogLookup(string __0,string __1,ref string __result)
    {
        int language=Localization.CurrentLanguage;
        if(!Localization.HasLanguage(language) || !Localization.TryTranslation(language,23,__1,__0,out var translated)) return true;
        __result=translated;
        return false;
    }
    private static bool RandomNameLookup(string __0,ref string __result)
    {
        int language=Localization.CurrentLanguage;
        if(!Localization.HasLanguage(language) || !Localization.TryTranslation(language,22,__0,null,out var translated)) return true;
        __result=translated;return false;
    }
    private static bool BuffNameLookup(string __0,ref string __result)
    {
        int language=Localization.CurrentLanguage;
        if(!Localization.HasLanguage(language) || !Localization.TryTranslation(language,11,__0,null,out var translated)) return true;
        __result=translated;return false;
    }
    private static bool BuffDescriptionLookup(string __0,ref string __result) => LookupBuffDescription(Localization.CurrentLanguage,__0,ref __result);
    private static bool BuffReferenceLookup(string __0,ref string __result)
    {
        int language=Localization.CurrentLanguage;
        if(!Localization.HasLanguage(language) || !Localization.TryTranslation(language,13,__0,null,out var translated)) return true;
        __result=translated;return false;
    }
    private static bool BuffDescriptionLanguageLookup(string __0,UserLanguage __1,ref string __result) => LookupBuffDescription((int)__1,__0,ref __result);
    private static bool LookupBuffDescription(int language,string key,ref string result)
    {
        if(!Localization.HasLanguage(language) || !Localization.TryTranslation(language,12,key,null,out var translated)) return true;
        result=translated;return false;
    }
    private static bool DialogLookup(string __0,ref string __result) => LookupDialog(Localization.CurrentLanguage,__0,ref __result);
    private static bool DialogLanguageLookup(string __0,UserLanguage __1,ref string __result) => LookupDialog((int)__1,__0,ref __result);
    private static bool LookupDialog(int language,string key,ref string result)
    {
        if(!Localization.HasLanguage(language)) return true;
        if(Localization.TryTranslation(language,0,key,null,out var translated)){result=translated;return false;}
        var section=LocalizationBinaryTool.GetSection((LocSectionId)0);
        result=section?.ContainsKey(key)==true ? section[key] : key;return false;
    }
    private static bool SectionLookup(LocSectionId __0,string __1,string __2,ref string __3,ref bool __result)
    {
        int language=Localization.CurrentLanguage;
        if(!Localization.HasLanguage(language)) return true;
        if(Localization.TryTranslation(language,(int)__0,__1,null,out var translated)){__3=translated;__result=true;return false;}
        var section=LocalizationBinaryTool.GetSection(__0);
        if(section?.ContainsKey(__1)==true){__3=section[__1];__result=true;return false;}
        __3=__2;__result=false;return false;
    }
    private static void TmpInline(MultiLangTMP __instance)
    {
        if(Localization.HasLanguage(Localization.CurrentLanguage) && __instance.TargetText is not null) __instance.TargetText.text=Localization.ResolveInline(__instance.TextSC,__instance.TextEN);
    }
    private static void LegacyInline(MultiLanguage __instance)
    {
        if(Localization.HasLanguage(Localization.CurrentLanguage) && __instance.TargetText is not null) __instance.TargetText.text=Localization.ResolveInline(__instance.TextSC,__instance.TextEN);
    }
    private static bool LanguageString(MultiLangString __instance,UserLanguage __0,ref string __result)
    {
        if(!Localization.HasLanguage((int)__0)) return true;
        __result=Localization.ResolveInline(__instance.s_chinese,__instance.english);return false;
    }
    private static bool CachedString(MultiLangString __instance,ref string __result)
    {
        if(!Localization.HasLanguage(Localization.CurrentLanguage)) return true;
        __result=Localization.ResolveInline(__instance.s_chinese,__instance.english);return false;
    }
    internal static void SettingsOptions(SettingUI __instance)
    {
        try
        {
            var dropdown=__instance.Dropdown_Language;
            if(dropdown is null || __instance.lanArrs is null) return;
            var values=__instance.lanArrs.ToList();
            foreach(var language in Localization.Packs.Values.GroupBy(p=>p.LanguageId).Select(g=>g.First()))
            {
                int index=values.FindIndex(v=>(int)v==language.LanguageId);
                if(index<0){index=values.Count;values.Add((UserLanguage)language.LanguageId);}
                while(dropdown.options.Count<=index) dropdown.options.Add(new TMP_Dropdown.OptionData(""));
                dropdown.options[index].text=language.DisplayName;
            }
            bool extended=values.Count!=__instance.lanArrs.Length;
            if(extended) __instance.lanArrs=new Il2CppStructArray<UserLanguage>(values.ToArray());
            if(extended){
                var data=__instance.m_SettingData ?? GameSettingData.Instance;
                int selected=data is null ? -1 : values.FindIndex(v=>v==data.GetUserLanguage());
                if(selected>=0) dropdown.SetValueWithoutNotify(selected);
            }
            dropdown.RefreshShownValue();
        }
        catch(Exception e){Localization.Log?.LogError($"Language dropdown extension failed: {e}");}
    }
}
