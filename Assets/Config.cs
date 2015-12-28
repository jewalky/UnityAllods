using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public static void Save()
    {
        // write file with current values
        using (FileStream fs = File.OpenWrite("unityallods.cfg"))
        {
            StreamWriter sw = new StreamWriter(fs);
            PropertyInfo[] configFields = typeof(Config).GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            foreach (PropertyInfo field in configFields)
            {
                string cmd = GameConsole.JoinArguments(new string[] { field.Name.ToLower(), field.GetValue(null, null).ToString() });
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