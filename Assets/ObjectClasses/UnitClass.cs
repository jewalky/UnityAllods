using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

public class UnitClass
{
    public readonly static int MagicIntNull = unchecked((int)0x0DEAD666);

    public struct AnimationFrame
    {
        public int Time;
        public int Frame;
    }

    public string DescText = null;
    public int ID = MagicIntNull;
    public int ParentID = MagicIntNull;
    public UnitClass Parent = null;
    public UnitFile File = null;
    public int Index = MagicIntNull;
    public int IdlePhases = MagicIntNull;
    public int MovePhases = MagicIntNull;
    public int MoveBeginPhases = MagicIntNull;
    public int AttackPhases = MagicIntNull;
    public int DyingPhases = MagicIntNull; // these are sometimes in another file.
    public int BonePhases = MagicIntNull; // same as above.
    public float CenterX = -2;
    public float CenterY = -2;
    public int SelectionX1 = MagicIntNull;
    public int SelectionX2 = MagicIntNull;
    public int SelectionY1 = MagicIntNull;
    public int SelectionY2 = MagicIntNull;
    public AnimationFrame[] AttackFrames = null;
    public AnimationFrame[] MoveFrames = null;
    public AnimationFrame[] IdleFrames = null;
    public int DyingID = MagicIntNull;
    public UnitClass Dying = null;
    public int Palette = MagicIntNull;
    public int AttackDelay = MagicIntNull;
    public string InfoPicture = null;
    //public Texture2D InfoPictureFile = null;
    private Dictionary<int, Texture2D> TextureFile = new Dictionary<int, Texture2D>();
    public bool InMapEditor = false;
    public bool Flip = false; // if sprite flip should be done

    public Texture2D UpdateInfoPicture(int face)
    {
        if (TextureFile.ContainsKey(face))
            return TextureFile[face];

        string picName = InfoPicture;
        if (face > 1)
            picName += face.ToString();
        picName += ".bmp";
        Texture2D pic = Images.LoadImage(picName, 0, Images.ImageType.AllodsBMP);
        TextureFile[face] = pic;
        return pic;
    }
}

public class UnitFile
{
    public Images.AllodsSpriteSeparate File = null;
    public Images.AllodsSpriteSeparate FileB = null;
    public string FileName = "";
    private bool Loaded = false;
    private Dictionary<int, Texture2D> Palettes = new Dictionary<int, Texture2D>();

    public void UpdateSprite()
    {
        if (!Loaded)
        {
            try
            {
                File = Images.Load256Separate(FileName + ".256");
            }
            catch (AllodsException)
            {
                File = null;
            }
            try
            {
                FileB = Images.Load256Separate(FileName + "b.256");
            }
            catch (AllodsException)
            {
                FileB = null;
            }
            Loaded = true;
        }
    }

    public Texture2D UpdatePalette(int num)
    {
        if (Palettes.ContainsKey(num))
            return Palettes[num];

        string filename = "palette.pal";
        if (num > 1) filename = string.Format("palette{0}.pal", num);
        string dirname = Path.GetDirectoryName(FileName);
        Texture2D palette = Images.LoadPalette(dirname + "/" + filename);
        Palettes[num] = palette;
        return palette;
    }
}

public class UnitClassLoader
{
    internal static bool ClassesLoaded = false;
    internal static List<UnitFile> Files = new List<UnitFile>();
    internal static List<UnitClass> Classes = new List<UnitClass>();

    public static UnitClass GetUnitClassById(int typeId)
    {
        foreach (UnitClass cls in Classes)
        {
            if (cls.ID == typeId)
                return cls;
        }

        return null;
    }

    public static UnitClass GetUnitClassByName(string name)
    {
        name = name.ToLower();
        foreach (UnitClass cls in Classes)
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
        Registry reg = new Registry("graphics/units/units.reg");
        int UnitCount = reg.GetInt("Global", "UnitCount", 0);
        int FileCount = reg.GetInt("Global", "FileCount", 0);
        for (int i = 0; i < FileCount; i++)
        {
            string filename = reg.GetString("Files", string.Format("File{0}", i), "");
            UnitFile file = new UnitFile();
            file.FileName = "graphics/units/" + filename.Replace('\\', '/');
            Files.Add(file);
        }

        for (int i = 0; i < UnitCount; i++)
        {
            string on = string.Format("Unit{0}", i);
            UnitClass cls = new UnitClass();

            cls.DescText = reg.GetString(on, "DescText", cls.DescText);
            cls.ID = reg.GetInt(on, "ID", cls.ID);
            int clsFile = reg.GetInt(on, "File", -1);
            if (clsFile >= 0 && clsFile < Files.Count)
                cls.File = Files[clsFile];
            cls.ParentID = reg.GetInt(on, "Parent", cls.ParentID);
            cls.Index = reg.GetInt(on, "Index", cls.Index);
            cls.IdlePhases = reg.GetInt(on, "IdlePhases", cls.IdlePhases);
            cls.MovePhases = reg.GetInt(on, "MovePhases", cls.MovePhases);
            cls.MoveBeginPhases = reg.GetInt(on, "MoveBeginPhases", cls.MoveBeginPhases);
            cls.AttackPhases = reg.GetInt(on, "AttackPhases", cls.AttackPhases);
            cls.DyingPhases = reg.GetInt(on, "DyingPhases", cls.DyingPhases);
            cls.BonePhases = reg.GetInt(on, "BonePhases", cls.BonePhases);
            int w = reg.GetInt(on, "Width", -1);
            int h = reg.GetInt(on, "Height", -1);
            if (w > 0 && h > 0)
            {
                int cx = reg.GetInt(on, "CenterX", UnitClass.MagicIntNull);
                int cy = reg.GetInt(on, "CenterY", UnitClass.MagicIntNull);
                if (cx != UnitClass.MagicIntNull && cy != UnitClass.MagicIntNull)
                {
                    cls.CenterX = (float)cx / w;
                    cls.CenterY = (float)cy / h;
                }

                int s1x = reg.GetInt(on, "SelectionX1", UnitClass.MagicIntNull);
                int s1y = reg.GetInt(on, "SelectionY1", UnitClass.MagicIntNull);
                int s2x = reg.GetInt(on, "SelectionX2", UnitClass.MagicIntNull);
                int s2y = reg.GetInt(on, "SelectionY2", UnitClass.MagicIntNull);
                if (s1x != UnitClass.MagicIntNull && s1y != UnitClass.MagicIntNull &&
                    s2x != UnitClass.MagicIntNull && s2y != UnitClass.MagicIntNull)
                {
                    cls.SelectionX1 = s1x - cx;
                    cls.SelectionX2 = s2x - cx;
                    cls.SelectionY1 = s1y - cy;
                    cls.SelectionY2 = s2y - cy;
                    
                }
            }

            int[] attackAnimFrame = reg.GetArray(on, "AttackAnimFrame", null);
            int[] attackAnimTime = reg.GetArray(on, "AttackAnimTime", null);
            if (attackAnimFrame != null && attackAnimTime != null &&
                attackAnimFrame.Length == attackAnimTime.Length)
            {
                cls.AttackFrames = new UnitClass.AnimationFrame[attackAnimFrame.Length];
                for (int j = 0; j < cls.AttackFrames.Length; j++)
                {
                    UnitClass.AnimationFrame af;
                    af.Frame = attackAnimFrame[j];
                    af.Time = attackAnimTime[j];
                    cls.AttackFrames[j] = af;
                }
            }

            int[] moveAnimFrame = reg.GetArray(on, "MoveAnimFrame", null);
            int[] moveAnimTime = reg.GetArray(on, "MoveAnimTime", null);
            if (moveAnimFrame != null && moveAnimTime != null &&
                moveAnimFrame.Length == moveAnimTime.Length)
            {
                cls.MoveFrames = new UnitClass.AnimationFrame[moveAnimFrame.Length];
                for (int j = 0; j < cls.MoveFrames.Length; j++)
                {
                    UnitClass.AnimationFrame af;
                    af.Frame = moveAnimFrame[j];
                    af.Time = moveAnimTime[j];
                    cls.MoveFrames[j] = af;
                }
            }

            int[] idleAnimFrame = reg.GetArray(on, "IdleAnimFrame", null);
            int[] idleAnimTime = reg.GetArray(on, "IdleAnimTime", null);
            if (idleAnimFrame != null && idleAnimTime != null &&
                idleAnimFrame.Length == idleAnimTime.Length)
            {
                cls.IdleFrames = new UnitClass.AnimationFrame[idleAnimFrame.Length];
                for (int j = 0; j < cls.IdleFrames.Length; j++)
                {
                    UnitClass.AnimationFrame af;
                    af.Frame = idleAnimFrame[j];
                    af.Time = idleAnimTime[j];
                    cls.IdleFrames[j] = af;
                }
            }

            cls.DyingID = reg.GetInt(on, "Dying", cls.DyingID);
            cls.Palette = reg.GetInt(on, "Palette", cls.Palette);
            cls.AttackDelay = reg.GetInt(on, "AttackDelay", cls.AttackDelay);
            cls.InfoPicture = reg.GetString(on, "InfoPicture", cls.InfoPicture);
            cls.InMapEditor = reg.GetInt(on, "InMapEditor", 0) != 0;
            cls.Flip = reg.GetInt(on, "Flip", 0) != 0;

            Classes.Add(cls);
        }

        foreach (UnitClass cls in Classes)
        {
            int id = cls.ParentID;
            while (id != -1)
            {
                UnitClass clsp = null;
                foreach (UnitClass clsp_ in Classes)
                {
                    if (clsp_.ID == id)
                    {
                        clsp = clsp_;
                        break;
                    }
                }

                if (clsp == null)
                    break;

                if (cls.Parent == null)
                    cls.Parent = clsp;

                if (cls.DescText == null)
                    cls.DescText = clsp.DescText;
                if (cls.File == null)
                    cls.File = clsp.File;
                if (cls.Index == UnitClass.MagicIntNull)
                    cls.Index = clsp.Index;
                if (cls.IdlePhases == UnitClass.MagicIntNull)
                    cls.IdlePhases = clsp.IdlePhases;
                if (cls.MovePhases == UnitClass.MagicIntNull)
                    cls.MovePhases = clsp.MovePhases;
                if (cls.MoveBeginPhases == UnitClass.MagicIntNull)
                    cls.MoveBeginPhases = clsp.MoveBeginPhases;
                if (cls.AttackPhases == UnitClass.MagicIntNull)
                    cls.AttackPhases = clsp.AttackPhases;
                if (cls.DyingPhases == UnitClass.MagicIntNull)
                    cls.DyingPhases = clsp.DyingPhases;
                if (cls.BonePhases == UnitClass.MagicIntNull)
                    cls.BonePhases = clsp.BonePhases;
                if (cls.CenterX == -2)
                    cls.CenterX = clsp.CenterX;
                if (cls.CenterY == -2)
                    cls.CenterY = clsp.CenterY;
                if (cls.SelectionX1 == UnitClass.MagicIntNull)
                    cls.SelectionX1 = clsp.SelectionX1;
                if (cls.SelectionY1 == UnitClass.MagicIntNull)
                    cls.SelectionY1 = clsp.SelectionY1;
                if (cls.SelectionX2 == UnitClass.MagicIntNull)
                    cls.SelectionX2 = clsp.SelectionX1;
                if (cls.SelectionY2 == UnitClass.MagicIntNull)
                    cls.SelectionY2 = clsp.SelectionY1;
                if (cls.AttackFrames == null)
                    cls.AttackFrames = clsp.AttackFrames;
                if (cls.MoveFrames == null)
                    cls.MoveFrames = clsp.MoveFrames;
                if (cls.DyingID == UnitClass.MagicIntNull)
                    cls.DyingID = clsp.DyingID;
                if (cls.Palette == UnitClass.MagicIntNull)
                    cls.Palette = clsp.Palette;
                if (cls.AttackDelay == UnitClass.MagicIntNull)
                    cls.AttackDelay = clsp.AttackDelay;
                if (cls.InfoPicture == null)
                    cls.InfoPicture = clsp.InfoPicture;
                if (cls.InMapEditor == false)
                    cls.InMapEditor = clsp.InMapEditor;
                if (cls.Flip == false)
                    cls.Flip = clsp.Flip;

                id = clsp.ParentID;
            }
        }

        foreach (UnitClass cls in Classes)
        {
            if (cls.DescText == null)
                cls.DescText = "<INVALID>";
            if (cls.Index == UnitClass.MagicIntNull)
                cls.Index = 0;
            if (cls.IdlePhases == UnitClass.MagicIntNull)
                cls.IdlePhases = 1;
            if (cls.MovePhases == UnitClass.MagicIntNull)
                cls.MovePhases = 0;
            if (cls.MoveBeginPhases == UnitClass.MagicIntNull)
                cls.MoveBeginPhases = 0;
            if (cls.AttackPhases == UnitClass.MagicIntNull)
                cls.AttackPhases = 0;
            if (cls.DyingPhases == UnitClass.MagicIntNull)
                cls.DyingPhases = 0;
            if (cls.BonePhases == UnitClass.MagicIntNull)
                cls.BonePhases = 0;
            if (cls.CenterX == -2)
                cls.CenterX = 0;
            if (cls.CenterY == -2)
                cls.CenterY = 0;
            if (cls.SelectionX1 == UnitClass.MagicIntNull)
                cls.SelectionX1 = 0;
            if (cls.SelectionY1 == UnitClass.MagicIntNull)
                cls.SelectionY1 = 0;
            if (cls.SelectionX2 == UnitClass.MagicIntNull)
                cls.SelectionX2 = 0;
            if (cls.SelectionY2 == UnitClass.MagicIntNull)
                cls.SelectionY2 = 0;
            if (cls.AttackFrames == null)
                cls.AttackFrames = null;
            if (cls.MoveFrames == null)
                cls.MoveFrames = null;
            if (cls.DyingID == UnitClass.MagicIntNull)
                cls.DyingID = cls.ID;
            if (cls.Palette == UnitClass.MagicIntNull)
                cls.Palette = -1;
            if (cls.AttackDelay == UnitClass.MagicIntNull)
                cls.AttackDelay = 0;
            if (cls.InfoPicture == null)
                cls.InfoPicture = "beeh";
            cls.Dying = GetUnitClassById(cls.DyingID);
            cls.InfoPicture = "graphics/infowindow/" + cls.InfoPicture;
        }
    }

    private static int LastLoaded = 0;
    public static float LoadSprites(float time)
    {
        InitClasses();

        float timestart = Time.realtimeSinceStartup;
        for (; LastLoaded < Files.Count; LastLoaded++)
        {
            Files[LastLoaded].UpdateSprite();
            if (Time.realtimeSinceStartup - timestart > time)
                break;
        }

        return (float)LastLoaded / Files.Count;
    }
}
