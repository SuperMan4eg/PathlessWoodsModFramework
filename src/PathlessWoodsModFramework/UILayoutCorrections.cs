using AniYa.UI;
using AniYa;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PathlessWoodsModFramework;

// Restricted to the native controls reported by the owner. Applies in every language.
internal sealed class UILayoutCorrections : MonoBehaviour
{
    public UILayoutCorrections(IntPtr pointer) : base(pointer) { }
    private float next;
    public void LateUpdate()
    {
        if(Time.unscaledTime<next) return;
        next=Time.unscaledTime+0.5f;
        foreach(var ui in UIObjectCache.Active<SettingUI>())
        {
            var apply=ui.transform.Find("Panel/Bottom/Btns/OKBtn/Text (TMP)")?.GetComponent<TMP_Text>();
            if(apply is not null) FitSingleLine(apply);
            var attack=ui.transform.Find("Panel/Contents/TabView/Contents1/AttackFacing/Dropdown")?.GetComponent<TMP_Dropdown>();
            if(attack?.captionText is not null) FitSingleLine(attack.captionText);
            if(attack?.itemText is not null) FitSingleLine(attack.itemText);
        }
        foreach(var text in UIObjectCache.Active<TMP_Text>())
        {
            if(IsConnectCaption(text.text))
            {
                text.text=MultiLangManager.Instance.GetDialog("join_world_menus_done");
                FitSingleLine(text);
                text.fontSizeMax=32;
                text.fontSizeMin=18;
                text.alignment=TextAlignmentOptions.MidlineLeft;
            }
            if(IsStatusCaption(text.text) && IsRoleStatusControl(text.transform))
            {
                text.text=StatusCaption();
                text.fontSizeMax=22;
                text.enableAutoSizing=true;
                text.fontSizeMin=10;
                text.enableWordWrapping=false;
                text.alignment=TextAlignmentOptions.MidlineLeft;
                text.overflowMode=TextOverflowModes.Truncate;
                text.margin=new Vector4(4,4,4,4);
            }
        }
        foreach(var text in UIObjectCache.Active<Text>())
            if(IsStatusCaption(text.text) && IsRoleStatusControl(text.transform))
            {
                text.text=StatusCaption();
                text.resizeTextMaxSize=22;
                text.resizeTextMinSize=10;
                text.resizeTextForBestFit=true;
                text.horizontalOverflow=HorizontalWrapMode.Overflow;
                text.alignment=TextAnchor.MiddleLeft;
                text.verticalOverflow=VerticalWrapMode.Truncate;
            }
        foreach(var ui in UIObjectCache.Active<GameStartUI>()) RestoreNotice(ui);
        foreach(var ui in UIObjectCache.Active<StartWorldUI>()) FixWorldPanel(ui.transform);
        foreach(var ui in UIObjectCache.Active<MultiPlayStartWorldUI>()) FixWorldPanel(ui.transform);
        foreach(var ui in UIObjectCache.Active<FileLoadUI>()) FixLoadPanel(ui);
    }
    private static bool IsStatusCaption(string? value)
    {
        if(string.IsNullOrWhiteSpace(value)) return false;
        value=System.Text.RegularExpressions.Regex.Replace(value,"<[^>]+>","").Replace("\\n"," ");
        // Native TextResize can insert line breaks inside a word (including the final letter).
        return string.Concat(value.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries)) is "StatusEffects" or "状态效果" or "Эффектысостояния";
    }
    private static bool IsConnectCaption(string? value)
    {
        if(string.IsNullOrWhiteSpace(value) || MultiLangManager.Instance is null) return false;
        return string.Concat(value.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries)) is "Подключиться";
    }
    private static string StatusCaption() => MultiLangManager.Instance is null ? "Status Effects" : MultiLangManager.Instance.GetDialog("wordsign_status_effect");
    private static bool IsRoleStatusControl(Transform text)
    {
        var main=text.GetComponentInParent<MainUI>();
        return main?.RoleStateBtn is not null && text.IsChildOf(main.RoleStateBtn);
    }
    private static void RestoreNotice(GameStartUI ui)
    {
        var data=GameSettingData._Instance_k__BackingField;
        if(data is null || !Localization.HasLanguage(Localization.CurrentLanguage) || ui.NoticeText is null || !string.IsNullOrWhiteSpace(ui.NoticeText.text)) return;
        var original=data.GameLanguage;
        try { data.GameLanguage=(UserLanguage)2; ui.RefreshNoticeText(); }
        finally { data.GameLanguage=original; }
    }
    private static void FitSingleLine(TMP_Text text)
    {
        // Retain the authored maximum. TMP recomputes the effective size after language changes.
        if(!text.enableAutoSizing)
        {
            text.fontSizeMax=text.fontSize;
            text.fontSizeMin=Math.Max(18,text.fontSize*0.55f);
            text.enableAutoSizing=true;
        }
        text.enableWordWrapping=false;
        text.overflowMode=TextOverflowModes.Ellipsis;
        var margin=text.margin;
        text.margin=new Vector4(Math.Max(4,margin.x),margin.y,Math.Max(4,margin.z),margin.w);
    }
    private static void FixLoadPanel(FileLoadUI ui)
    {
        var row=ui.transform.Find("Panel/Contents/Right/FileInfoView/Banner1/Content1");
        var title=row?.Find("title/Text")?.GetComponent<TMP_Text>();
        var input=row?.Find("InputField (TMP)")?.TryCast<RectTransform>();
        if(title is not null) FitSingleLine(title);
        var platform=ui.transform.Find("Panel/Contents/Right/FileInfoView/Banner3/LinkPlatform");
        var options=platform?.Find("SelectToggles");
        var caption=platform?.Find("title/Text")?.GetComponent<TMP_Text>();
        if(options is not null && caption is not null)
        {
            // These native choice captions are cached when the load window is created.
            if(Localization.CurrentLanguage>=0 && MultiLangManager.Instance is not null)
            {
                RefreshChoice(options,"NormalToggle/Text","setting_normal_connect_mode_IP");
                RefreshChoice(options,"NobleToggle/Text","setting_normal_connect_mode_remote_IP");
            }
            var optionsRect=options.TryCast<RectTransform>();
            if(optionsRect is null) return;
            var optionBounds=HorizontalBounds(platform!,optionsRect);
            var labelBounds=HorizontalBounds(platform!,caption.rectTransform);
            float available=optionBounds.Left-labelBounds.Left-20;
            if(available>80)
            {
                var fitter=caption.GetComponent<ContentSizeFitter>();
                if(fitter is not null) fitter.enabled=false;
                caption.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,available);
                FitSingleLine(caption);
            }
            foreach(var text in options.GetComponentsInChildren<TMP_Text>(true)) FitSingleLine(text);
        }
    }
    private static void RefreshChoice(Transform options,string path,string key)
    {
        var text=options.Find(path)?.GetComponent<TMP_Text>();
        if(text is null) return;
        string value=MultiLangManager.Instance.GetDialog(key);
        if(text.text!=value) text.text=value;
    }
    private static (float Left,float Right) HorizontalBounds(Transform root,RectTransform child)
    {
        var r=child.rect;
        float left=float.PositiveInfinity,right=float.NegativeInfinity;
        foreach(var point in new[]{new Vector3(r.x,r.y,0),new Vector3(r.x+r.width,r.y,0),new Vector3(r.x,r.y+r.height,0),new Vector3(r.x+r.width,r.y+r.height,0)})
        {
            float x=root.InverseTransformPoint(child.TransformPoint(point)).x;
            left=Math.Min(left,x);right=Math.Max(right,x);
        }
        return(left,right);
    }
    private static void FixWorldPanel(Transform root)
    {
        foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            var parentTransform=text.transform.parent;
            if(parentTransform is null) continue;
            string parent=parentTransform.name;
            if(parent=="WorldName" && text.name=="WorldNameText")
            {
                FitSingleLine(text);
            }
            if(parent.Contains("Toggle",StringComparison.Ordinal) && text.text is "Локальная игра" or "Сетевая игра" or "Local Game" or "Online game") FitSingleLine(text);
        }
    }
}
