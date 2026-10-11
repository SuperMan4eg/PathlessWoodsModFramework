using System.Collections.Concurrent;
using UnityEngine;

namespace PathlessWoodsModFramework;
public static class FrameworkRuntime
{
    public const int ApiVersion=1;
    public const string SupportedSteamBuild="25836294";
    private static readonly ConcurrentQueue<Action> Work=new();
    /// <summary>Queue short work on Unity's main thread. Up to64 callbacks run each frame.</summary>
    public static void Post(Action callback)
    {ArgumentNullException.ThrowIfNull(callback);Work.Enqueue(callback);}
    internal static void Drain()
    {
        for(int i=0;i<64 && Work.TryDequeue(out var callback);i++)
            try{callback();}catch(Exception error){Localization.Log?.LogError($"Mod main-thread callback failed: {error}");}
    }
}
internal sealed class FrameworkDispatcher:MonoBehaviour
{
    public FrameworkDispatcher(IntPtr p):base(p){}
    public void Update()=>FrameworkRuntime.Drain();
}
