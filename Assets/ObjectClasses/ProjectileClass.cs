using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class ProjectileClass
{
    public int ID;
    public string FileName;
    public Images.AllodsSprite File;
    public int Phases;
    public bool HasPalette;
    public int Width; // this isn't generally used
    public int Height; // same as above
    public int RotationPhases;
    public bool IsHoming;
    public bool Is16A;
    public bool IsSFX;
    public bool Flip;

    public void UpdateSprite()
    {
        if (File != null)
            return;
        if (!Is16A)
            File = Images.Load256(FileName, HasPalette);
        else File = Images.Load16A(FileName, HasPalette);
    }
}

public class ProjectileClassLoader
{
    internal static bool ClassesLoaded = false;
    internal static List<ProjectileClass> Classes = new List<ProjectileClass>();

    public static ProjectileClass GetProjectileClassById(int id)
    {
        foreach (ProjectileClass cls in Classes)
            if (cls.ID == id)
                return cls;

        return null;
    }


    public static void InitClasses()
    {
        if (ClassesLoaded)
            return;
        ClassesLoaded = true;

        Registry reg = new Registry("graphics/projectiles/projectiles.reg");
        int Count = reg.GetInt("Global", "Count", 0);

        for (int i = 0; i < Count; i++)
        {
            string on = string.Format("Projectile{0}", i);
            ProjectileClass cls = new ProjectileClass();
            cls.ID = reg.GetInt(on, "ID", -1);
            cls.FileName = "graphics/projectiles/"+reg.GetString(on, "File", "");
            cls.Is16A = reg.GetInt(on, "A16", 0) != 0;
            cls.FileName += cls.Is16A ? ".16a" : ".256";
            cls.Phases = reg.GetInt(on, "Phases", 0);
            cls.HasPalette = reg.GetInt(on, "Palette", 0) != 0;
            cls.Width = reg.GetInt(on, "Width", 0);
            cls.Height = reg.GetInt(on, "Height", 0);
            cls.RotationPhases = reg.GetInt(on, "RotationPhases", 0);
            cls.IsSFX = reg.GetInt(on, "SFX", 0) != 0;
            cls.IsHoming = reg.GetInt(on, "Homing", 0) != 0;
            cls.Flip = reg.GetInt(on, "Flip", 0) != 0;
            Debug.LogFormat("{0} flip = {1}", cls.FileName, cls.Flip);
            Classes.Add(cls);
        }
    }
}