using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;

namespace PathlessWoodsModFramework;

/// <summary>Optional player-facing metadata supplied by an independent plugin.</summary>
public sealed record ModDescription(string Id,string Name,string Version,string Description="",string Author="");
/// <summary>An immutable snapshot; Loaded describes loader discovery, not gameplay correctness.</summary>
public sealed record InstalledMod(ModDescription Description,bool Loaded,int SettingCount);

public static class ModRegistry
{
    private static readonly Dictionary<string,ModDescription> Registered=new(StringComparer.Ordinal);
    private static readonly object Gate=new();
    /// <summary>Register once during the plugin's Load method, using its BepInEx GUID.</summary>
    public static void Register(ModDescription description)
    {
        ArgumentNullException.ThrowIfNull(description);
        if(string.IsNullOrWhiteSpace(description.Id)||string.IsNullOrWhiteSpace(description.Name)||!Version.TryParse(description.Version,out _))throw new ArgumentException("A mod ID, name and numeric version are required.",nameof(description));
        lock(Gate)if(!Registered.TryAdd(description.Id,description))throw new InvalidOperationException($"Already registered: {description.Id}");
    }
    /// <summary>Includes loader-discovered plugins even when they do not use this API.</summary>
    public static IReadOnlyList<InstalledMod> GetInstalled()
    {
        var items=new Dictionary<string,InstalledMod>(StringComparer.Ordinal);
        lock(Gate)foreach(var entry in Registered)items[entry.Key]=new(entry.Value,false,0);
        foreach(var plugin in IL2CPPChainloader.Instance.Plugins.Values)
        {
            var info=plugin.Metadata;
            ModDescription description;
            lock(Gate)description=Registered.TryGetValue(info.GUID,out var registered)?registered:new(info.GUID,info.Name,info.Version.ToString());
            description=description with {Version=info.Version.ToString()};
            items[info.GUID]=new(description,plugin.Instance is BasePlugin,plugin.Instance is BasePlugin instance?instance.Config.Count:0);
        }
        return Array.AsReadOnly(items.Values.OrderBy(x=>x.Description.Name,StringComparer.OrdinalIgnoreCase).ThenBy(x=>x.Description.Id,StringComparer.Ordinal).ToArray());
    }
    internal static ConfigFile? ConfigFor(string id)=>IL2CPPChainloader.Instance.Plugins.TryGetValue(id,out var info)&&info.Instance is BasePlugin instance?instance.Config:null;
}
