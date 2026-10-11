using AniYa;
using AniYa.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PathlessWoodsModFramework;
internal static class NativeModsLauncher
{
    private static readonly Dictionary<int,GameObject> Buttons=new();
    private static readonly Dictionary<int,Dictionary<int,Vector2>> PausePositions=new();
    private static readonly Dictionary<int,bool> CaptionLabels=new();
    internal static void Install(Harmony harmony)
    {
        var captionPrefix=new HarmonyMethod(typeof(NativeModsLauncher),nameof(OwnCaption)){priority=Priority.Last};
        harmony.Patch(AccessTools.PropertySetter(typeof(TMP_Text),"text"),prefix:captionPrefix);
        harmony.Patch(AccessTools.PropertySetter(typeof(Text),"text"),prefix:captionPrefix);
        harmony.Patch(AccessTools.Method(typeof(GameStartUI),nameof(GameStartUI.ShowMenuButtons)),postfix:new HarmonyMethod(typeof(NativeModsLauncher),nameof(MainShown)));
        harmony.Patch(AccessTools.Method(typeof(GamePauseUI),nameof(GamePauseUI.OnEnable)),postfix:new HarmonyMethod(typeof(NativeModsLauncher),nameof(PauseShown)));
    }
    private static void MainShown(GameStartUI __instance)=>EnsureMain(__instance);
    private static void PauseShown(GamePauseUI __instance)=>EnsurePause(__instance);
    internal static void EnsureMain(GameStartUI menu)
    {
        if(menu.ButtonsNode==null)return;
        var buttons=menu.ButtonsNode.GetComponentsInChildren<Button>(true);
        var quit=buttons.FirstOrDefault(b=>b.name=="Btn_Quit");
        var template=buttons.FirstOrDefault(b=>b.name=="Btn_UserManual")??buttons.FirstOrDefault(b=>b.name=="Btn_Settings");
        if(template==null || quit==null)return;
        var clone=Ensure(menu.GetInstanceID(),template.gameObject,"ModsLauncher",false);
        int quitIndex=quit.transform.GetSiblingIndex();
        clone.transform.SetSiblingIndex(quitIndex-(clone.transform.GetSiblingIndex()<quitIndex?1:0));
        SetCaption(clone,false);
    }
    internal static void EnsurePause(GamePauseUI menu)
    {
        if(menu.BtnEscape==null)return;
        var settings=menu.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name.Contains("Setting",StringComparison.OrdinalIgnoreCase));
        var template=settings?.gameObject??menu.BtnEscape;
        var clone=Ensure(menu.GetInstanceID(),template,"ModsLauncherPause",true);
        SetCaption(clone,true);
        var parent=template.transform.parent;
        clone.transform.SetSiblingIndex(template.transform.GetSiblingIndex()+1);
        if(parent.GetComponent<VerticalLayoutGroup>()!=null)return;
        // Some pause variants use fixed positions instead of a layout group.
        // Keep the same vertical range and distribute one extra row within it.
        var rows=parent.GetComponentsInChildren<Button>(true).Where(b=>b.transform.parent==parent && b.gameObject.activeSelf && b.gameObject!=clone)
            .Select(b=>b.GetComponent<RectTransform>()).Where(t=>t!=null).ToArray();
        if(rows.Length<2)return;
        if(!PausePositions.TryGetValue(menu.GetInstanceID(),out var originals))
            PausePositions[menu.GetInstanceID()]=originals=rows.ToDictionary(t=>t.GetInstanceID(),t=>t.anchoredPosition);
        var templateRect=template.GetComponent<RectTransform>();
        var column=rows.Where(t=>originals.ContainsKey(t.GetInstanceID()) && Math.Abs(originals[t.GetInstanceID()].x-templateRect.anchoredPosition.x)<templateRect.rect.width*.5f)
            .OrderByDescending(t=>originals[t.GetInstanceID()].y).ToList();
        if(column.Count<2)return;
        float top=originals[column[0].GetInstanceID()].y,bottom=originals[column[^1].GetInstanceID()].y;
        int index=column.FindIndex(t=>t.gameObject==template);
        if(index<0)return;
        column.Insert(index+1,clone.GetComponent<RectTransform>());
        for(int i=0;i<column.Count;i++)column[i].anchoredPosition=new Vector2(templateRect.anchoredPosition.x,Mathf.Lerp(top,bottom,(float)i/(column.Count-1)));
    }
    private static GameObject Ensure(int id,GameObject template,string name,bool pause)
    {
        if(Buttons.TryGetValue(id,out var existing) && existing!=null)return existing;
        var clone=UnityEngine.Object.Instantiate(template,template.transform.parent);
        clone.name=name;
        foreach(var localizer in clone.GetComponentsInChildren<MultiLangTMP>(true)){localizer.enabled=false;UnityEngine.Object.Destroy(localizer);}
        foreach(var localizer in clone.GetComponentsInChildren<MultiLanguage>(true)){localizer.enabled=false;UnityEngine.Object.Destroy(localizer);}
        // A new event also removes serialized callbacks; RemoveAllListeners alone does not.
        foreach(var button in clone.GetComponentsInChildren<Button>(true))button.onClick=new Button.ButtonClickedEvent();
        var target=clone.GetComponent<Button>()??clone.GetComponentInChildren<Button>(true);
        target.onClick.AddListener((UnityAction)(()=>ModMenu.Open()));
        clone.SetActive(true);Buttons[id]=clone;
        return clone;
    }
    private static void SetCaption(GameObject clone,bool pause)
    {
        var label=clone.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name=="Text")??clone.GetComponentInChildren<TMP_Text>(true);
        var legacy=clone.GetComponentsInChildren<Text>(true).FirstOrDefault(t=>t.name=="Text")??clone.GetComponentInChildren<Text>(true);
        if(label!=null){CaptionLabels[label.GetInstanceID()]=pause;label.text=Caption(pause);}
        if(legacy!=null){CaptionLabels[legacy.GetInstanceID()]=pause;legacy.text=Caption(pause);}
    }
    private static string Caption(bool pause)=>Localization.CurrentLanguage==5?(pause?"Моды [F6]":"Моды"):(pause?"Mods [F6]":"Mods");
    // Pause variants use both TMP and legacy Text. Only labels owned by this
    // launcher replace inherited localization assignments at the setter boundary.
    private static void OwnCaption(UnityEngine.Object __instance,ref string __0)
    {if(CaptionLabels.TryGetValue(__instance.GetInstanceID(),out var pause))__0=Caption(pause);}
}
