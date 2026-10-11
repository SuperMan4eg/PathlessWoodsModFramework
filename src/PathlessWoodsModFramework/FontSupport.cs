using TMPro;
using UnityEngine;

namespace PathlessWoodsModFramework;

public static class FontSupport
{
    internal static bool Enabled;
    /// <summary>Preloads Russian glyphs from native font sources and attaches a native fallback where safe.</summary>
    public static void EnableCyrillicFallback() => Enabled = true;
}

internal sealed class FontMonitor : MonoBehaviour
{
    public FontMonitor(IntPtr pointer) : base(pointer) { }
    private float nextCheck;
    private TMP_FontAsset? fallback;
    private readonly HashSet<IntPtr> handled = new();
    private const string Glyphs = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя…—–«»";
    private static bool Reaches(TMP_FontAsset root, IntPtr target, HashSet<IntPtr> visited)
    {
        if (root.Pointer == target) return true;
        if (!visited.Add(root.Pointer)) return false;
        if (root.fallbackFontAssetTable is not null)
            foreach (var child in root.fallbackFontAssetTable)
                if (child is not null && Reaches(child, target, visited)) return true;
        return false;
    }
    public void Update()
    {
        if (!FontSupport.Enabled || Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 1;
        try
        {
            var assets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (fallback is null)
            {
                foreach (var asset in assets)
                {
                    if (!asset.TryAddCharacters(Glyphs, out var missing)) continue;
                    fallback = asset;
                    handled.Add(asset.Pointer);
                    Localization.Log?.LogInfo($"Native Cyrillic glyph coverage PASS: {asset.name}; all {Glyphs.Length} requested characters");
                    break;
                }
            }
            if (fallback is null) return;
            foreach (var asset in assets)
            {
                if (handled.Contains(asset.Pointer)) continue;
                if (!asset.TryAddCharacters(Glyphs, out var missing))
                {
                    if (Reaches(fallback, asset.Pointer, new HashSet<IntPtr>()))
                        Localization.Log?.LogInfo($"Kept existing descendant font to avoid fallback cycle: {asset.name}");
                    else
                    {
                        asset.fallbackFontAssetTable ??= new Il2CppSystem.Collections.Generic.List<TMP_FontAsset>();
                        if (!asset.fallbackFontAssetTable.Contains(fallback)) asset.fallbackFontAssetTable.Add(fallback);
                        Localization.Log?.LogInfo($"Native Cyrillic fallback attached: {asset.name} -> {fallback.name}");
                    }
                }
                handled.Add(asset.Pointer);
            }
        }
        catch (Exception e) { Localization.Log?.LogError($"Cyrillic preparation failed: {e}"); FontSupport.Enabled = false; }
    }
}
