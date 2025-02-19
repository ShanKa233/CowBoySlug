using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace CowBoySlug
{
  public class HatData
  {
    public static Dictionary<string, HatData> HatsDictionary = new Dictionary<string, HatData>();
    //该帽子记录在存档内的id
    public string id { get; set; }

    //帽子的贴图名称
    public string sprite_name { get; set; }

    //附加方式有贴纸,飘带(stiker)
    //public string addition_type { get; set; }


    //戴上后贴图会不会归位
    //public bool centerlock { get; set; }
    public override string ToString() => String.Format("\tid:{0},\tsprite_name:{1}", id, sprite_name);
  }


  public class LoadHats
  {
    public static bool loaded = false;
    public static void Hook()
    {
      if (!loaded)
      {
        InitHatData();
        loaded = true;
        //UnityEngine.Debug.Log(("帽子类型是", HatType.Strap.ToString()));
      }
    }

    public static string cowBoyHatFolderName = "cowboyhats";

    public static void InitHatData()
    {
      foreach (var mod in ModManager.ActiveMods)
      {
        string path = mod.path + Path.DirectorySeparatorChar + cowBoyHatFolderName;

        if (!Directory.Exists(path))
          continue;

        LoadInDirectory(new DirectoryInfo(path), new DirectoryInfo(mod.path).FullName);
      }
    }

    public static void LoadInDirectory(DirectoryInfo info, string rootPath)
    {

      foreach (var dir in info.GetDirectories())
      {
        LoadInDirectory(dir, rootPath);
      }

      try
      {
        foreach (var png in info.GetFiles("*.png"))
        {
          Futile.atlasManager.LoadImage(cowBoyHatFolderName + Path.DirectorySeparatorChar + png.Name.Replace(".png", ""));
        }
      }
      catch (Exception)
      {
        Debug.LogError("Can'tFindHatPng");
      }

      foreach (var file in info.GetFiles("*.json"))
      {

        string JSONstring = File.ReadAllText(file.FullName);


        var hat = JsonConvert.DeserializeObject<HatData>(JSONstring);


        if (hat.id != null && hat.sprite_name != null)
        {
          //if (hat.addition_type == null) hat.addition_type = "stiker";

          HatData.HatsDictionary.Add(hat.id, hat);
          UnityEngine.Debug.Log("[cowboyhat]" + hat);
        }

      }


    }
  }
}
