using BepInEx.Configuration;
using System.Globalization;

namespace PathlessWoodsModFramework;
/// <summary>Optional ConfigDescription tag for the in-game settings page.</summary>
public sealed record ModSettingOptions(string? DisplayName=null,bool RequiresRestart=false);
internal static class ModSettings
{
    internal static bool Supported(ConfigEntryBase entry)=>entry.SettingType==typeof(bool)||entry.SettingType==typeof(string)||entry.SettingType==typeof(int)||entry.SettingType==typeof(float)||entry.SettingType==typeof(double)||entry.SettingType.IsEnum && !entry.SettingType.IsDefined(typeof(FlagsAttribute),false);
    internal static bool TrySave(ConfigEntryBase entry,string input,out string error)
    {
        error="";object value;
        try
        {
            if(!Supported(entry))throw new ArgumentException("This setting type is read-only.");
            var type=entry.SettingType;
            if(type==typeof(string))value=input;
            else if(type==typeof(bool))value=bool.Parse(input);
            else if(type.IsEnum){value=Enum.Parse(type,input,false);if(!Enum.IsDefined(type,value))throw new FormatException("Unknown enum option.");}
            else value=Convert.ChangeType(input,type,CultureInfo.InvariantCulture);
            if(value is float f && !float.IsFinite(f)||value is double d && !double.IsFinite(d))throw new FormatException("Value must be finite.");
            var acceptable=entry.Description.AcceptableValues;
            if(acceptable is not null && !acceptable.IsValid(value))throw new ArgumentOutOfRangeException(nameof(input),"Value is outside the allowed range/list.");
            object old=entry.BoxedValue;
            try{entry.BoxedValue=value;entry.ConfigFile.Save();}
            catch{entry.BoxedValue=old;throw;}
            return true;
        }
        catch(Exception ex){error=ex.Message;return false;}
    }
    internal static string Value(ConfigEntryBase entry)=>Convert.ToString(entry.BoxedValue,CultureInfo.InvariantCulture)??"";
}
