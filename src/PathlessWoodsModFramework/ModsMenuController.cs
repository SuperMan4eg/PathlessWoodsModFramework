using AniYa.UI;
using TMPro;
using UnityEngine;

namespace PathlessWoodsModFramework;
public static class ModMenu
{
    public static bool IsOpen=>ModsMenuController.Instance?.View?.IsOpen==true;
    /// <summary>Opens from a visible main/pause menu; scheduled on the main thread.</summary>
    public static void Open()=>FrameworkRuntime.Post(()=>ModsMenuController.Instance?.Open());
    public static void Close()=>FrameworkRuntime.Post(()=>ModsMenuController.Instance?.View?.Close());
}
internal sealed class ModsMenuController:MonoBehaviour
{
    public ModsMenuController(IntPtr p):base(p){Instance=this;}
    internal static ModsMenuController? Instance;
    internal ModsMenuView? View;
    private float next;
    private bool available;
    private static bool Visible(GameObject? go)=>go is not null && go.activeInHierarchy && go.GetComponentsInParent<CanvasGroup>().All(x=>x.alpha>.05f);
    internal void Open(){if(available)View?.Open();}
    public void Update()
    {
        if(Time.unscaledTime>=next)
        {
            next=Time.unscaledTime+.5f;
            var start=UIObjectCache.Active<GameStartUI>().FirstOrDefault(x=>Visible(x.ButtonsNode));
            var pause=UIObjectCache.Active<GamePauseUI>().FirstOrDefault(x=>Visible(x.BtnEscape));
            available=start is not null || pause is not null;
            if(start is not null)NativeModsLauncher.EnsureMain(start);
            if(pause is not null)NativeModsLauncher.EnsurePause(pause);
            if(View is null && available)
            {
                var font=UIObjectCache.Active<TextMeshProUGUI>().Select(x=>x.font).FirstOrDefault(x=>x!=null);
                if(font is not null)View=new ModsMenuView(font);
            }
            View?.SetAvailable(available);
        }
        if(View is not null && available && Input.GetKeyDown(KeyCode.F6)){if(View.IsOpen)View.Close();else View.Open();}
        if(View?.IsOpen==true && Input.GetKeyDown(KeyCode.Escape))View.Close();
    }
}
