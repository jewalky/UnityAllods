using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class StructureClass
{
    public readonly static int MagicIntNull = unchecked((int)0xDEADBABE);

    public struct AnimationFrame
    {
        public int Time;
        public int Frame;
    }

    public string DescText = null;
    public int ID = MagicIntNull;
    public StructureFile File = null;
    public int TileWidth = MagicIntNull;
    public int TileHeight = MagicIntNull;
    public int FullHeight = MagicIntNull;
    public int SelectionX1 = MagicIntNull;
    public int SelectionX2 = MagicIntNull;
    public int SelectionY1 = MagicIntNull;
    public int SelectionY2 = MagicIntNull;
    public int ShadowY = MagicIntNull;
    public string AnimMask = null;
    public AnimationFrame[] Frames = null;
    public string Picture = null;
    public Texture2D PictureFile = null;
    public int IconID = MagicIntNull;
    public bool Indestructible = false;
    public bool Usable = false;
    public bool Flat = false;
    public int LightRadius = MagicIntNull;
    public int LightPulse = MagicIntNull;
}

public class StructureFile : ObstacleFile { }

public class StructureClassLoader
{
    internal static bool ClassesLoaded = false;
    internal static List<StructureClass> Classes = new List<StructureClass>();

    public static StructureClass GetStructureClassById(int typeId)
    {
        foreach (StructureClass cls in Classes)
        {
            if (cls.ID == typeId)
                return cls;
        }

        return null;
    }

    public static StructureClass GetStructureClassByName(string name)
    {
        name = name.ToLower();
        foreach (StructureClass cls in Classes)
        {
            if (cls.DescText.ToLower() == name)
                return cls;
        }

        return null;
    }

    public static void InitClasses()
    {
        if (ClassesLoaded)
            return;
        ClassesLoaded = true;
        Registry reg = new Registry("graphics/structures/structures.reg");
        int ObjectCount = reg.GetInt("Global", "Count", 0);
        for (int i = 0; i < ObjectCount; i++)
        {
            string on = string.Format("Structure{0}", i);
            StructureClass cls = new StructureClass();
            cls.DescText = reg.GetString(on, "DescText", "");
            cls.ID = reg.GetInt(on, "ID", -1);
            string file = reg.GetString(on, "File", null);
            if (file != null)
            {
                StructureFile sfile = new StructureFile();
                sfile.FileName = "graphics/structures/" + file;
                cls.File = sfile;
            }
            cls.TileWidth = reg.GetInt(on, "TileWidth", 1);
            cls.TileHeight = reg.GetInt(on, "TileHeight", 1);
            cls.FullHeight = reg.GetInt(on, "FullHeight", cls.TileHeight);
            cls.SelectionX1 = reg.GetInt(on, "SelectionX1", 0);
            cls.SelectionY1 = reg.GetInt(on, "SelectionY1", 0);
            cls.SelectionX2 = reg.GetInt(on, "SelectionX2", cls.TileWidth * 32);
            cls.SelectionY2 = reg.GetInt(on, "SelectionY2", cls.FullHeight * 32);
            cls.ShadowY = reg.GetInt(on, "ShadowY", 0);
            if (cls.ShadowY < 0) // this is very bad. this means that Nival artists were trying to make this structure Flat, but didn't know about the feature.
            {                    // as such, they were setting ShadowY -20000 and the shadow was still appearing sometimes despite LOOKING AS IF it was flat.
                cls.Flat = true;
                cls.ShadowY = 0;
            }

            // also this fixes Bee houses
            if (cls.ShadowY > cls.FullHeight * 32)
                cls.ShadowY = 0;

            cls.AnimMask = reg.GetString(on, "AnimMask", null);
            int phases = reg.GetInt(on, "Phases", 1);
            if (phases == 1)
            {
                cls.Frames = new StructureClass.AnimationFrame[1];
                cls.Frames[0].Frame = 0;
                cls.Frames[0].Time = 0;
            }
            else
            {
                int[] animFrame = reg.GetArray(on, "AnimFrame", null);
                int[] animTime = reg.GetArray(on, "AnimTime", null);
                if (animFrame == null || animTime == null ||
                    animFrame.Length != animTime.Length)
                {
                    // some structures already have invalid definitions.
                    cls.Frames = new StructureClass.AnimationFrame[1];
                    cls.Frames[0].Frame = 0;
                    cls.Frames[0].Time = 0;
                }
                else
                {
                    cls.Frames = new StructureClass.AnimationFrame[animFrame.Length];
                    for (int j = 0; j < animFrame.Length; j++)
                    {
                        cls.Frames[j].Frame = animFrame[j];
                        cls.Frames[j].Time = animTime[j];
                    }
                }
            }
            cls.Picture = "graphics/infowindow/" + reg.GetString(on, "Picture", "") + ".bmp";
            cls.IconID = reg.GetInt(on, "IconID", StructureClass.MagicIntNull);
            cls.Indestructible = reg.GetInt(on, "Indestructible", 0)!=0;
            cls.Usable = reg.GetInt(on, "Usable", 0)!=0;
            cls.Flat = reg.GetInt(on, "Flat", 0)!=0;
            cls.LightRadius = reg.GetInt(on, "LightRadius", 0);
            cls.LightPulse = reg.GetInt(on, "LightPulse", 0);
            Classes.Add(cls);
        }
    }
}