using System.Reflection;
using System.IO;
using UnityEngine;

public class Config
{
    public static bool cl_grid
    {
        get
        {
            return MapView.Instance.GridEnabled;
        }

        set
        {
            MapView.Instance.GridEnabled = value;
            Save();
        }
    }

    public static bool cl_chatalternate
    {
        get
        {
            return MapViewChat.Instance.AlternateColors;
        }

        set
        {
            MapViewChat.Instance.AlternateColors = value;
            Save();
        }
    }

    private static string _sv_avatar = "Urd";
    public static string sv_avatar
    {
        get
        {
            return _sv_avatar;
        }

        set
        {
            _sv_avatar = value;
            Save();
        }
    }

    private static string _cl_nickname = "";
    public static string cl_nickname
    {
        get
        {
            return _cl_nickname;
        }

        set
        {
            _cl_nickname = value;
            Save();
        }
    }

    public static bool cl_spritesb
    {
        get
        {
            return MapView.Instance.SpritesBEnabled;
        }

        set
        {
            MapView.Instance.SpritesBEnabled = value;
            Save();
        }
    }

    private static int _sv_pathfinding = 0;
    public static int sv_pathfinding
    {
        get
        {
            return _sv_pathfinding;
        }

        set
        {
            _sv_pathfinding = value;
            Save();
        }
    }

    public static void Save()
    {
        // write file with current values
        using (FileStream fs = File.Open("unityallods.cfg", FileMode.Create))
        {
            
            StreamWriter sw = new StreamWriter(fs);
            PropertyInfo[] configFields = typeof(Config).GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            foreach (PropertyInfo field in configFields)
            {
                string cmd = GameConsole.JoinArguments(new[] { field.Name.ToLower(), field.GetValue(null, null).ToString() });
                sw.WriteLine(cmd);
            }

            sw.Flush();
        }
    }

    public static void Load()
    {
        if (ResourceManager.FileExists("unityallods.cfg"))
        {
            StringFile sf = new StringFile("unityallods.cfg");
            foreach (string cmd in sf.Strings)
                GameConsole.Instance.ExecuteCommand(cmd);
        }
        else Save();
    }
}