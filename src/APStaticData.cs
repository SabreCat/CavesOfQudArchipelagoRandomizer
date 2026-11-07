using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XRL;

[System.Serializable]
public class LocationInfo
{
    public string Category;
    public string Type;
    public int Amount = 1;
    public string Blueprint;
}

[System.Serializable]
public class ItemInfo
{
    public string Category;
    public string Type;
    public int Amount = 1;
    public string Blueprint;
    public string Population;
}

public static class APStaticData
{
    // The mods folder must not be renamed or this path doesn't match
    private static readonly string _modPath = DataManager.SavePath(
        @"Mods/CavesOfQudArchipelagoRandomizer"
    );

    public static readonly string WorldVersion = LoadWorldVersion();

    private static string LoadWorldVersion()
    {
        return (string)JObject.Parse(
            File.ReadAllText(_modPath + @"/Archipelago/worlds/cavesofqud/archipelago.json")
        )["world_version"];
    }

    public static readonly Assembly MultiClient = Assembly.LoadFrom(
        _modPath + @"/thirdparty/Archipelago.MultiClient.Net.dll"
    );

    public static readonly Dictionary<string, LocationInfo> Locations = LoadStaticLocationDefs();

    private static Dictionary<string, LocationInfo> LoadStaticLocationDefs()
    {
        string json = File.ReadAllText(
            _modPath + @"/Archipelago/worlds/cavesofqud/data/Locations.json"
        );
        var locs = JsonConvert.DeserializeObject<Dictionary<string, LocationInfo>>(json);
        return locs;
    }

    public static readonly Dictionary<string, ItemInfo> Items = LoadStaticItemDefs();

    private static Dictionary<string, ItemInfo> LoadStaticItemDefs()
    {
        string json = File.ReadAllText(
            _modPath + @"/Archipelago/worlds/cavesofqud/data/Items.json"
        );
        var items = JsonConvert.DeserializeObject<Dictionary<string, ItemInfo>>(json);
        return items;
    }
}
