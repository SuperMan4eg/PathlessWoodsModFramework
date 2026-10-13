using AniYa;
using AniYa.UI;
using AniYa.UI.Craft;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

namespace PathlessWoodsModFramework;
// Exact reported captions and known UI components; no blanket shrinking of game text.
internal sealed class ReportedControlLayout:MonoBehaviour
{
    public ReportedControlLayout(IntPtr p):base(p){}
    private float next;
    private static readonly Dictionary<int,(string Text,float Width,float Height,float Max,int Language)> Layouts=new();
    private static readonly Dictionary<int,(string Text,float Width,int Language)> LegacyLayouts=new();
    private static readonly Dictionary<int,(float Next,int State)> HelpPolls=new();
    private static readonly Dictionary<int,(TMP_Text Text,string English,string Russian)> SystemTexts=new();
    private static readonly Dictionary<int,(Text Text,string English,string Russian)> SystemLegacy=new();
    private static readonly Dictionary<string,string> SystemCaptions=new(StringComparer.Ordinal)
    {{"System","Система"},{"Setting","Настройки"},{"Escape","Выбраться"},{"Save & Quit","Сохранить и выйти"},{"Quit","Выйти"}};
    private static bool Changed(TMP_Text text)=>!Layouts.TryGetValue(text.GetInstanceID(),out var state)||state!=(text.text,text.rectTransform.rect.width,text.rectTransform.rect.height,text.fontSizeMax,Localization.CurrentLanguage);
    private static void RememberLayout(TMP_Text text)=>Layouts[text.GetInstanceID()]=(text.text,text.rectTransform.rect.width,text.rectTransform.rect.height,text.fontSizeMax,Localization.CurrentLanguage);
    private static bool ChangedLegacy(Text text)=>!LegacyLayouts.TryGetValue(text.GetInstanceID(),out var state)||state!=(text.text,text.rectTransform.rect.width,Localization.CurrentLanguage);
    private static void RememberLegacy(Text text)=>LegacyLayouts[text.GetInstanceID()]=(text.text,text.rectTransform.rect.width,Localization.CurrentLanguage);
    private static void ApplySystemCaption(TMP_Text text)
    {
        if(Localization.HasLanguage(Localization.CurrentLanguage) && text.text.StartsWith("有任何问题欢迎联系我们",StringComparison.Ordinal))
        {
            var translated=Localization.ResolveInline(text.text,text.text);
            if(translated!=text.text){SystemTexts[text.GetInstanceID()]=(text,text.text,translated);text.text=translated;}
            return;
        }
        if(Localization.CurrentLanguage!=5 || text.GetComponentInParent<GamePauseUI>() is null || !SystemCaptions.TryGetValue(text.text,out var value))return;
        SystemTexts[text.GetInstanceID()]=(text,text.text,value);
        text.text=value;CaptionLayoutState.Fit(text,30,true);
    }
    private static void ApplySystemLegacy(Text text)
    {
        if(Localization.HasLanguage(Localization.CurrentLanguage) && text.text.StartsWith("有任何问题欢迎联系我们",StringComparison.Ordinal))
        {
            var translated=Localization.ResolveInline(text.text,text.text);
            if(translated!=text.text){SystemLegacy[text.GetInstanceID()]=(text,text.text,translated);text.text=translated;}
            return;
        }
        if(Localization.CurrentLanguage!=5 || text.GetComponentInParent<GamePauseUI>() is null || !SystemCaptions.TryGetValue(text.text,out var value))return;
        SystemLegacy[text.GetInstanceID()]=(text,text.text,value);
        text.text=value;text.resizeTextForBestFit=true;text.resizeTextMaxSize=30;text.resizeTextMinSize=18;text.horizontalOverflow=HorizontalWrapMode.Wrap;
    }
    private static bool IsCloudChoice(TMP_Text text) =>
        text.GetComponentInParent<ConfirmUI>(true) is not null &&
        CaptionLayoutState.Canonical(text.text) is "Облачная копия" or "Локальная копия" or "Проверить облако" or "Позже";
    private static bool FitCloudChoice(TMP_Text text)
    {
        if (!IsCloudChoice(text)) return false;
        var button = text.GetComponentInParent<Button>(true);
        if (button is null || text.transform.parent != button.transform) return false;
        // The native horizontal group uses the shrinking label's preferred width and can collapse it to zero.
        // Keep this label out of that group and stretch it inside the already laid-out button instead.
        var layout = text.GetComponent<LayoutElement>() ?? text.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        var rect = text.rectTransform;
        rect.localScale = Vector3.one;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(button.name == "ButtonCancel" ? 84 : 12, 4);
        rect.offsetMax = new Vector2(-12, -4);
        return true;
    }
    internal static void ExpandButtonCaption(TMP_Text text)
    {
        if(FitCloudChoice(text))return;
        var button=text.GetComponentInParent<Button>(true);if(button is null)return;
        var control=button.transform.TryCast<RectTransform>();var parent=text.transform.parent;
        if(control is null || parent is null || control.rect.width<60)return;
        // Use the actual button's available area, not a tiny intermediate text holder.
        float left=parent.InverseTransformPoint(control.TransformPoint(new Vector3(control.rect.xMin+12,0))).x;
        float right=parent.InverseTransformPoint(control.TransformPoint(new Vector3(control.rect.xMax-12,0))).x;
        var rect=text.rectTransform;rect.localScale=Vector3.one;rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,right-left);
        var pos=rect.localPosition;pos.x=left+(right-left)*rect.pivot.x;rect.localPosition=pos;
        float height=Math.Abs(parent.InverseTransformVector(control.TransformVector(new Vector3(0,control.rect.height-8))).y);
        if(height>20)rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
    }
    internal static void FitStatusEffect(TMP_Text title,Transform item)
    {
        var root=item.TryCast<RectTransform>();if(root is null)return;
        float width=Math.Clamp(root.rect.width,56,76)-4;
        title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);
        float oldHeight=title.rectTransform.rect.height;
        var position=title.rectTransform.anchoredPosition;
        title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,52);
        position.y+=(oldHeight-52)*(1-title.rectTransform.pivot.y);title.rectTransform.anchoredPosition=position;
        string caption=CaptionLayoutState.Canonical(title.text.Replace("-\n",""));
        title.text=caption switch {"Обезвоживание"=>"Обезво-\nживание","Недоедание"=>"Недо-\nедание",_=>string.Join("\n",caption.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries))};
        CaptionLayoutState.Fit(title,16,title.text.Contains('\n'));title.fontSizeMin=10;
        title.alignment=TextAlignmentOptions.Top;RememberLayout(title);
    }
    internal static void ApplyEvent(TMP_Text text)
    {
        CraftingCaptionLayout.Apply(text);
        ApplySystemCaption(text);
        if(text.GetComponentInParent<OtherBuffIcon>() is { } hud && hud.buffNameText==text)FitStatusEffect(text,hud.transform);
        else if(text.GetComponentInParent<BuffPreviewItem>() is { } preview && preview.buffNameText==text)FitStatusEffect(text,preview.transform);
        else if(text.GetComponentInParent<BuffItem>() is { } buff && buff.text==text)FitStatusEffect(text,buff.transform);
        else if(text.GetComponentInParent<PropertyItem>() is { } property)FitProperty(property);
        else if(text.GetComponentInParent<EquipmentBuffItem>() is { } equipment && equipment.buffNameText==text)
        {text.text=CaptionLayoutState.Canonical(text.text);CaptionLayoutState.Fit(text,18,false);text.fontSizeMin=12;}
        else Apply(text);
        if(IsSettingsHeading(text) || IsSettingsTab(text) || text.GetComponentInParent<SettingUI>() is not null || text.GetComponentInParent<TMP_Dropdown>() is not null)FitSettings(text);
        RememberLayout(text);
    }
    private static bool IsSettingsHeading(TMP_Text text)=>CaptionLayoutState.Canonical(text.text) is "Настройки" or "Настройки игры";
    private static bool IsSettingsTab(TMP_Text text)=>CaptionLayoutState.Canonical(text.text) is "Общие" or "Графика" or "Управление" or "Звук" && (text.GetComponentInParent<Button>() is not null || text.GetComponentInParent<Toggle>() is not null || text.GetComponentInParent<SettingUI>() is not null);
    internal static bool IsTargeted(TMP_Text text)=>IsCloudChoice(text) || text.GetComponentInParent<OtherBuffIcon>() is not null || text.GetComponentInParent<BuffPreviewItem>() is not null || text.GetComponentInParent<BuffItem>() is not null || text.GetComponentInParent<SettingUI>() is not null || text.GetComponentInParent<TMP_Dropdown>() is not null || IsSettingsHeading(text) || IsSettingsTab(text);
    internal static void FitSettings(TMP_Text text)
    {
        text.text=CaptionLayoutState.Canonical(text.text);
        if(IsSettingsHeading(text))
        {
            CaptionLayoutState.Fit(text,36);text.fontSizeMin=24;text.alignment=TextAlignmentOptions.Midline;
            var rect=text.rectTransform;rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,60);
            var canvas=text.GetComponentInParent<Canvas>()?.transform.TryCast<RectTransform>();
            if(canvas is not null)
            {
                float top=canvas.InverseTransformPoint(rect.TransformPoint(new Vector3(0,rect.rect.yMax))).y;
                float overflow=top-(canvas.rect.yMax-8);
                if(overflow>0)rect.position-=canvas.TransformVector(new Vector3(0,overflow));
            }
        }
        else if(IsSettingsTab(text))
        {
            var control=text.GetComponentInParent<Button>()?.transform.TryCast<RectTransform>() ?? text.GetComponentInParent<Toggle>()?.transform.TryCast<RectTransform>() ?? text.transform.parent?.TryCast<RectTransform>();
            var parent=text.transform.parent;
            if(control is not null && parent is not null && control.rect.width>=80)
            {
                float left=parent.InverseTransformPoint(control.TransformPoint(new Vector3(control.rect.xMin+44,0))).x;
                float right=parent.InverseTransformPoint(control.TransformPoint(new Vector3(control.rect.xMax-12,0))).x;
                var rect=text.rectTransform;rect.localScale=Vector3.one;rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,right-left);
                var position=rect.localPosition;position.x=left+(right-left)*rect.pivot.x;rect.localPosition=position;
            }
            CaptionLayoutState.Fit(text,30);text.fontSizeMin=14;text.alignment=TextAlignmentOptions.Midline;
        }
        else if(text.GetComponentInParent<TMP_Dropdown>() is { } dropdown && dropdown.captionText==text)
        {
            var control=dropdown.transform.TryCast<RectTransform>();var parent=text.transform.parent;
            if(control is not null && parent is not null && control.rect.width>=80)
            {
                float left=parent.InverseTransformPoint(control.TransformPoint(new Vector3(control.rect.xMin+12,0))).x;
                float right=parent.InverseTransformPoint(control.TransformPoint(new Vector3(control.rect.xMax-40,0))).x;
                var rect=text.rectTransform;rect.localScale=Vector3.one;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,right-left);
                var pos=rect.localPosition;pos.x=left+(right-left)*rect.pivot.x;rect.localPosition=pos;
                float height=Math.Abs(parent.InverseTransformVector(control.TransformVector(new Vector3(0,control.rect.height-8))).y);
                if(height>=20)rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
            }
            CaptionLayoutState.Fit(text,30);text.fontSizeMin=10;text.alignment=TextAlignmentOptions.MidlineLeft;
        }
        else if(text.GetComponentInParent<Button>() is not null || text.GetComponentInParent<Toggle>() is not null)
        {BoundToControl(text);CaptionLayoutState.Fit(text,30);}
    }
    internal static void PollHelp(Transform help)
    {
        if(!help.gameObject.activeInHierarchy)return;
        int id=help.GetInstanceID();HelpPolls.TryGetValue(id,out var saved);
        if(Time.unscaledTime<saved.Next)return;
        var root=help.TryCast<RectTransform>();var canvas=help.GetComponentInParent<Canvas>();
        if(root is null || canvas is null)return;
        int state=HashCode.Combine(root.position,root.rect.size,canvas.transform.lossyScale,Screen.width,Screen.height);
        foreach(var text in help.GetComponentsInChildren<TMP_Text>(false))state=HashCode.Combine(state,text.text,text.rectTransform.rect.size);
        HelpPolls[id]=(Time.unscaledTime+.5f,state);
        if(saved.State==state)return;
        KeepHelpInside(help);
    }
    private static readonly Dictionary<int,(Image Image,Sprite Sprite,Color Color)> CommunityImages=new();
    private static Image[] CommunityButtons=Array.Empty<Image>();
    private static float nextCommunity;
    private static readonly Dictionary<int,(Text Text,string Value,bool Active,Color Background)> CommunityLabels=new();
    private static void CommunityCaption()
    {
        bool russian=Localization.HasLanguage(Localization.CurrentLanguage);
        if(Time.unscaledTime>=nextCommunity){CommunityButtons=UIObjectCache.Get<Image>().Where(x=>x!=null && x.name=="Button_Bilibili" && x.transform.parent?.name=="C_Media").ToArray();nextCommunity=Time.unscaledTime+4f;}
        foreach(var image in CommunityButtons)
        {
            if(image==null)continue;
            int id=image.GetInstanceID();
            if(!CommunityImages.TryGetValue(id,out var original))
            {
                original=(image,image.sprite,image.color);CommunityImages[id]=original;
            }
            var background=image.transform.Find("bg")?.GetComponent<Image>();
            var label=image.transform.Find("Text")?.GetComponent<Text>();
            if(label is not null && background is not null && !CommunityLabels.ContainsKey(id))CommunityLabels[id]=(label,label.text,label.gameObject.activeSelf,background.color);
            if(!russian)
            {
                image.sprite=original.Sprite;image.color=original.Color;
                if(CommunityLabels.TryGetValue(id,out var native)){native.Text.text=native.Value;native.Text.gameObject.SetActive(native.Active);if(background is not null)background.color=native.Background;}
                continue;
            }
            // The original sprite contains Chinese lettering. Reuse this button's
            // existing background while retaining its target graphic and click handler.
            var blue=new Color(0.02f,0.63f,0.82f,1);
            if(background is not null){image.sprite=background.sprite;image.color=blue;background.color=blue;}
            if(label is not null)
            {
                label.gameObject.SetActive(true);label.text="Bilibili";label.color=Color.white;
                label.resizeTextForBestFit=true;label.resizeTextMaxSize=32;label.resizeTextMinSize=18;
                label.horizontalOverflow=HorizontalWrapMode.Overflow;label.alignment=TextAnchor.MiddleCenter;
            }
        }
    }
    private static readonly Dictionary<string,(string Value,bool Wrap)> Captions=new(StringComparer.Ordinal);
    static ReportedControlLayout()
    {
        foreach(string s in new[]{"Облачная копия","Локальная копия","Проверить облако","Позже","Конфликт сохранений","Сохранения","Ёмкость","Истощение","Сильный дух","Без души","Без разума","[Лёгкая]","[Обычная]","[Сложная]","Дерево изготовления","Выносливость","Телосложение","Эффект","Характеристики","Эффекты состояния"})Captions[Compact(s)]=(s,false);
        foreach(string s in new[]{"Умеренные равнины","Изделия из меди"})Captions[Compact(s)]=(s,true);
    }
    private static string Compact(string s)=>Regex.Replace(Regex.Replace(s,"<[^>]*>",""),@"\s+","");
    internal static bool Apply(TMP_Text text)
    {
        if(!Localization.HasLanguage(Localization.CurrentLanguage) || string.IsNullOrWhiteSpace(text.text))return false;
        string current=text.text;
        if(current.StartsWith("QQ群",StringComparison.Ordinal) || current=="关注B站" || current.StartsWith("有任何问题欢迎联系我们",StringComparison.Ordinal))
        {
            string value=Localization.ResolveInline(current,current);if(value!=current)text.text=value;
        }
        string canonical=CaptionLayoutState.Canonical(current);
        bool groupBanner=canonical.StartsWith("Изделия из ",StringComparison.Ordinal)||canonical.StartsWith("Гончарное дело",StringComparison.Ordinal)||canonical is "Взрывчатка" or "Основы гончарного дела";
        if(!Captions.TryGetValue(Compact(current),out var caption))
        {if(!groupBanner)return false;caption=(canonical,true);}
        // Native Refresh may have split a word after SetText's postfix completed.
        text.text=caption.Value;
        var fitter=text.GetComponent<ContentSizeFitter>();if(fitter is not null)fitter.enabled=false;
        if(caption.Value is "Облачная копия" or "Локальная копия" or "Проверить облако" or "Позже")ExpandButtonCaption(text);
        else BoundToControl(text);
        text.fontSizeMax=groupBanner?26:caption.Value=="Эффект"?20:30;
        text.enableAutoSizing=true;text.fontSizeMin=caption.Wrap?12:14;
        text.enableWordWrapping=caption.Wrap;text.overflowMode=TextOverflowModes.Ellipsis;
        text.margin=new Vector4(4,2,4,2);
        text.alignment=TextAlignmentOptions.Midline;
        return true;
    }
    internal static bool ApplyLegacy(Text text)
    {
        if(CraftingCaptionLayout.Apply(text))return true;
        if(!Localization.HasLanguage(Localization.CurrentLanguage) || string.IsNullOrWhiteSpace(text.text))return false;
        string current=text.text;
        if(current.StartsWith("QQ群",StringComparison.Ordinal) || current=="关注B站" || current.StartsWith("有任何问题欢迎联系我们",StringComparison.Ordinal))text.text=Localization.ResolveInline(current,current);
        if(!Captions.TryGetValue(Compact(current),out var caption))return false;
        text.text=caption.Value;text.resizeTextMaxSize=Math.Max(14,text.fontSize);text.resizeTextMinSize=10;text.resizeTextForBestFit=true;
        text.horizontalOverflow=caption.Wrap?HorizontalWrapMode.Wrap:HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Truncate;
        return true;
    }
    private static (float Left,float Right) Bounds(Transform parent,RectTransform child)
    {
        var rect=child.rect;float left=float.PositiveInfinity,right=float.NegativeInfinity;
        foreach(var p in new[]{new Vector3(rect.xMin,rect.yMin),new Vector3(rect.xMax,rect.yMax)})
        {float x=parent.InverseTransformPoint(child.TransformPoint(p)).x;left=Math.Min(left,x);right=Math.Max(right,x);}
        return(left,right);
    }
    private static void LimitWidth(TMP_Text text,RectTransform bounds)
    {
        var rect=text.rectTransform;if(rect.rect.width<=0 || bounds.rect.width<=0)return;
        var b=Bounds(bounds,rect);float available=bounds.rect.xMax-b.Left-6;
        if(available<40 || available>=rect.rect.width)return;
        float old=rect.rect.width;var pos=rect.anchoredPosition;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,available);
        pos.x+=(available-old)*rect.pivot.x;rect.anchoredPosition=pos;
    }
    private static void BoundToControl(TMP_Text text)
    {
        var direct=text.transform.parent?.TryCast<RectTransform>();
        if(direct is not null)LimitWidth(text,direct);
        var t=text.transform.parent;
        for(int i=0;t is not null && i<6;i++,t=t.parent)
        {
            if(t.GetComponent<Button>() is not null || t.GetComponent<Toggle>() is not null)
            {var r=t.TryCast<RectTransform>();if(r is not null)LimitWidth(text,r);break;}
        }
    }
    internal static void FitTooltip(TechLinkTooltipUI ui)
    {
        if(!Localization.HasLanguage(Localization.CurrentLanguage))return;
        float width=ui._maxWidth;
        if(width<120 || width>1600)return;
        float height=16;
        foreach(var text in new[]{ui._text,ui._hintText})
        {
            if(text is null || string.IsNullOrEmpty(text.text))continue;
            text.enableWordWrapping=true;text.enableAutoSizing=false;text.overflowMode=TextOverflowModes.Overflow;
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width-24);
            float h=text.GetPreferredValues(text.text,width-24,float.PositiveInfinity).y+8;
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,h);height+=h;
        }
        if(ui._root is not null)ui._root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
    }
    public void LateUpdate()
    {
        if(Time.unscaledTime<next)return;
        next=Time.unscaledTime+0.25f;
        CommunityCaption();
        if(Localization.CurrentLanguage!=5)
        {
            foreach(var saved in SystemTexts.Values)if(saved.Text!=null && saved.Text.text==saved.Russian)saved.Text.text=saved.English;
            foreach(var saved in SystemLegacy.Values)if(saved.Text!=null && saved.Text.text==saved.Russian)saved.Text.text=saved.English;
        }
        if(!Localization.HasLanguage(Localization.CurrentLanguage))return;
        foreach(var text in UIObjectCache.Active<TMP_Text>())if(Changed(text))ApplyEvent(text);
        foreach(var text in UIObjectCache.Active<Text>())if(ChangedLegacy(text)){ApplySystemLegacy(text);ApplyLegacy(text);RememberLegacy(text);}
        // Native SetText/Refresh hooks restore caption state; avoid reformatting all widgets periodically.
        foreach(var property in UIObjectCache.Active<PropertyItem>())if(property._label is { } label && property._value is { } value && (Changed(label)||Changed(value))){FitProperty(property);RememberLayout(label);RememberLayout(value);}
        foreach(var effect in UIObjectCache.Active<EquipmentBuffItem>())
        {if(effect.buffNameText is { } name && Changed(name)){name.text=CaptionLayoutState.Canonical(name.text);CaptionLayoutState.Fit(name,22);RememberLayout(name);}}
        foreach(var buff in UIObjectCache.Active<BuffItem>())if(buff.text is { } title)FitStatusEffect(title,buff.transform);
        foreach(var buff in UIObjectCache.Active<BuffPreviewItem>())if(buff.buffNameText is { } title)FitStatusEffect(title,buff.transform);
        foreach(var buff in UIObjectCache.Active<OtherBuffIcon>())if(buff.buffNameText is { } title)FitStatusEffect(title,buff.transform);
        foreach(var tips in UIObjectCache.Active<OperationTipsUI>())PollHelp(tips.transform);
        foreach(var tips in UIObjectCache.Active<UsingPropOperationHelpUI>())PollHelp(tips.transform);
        foreach(var main in UIObjectCache.Active<MainUI>())
            if(main.foodchiDetailUI is { } panel)
                foreach(var text in panel.GetComponentsInChildren<TMP_Text>(false))if(Changed(text)){CaptionLayoutState.Fit(text,18,true);RememberLayout(text);}
        foreach(var help in UIObjectCache.Active<KeyboardMouseHelpContent>())
        {
            if(help._rows is null)continue;
            foreach(var row in help._rows)
            {
                var text=row?.Description;if(text is null || !Changed(text))continue;
                var rect=row?.Root?.GetComponent<RectTransform>();if(rect is not null)LimitWidth(text,rect);
                var parent=text.transform.parent;
                for(int i=0;parent is not null && i<8;i++,parent=parent.parent)
                {
                    if(parent.GetComponent<RectMask2D>() is not null)
                    {var mask=parent.TryCast<RectTransform>();if(mask is not null)LimitWidth(text,mask);break;}
                }
                if(!text.enableAutoSizing)text.fontSizeMax=Math.Max(14,text.fontSize);
                text.enableAutoSizing=true;text.fontSizeMin=10;text.enableWordWrapping=true;text.overflowMode=TextOverflowModes.Ellipsis;
                text.margin=new Vector4(2,1,4,1);RememberLayout(text);
            }
        }
    }
    internal static void FitProperty(PropertyItem item)
    {
        if(item._label is not { } label || item._value is not { } value)return;
        var root=item.transform.TryCast<RectTransform>();if(root is null || root.rect.width<50)return;
        var parent=root.parent?.TryCast<RectTransform>();
        if(parent is not null && parent.rect.width>root.rect.width && root.rect.width<140)
        {
            float width=Math.Min(170,parent.rect.width);root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);
            var layout=root.GetComponent<LayoutElement>();if(layout is not null)layout.preferredWidth=width;
        }
        // Give the number its own area rather than drawing it over a translated label.
        float valueWidth=Math.Max(32,value.GetPreferredValues(value.text).x+8);
        float labelWidth=Math.Max(30,root.rect.width-valueWidth-8);
        Place(label.rectTransform,0,labelWidth);Place(value.rectTransform,labelWidth+8,valueWidth);
        if(item._labelBg is { } bg)Place(bg.rectTransform,0,labelWidth);
        label.text=CaptionLayoutState.Canonical(label.text);CaptionLayoutState.Fit(label,22);CaptionLayoutState.Fit(value,24);
    }
    private static void Place(RectTransform rect,float left,float width)
    {
        rect.anchorMin=new Vector2(0,rect.anchorMin.y);rect.anchorMax=new Vector2(0,rect.anchorMax.y);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);
        var pos=rect.anchoredPosition;pos.x=left+width*rect.pivot.x;rect.anchoredPosition=pos;
    }
    internal static void KeepHelpInside(Transform help)
    {
        var root=help.TryCast<RectTransform>();var canvas=help.GetComponentInParent<Canvas>();
        var canvasRect=canvas?.transform.TryCast<RectTransform>();if(root is null || canvasRect is null)return;
        float right=Bounds(canvasRect,root).Right;
        foreach(var text in help.GetComponentsInChildren<TMP_Text>(true))
        {
            var bounds=Bounds(canvasRect,text.rectTransform);right=Math.Max(right,bounds.Left+Math.Max(text.rectTransform.rect.width,text.GetPreferredValues(text.text).x));
        }
        // Translate the entire block once by the overflow; repeated calls are idempotent.
        float overflow=right-(canvasRect.rect.xMax-28);
        if(overflow<=0)return;
        var shift=canvasRect.TransformVector(new Vector3(-overflow,0,0));root.position+=shift;
    }
}
