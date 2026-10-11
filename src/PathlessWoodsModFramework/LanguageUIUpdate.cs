using AniYa.UI;
using UnityEngine;

namespace PathlessWoodsModFramework;

internal sealed class LanguageUIUpdate : MonoBehaviour
{
    public LanguageUIUpdate(IntPtr p):base(p){}
    private float next;
    public void Update()
    {
        if(Time.unscaledTime<next || Localization.Packs.Count==0) return;
        next=Time.unscaledTime+0.25f;
        foreach(var ui in Resources.FindObjectsOfTypeAll<SettingUI>()) LanguageHooks.SettingsOptions(ui);
    }
}
