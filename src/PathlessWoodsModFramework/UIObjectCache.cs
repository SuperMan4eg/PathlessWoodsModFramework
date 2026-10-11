using UnityEngine;
namespace PathlessWoodsModFramework;
// Share discoveries between layout components. Hidden prefab trees are not fitted.
internal static class UIObjectCache
{
    private static int sequence;
    private static class Set<T> where T:UnityEngine.Object
    {internal static T[] Items=Array.Empty<T>();internal static float Next;internal static readonly float Offset=(sequence++%12)*.17f;}
    internal static T[] Get<T>() where T:UnityEngine.Object
    {
        if(Time.unscaledTime>=Set<T>.Next)
        {Set<T>.Items=Resources.FindObjectsOfTypeAll<T>().ToArray();Set<T>.Next=Time.unscaledTime+4f+Set<T>.Offset;}
        return Set<T>.Items;
    }
    internal static IEnumerable<T> Active<T>() where T:Component
    {foreach(var item in Get<T>())if(item!=null && item.gameObject.activeInHierarchy)yield return item;}
}
