using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PathlessWoodsModFramework;
internal sealed class ModsMenuView
{
    private readonly TMP_FontAsset font;
    private readonly GameObject root,window;
    private readonly RectTransform panel;
    private readonly List<GameObject> dynamicRows=new();
    private readonly TMP_Text title,detail,status,pageLabel;
    private readonly TMP_InputField search;
    private IReadOnlyList<InstalledMod> mods=Array.Empty<InstalledMod>();
    private string query="",selectedId="";
    private int page,settingsPage;
    private bool cursorVisible;
    private CursorLockMode cursorLock;
    internal bool IsOpen=>window.activeSelf;
    private static bool Russian=>Localization.CurrentLanguage==5;
    private static string T(string english,string russian)=>Russian?russian:english;
    private static readonly Color Teal=new(.15f,.58f,.51f,.96f);
    internal ModsMenuView(TMP_FontAsset font)
    {
        this.font=font;
        root=new GameObject("PathlessWoodsModsMenu");var canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;root.AddComponent<GraphicRaycaster>();
        var scaler=root.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;
        UnityEngine.Object.DontDestroyOnLoad(root);
        window=Box(root.transform,"Modal",new Vector2(1280,720),Vector2.zero,new Color(0,0,0,.68f)).gameObject;
        var modal=window.GetComponent<RectTransform>();modal.anchorMin=Vector2.zero;modal.anchorMax=Vector2.one;modal.offsetMin=modal.offsetMax=Vector2.zero;
        panel=Box(window.transform,"ModsPanel",new Vector2(1040,620),Vector2.zero,new Color(.055f,.09f,.09f,.99f));
        title=Label(panel,"Title","",new Vector2(860,42),new Vector2(-35,270),30);
        Button(panel,"Close","×",new Vector2(52,42),new Vector2(470,270),Close);
        search=InputField(panel,"Search",new Vector2(320,38),new Vector2(-330,215),"");
        search.placeholder=Label(search.transform,"SearchHint",T("Search mods","Поиск модов"),new Vector2(304,34),Vector2.zero,20).TryCast<Graphic>();
        search.onValueChanged.AddListener((UnityAction<string>)(value=>{query=value;page=0;Render();}));
        detail=Label(panel,"Details","",new Vector2(610,122),new Vector2(180,175),21);detail.enableWordWrapping=true;detail.alignment=TextAlignmentOptions.TopLeft;
        status=Label(panel,"Status","",new Vector2(960,38),new Vector2(0,-265),17);status.enableWordWrapping=true;
        pageLabel=Label(panel,"Page","",new Vector2(160,34),new Vector2(-330,-220),18);
        Button(panel,"Previous","‹",new Vector2(48,34),new Vector2(-450,-220),()=>{if(page>0){page--;Render();}});
        Button(panel,"Next","›",new Vector2(48,34),new Vector2(-210,-220),()=>{if((page+1)*7<Filtered().Length){page++;Render();}});
        window.SetActive(false);
    }
    internal void SetAvailable(bool available)
    {if(!available && IsOpen)Close();}
    internal void Open()
    {
        if(IsOpen)return;
        mods=ModRegistry.GetInstalled();cursorVisible=Cursor.visible;cursorLock=Cursor.lockState;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;
        window.SetActive(true);title.text=T("Mods","Моды");
        status.text=T("Settings are saved when applied. A mod may require a restart.","Настройки сохраняются при применении. Моду может требоваться перезапуск.");
        if(string.IsNullOrEmpty(selectedId))selectedId=mods.FirstOrDefault()?.Description.Id??"";
        Render();
    }
    internal void Close()
    {if(!IsOpen)return;window.SetActive(false);Cursor.visible=cursorVisible;Cursor.lockState=cursorLock;}
    private InstalledMod[] Filtered()=>mods.Where(x=>string.IsNullOrEmpty(query)||x.Description.Name.Contains(query,StringComparison.OrdinalIgnoreCase)||x.Description.Id.Contains(query,StringComparison.OrdinalIgnoreCase)).ToArray();
    private void Render()
    {
        if(!IsOpen)return;
        foreach(var row in dynamicRows){row.SetActive(false);UnityEngine.Object.Destroy(row);}dynamicRows.Clear();
        var filtered=Filtered();page=Math.Clamp(page,0,Math.Max(0,(filtered.Length-1)/7));
        pageLabel.text=$"{page+1} / {Math.Max(1,(filtered.Length+6)/7)}";
        if(filtered.Length==0)dynamicRows.Add(Label(panel,"NoMatches",T("No matching mods.","Совпадений нет."),new Vector2(320,48),new Vector2(-330,160),20).gameObject);
        foreach(var row in filtered.Skip(page*7).Take(7).Select((mod,index)=>(mod,index)))
        {
            var item=row.mod;
            var button=Button(panel,"ModRow",$"{item.Description.Name}\n{item.Description.Version}",new Vector2(320,48),new Vector2(-330,160-row.index*51),()=>{selectedId=item.Description.Id;settingsPage=0;Render();});
            button.GetComponentInChildren<TMP_Text>().fontSizeMax=20;button.GetComponentInChildren<TMP_Text>().enableWordWrapping=true;dynamicRows.Add(button.gameObject);
        }
        var selected=mods.FirstOrDefault(x=>x.Description.Id==selectedId);
        if(selected is null){detail.text=T("No loaded mods found.","Загруженные моды не найдены.");return;}
        var description=selected.Description;
        detail.text=$"{description.Name}  {description.Version}\n{description.Id}\n{(selected.Loaded?T("Loaded","Загружен"):T("Registered, not loaded","Зарегистрирован, не загружен"))}  {description.Author}\n{description.Description}";
        var config=ModRegistry.ConfigFor(description.Id);
        var entries=config?.Select(x=>x.Value).OrderBy(x=>x.Definition.Section,StringComparer.Ordinal).ThenBy(x=>x.Definition.Key,StringComparer.Ordinal).ToArray()??Array.Empty<ConfigEntryBase>();
        settingsPage=Math.Clamp(settingsPage,0,Math.Max(0,(entries.Length-1)/4));
        if(entries.Length==0){var empty=Label(panel,"NoSettings",T("This mod has no configurable settings.","У этого мода нет доступных настроек."),new Vector2(610,48),new Vector2(180,55),20);dynamicRows.Add(empty.gameObject);}
        foreach(var row in entries.Skip(settingsPage*4).Take(4).Select((entry,index)=>(entry,index)))SettingRow(row.entry,row.index);
        if(entries.Length>4)
        {
            dynamicRows.Add(Button(panel,"SettingsPrevious","‹",new Vector2(44,32),new Vector2(50,-220),()=>{if(settingsPage>0){settingsPage--;Render();}}).gameObject);
            dynamicRows.Add(Label(panel,"SettingsPage",$"{settingsPage+1}/{(entries.Length+3)/4}",new Vector2(120,32),new Vector2(180,-220),18).gameObject);
            dynamicRows.Add(Button(panel,"SettingsNext","›",new Vector2(44,32),new Vector2(310,-220),()=>{if((settingsPage+1)*4<entries.Length){settingsPage++;Render();}}).gameObject);
        }
    }
    private void SettingRow(ConfigEntryBase entry,int index)
    {
        float y=70-index*66;
        var presentation=entry.Description.Tags?.OfType<ModSettingOptions>().FirstOrDefault();
        string name=presentation?.DisplayName??$"{entry.Definition.Section} / {entry.Definition.Key}";
        if(presentation?.RequiresRestart==true)name+=T(" (restart)"," (перезапуск)");
        if(entry.SettingType.IsEnum && ModSettings.Supported(entry))name+=" ["+string.Join(" / ",Enum.GetNames(entry.SettingType))+"]";
        var label=Label(panel,"SettingName",name,new Vector2(610,24),new Vector2(180,y+18),17);label.alignment=TextAlignmentOptions.MidlineLeft;dynamicRows.Add(label.gameObject);
        if(entry.SettingType==typeof(bool))
        {
            dynamicRows.Add(Button(panel,"SettingToggle",(bool)entry.BoxedValue?T("On","Вкл."):T("Off","Выкл."),new Vector2(360,32),new Vector2(70,y-12),()=>Apply(entry,(!(bool)entry.BoxedValue).ToString())).gameObject);
        }
        else if(ModSettings.Supported(entry))
        {
            var input=InputField(panel,"SettingInput",new Vector2(360,32),new Vector2(70,y-12),ModSettings.Value(entry));dynamicRows.Add(input.gameObject);
            dynamicRows.Add(Button(panel,"ApplySetting",T("Apply","Применить"),new Vector2(104,32),new Vector2(320,y-12),()=>Apply(entry,input.text)).gameObject);
        }
        else dynamicRows.Add(Label(panel,"ReadOnlySetting",ModSettings.Value(entry),new Vector2(480,32),new Vector2(130,y-12),17).gameObject);
        if(ModSettings.Supported(entry))dynamicRows.Add(Button(panel,"ResetSetting","R",new Vector2(48,32),new Vector2(430,y-12),()=>Apply(entry,Convert.ToString(entry.DefaultValue,System.Globalization.CultureInfo.InvariantCulture)??"")).gameObject);
    }
    private void Apply(ConfigEntryBase entry,string value)
    {status.text=ModSettings.TrySave(entry,value,out var error)?T("Saved.","Сохранено."):T("Could not save: ","Не удалось сохранить: ")+error;Render();}
    private RectTransform Box(Transform parent,string name,Vector2 size,Vector2 position,Color color)
    {
        var go=new GameObject(name);var rect=go.AddComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;go.AddComponent<Image>().color=color;return rect;
    }
    private TMP_Text Label(Transform parent,string name,string value,Vector2 size,Vector2 position,float maximum)
    {
        var go=new GameObject(name);var text=go.AddComponent<TextMeshProUGUI>();text.transform.SetParent(parent,false);text.rectTransform.sizeDelta=size;text.rectTransform.anchoredPosition=position;text.font=font;text.text=value;text.color=new Color(.92f,.97f,.94f,1);text.raycastTarget=false;CaptionLayoutState.Fit(text,maximum);text.alignment=TextAlignmentOptions.Midline;return text;
    }
    private Button Button(Transform parent,string name,string caption,Vector2 size,Vector2 position,Action action)
    {
        var rect=Box(parent,name,size,position,Teal);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();button.onClick.AddListener((UnityAction)(()=>action()));Label(rect,"Caption",caption,size-new Vector2(16,4),Vector2.zero,24);return button;
    }
    private TMP_InputField InputField(Transform parent,string name,Vector2 size,Vector2 position,string value)
    {
        var rect=Box(parent,name,size,position,new Color(.13f,.2f,.2f,1));
        var text=Label(rect,"InputText",value,size-new Vector2(16,4),Vector2.zero,20);text.alignment=TextAlignmentOptions.MidlineLeft;
        var input=rect.gameObject.AddComponent<TMP_InputField>();input.textViewport=text.rectTransform;input.textComponent=text.TryCast<TextMeshProUGUI>();input.characterLimit=2048;input.text=value;return input;
    }
}
