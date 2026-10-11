using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using PathlessWoodsModFramework;

namespace ExampleMod;
[BepInPlugin(Id,"Framework Example Mod","0.1.0")]
[BepInDependency(FrameworkPlugin.Id)]
public sealed class ExamplePlugin:BasePlugin
{
    public const string Id="example.pathlesswoods.frameworkdemo";
    public override void Load()
    {
        ModRegistry.Register(new(Id,"Framework Example Mod","0.1.0","An independent consumer of the framework API.","Example author"));
        Config.Bind("General","Enabled",true,new ConfigDescription("Example toggle."));
        Config.Bind("General","Strength",5,new ConfigDescription("Example bounded integer.",new AcceptableValueRange<int>(1,10),new ModSettingOptions("Example strength",true)));
        Config.Bind("General","Message","Hello",new ConfigDescription("Example text."));
        Config.Bind("General","Mode",ExampleMode.Normal,new ConfigDescription("Example enum."));
        FrameworkRuntime.Post(()=>Log.LogInfo("Example callback ran on the main thread."));
    }
}
public enum ExampleMode{Normal,Quiet}
