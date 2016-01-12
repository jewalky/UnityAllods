using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewHuman : MapViewUnit
{
    private MapHuman LogicHuman
    {
        get
        {
            return (MapHuman)LogicObject;
        }
    }

    protected override Texture2D GetPalette()
    {
        return UpdateHumanPalette();
    }

    private static Texture2D[,] _HumanPalettes = null;
    // type = 0 - heroes
    // type = 1 - heroes_l
    // type = 2 - humans
    private Texture2D UpdateHumanPalette(int type = 2)
    {
        if (_HumanPalettes == null)
        {
            _HumanPalettes = new Texture2D[3, 17];
            for (int i = 0; i < 3; i++)
            {
                string subdir = new string[] { "heroes", "heroes_l", "humans" }[i];
                // palettes 0 to 15 (#1 to #16) are in human.pal file.
                for (int j = 0; j < 16; j++)
                    _HumanPalettes[i, j] = Images.LoadPalette(string.Format("graphics/units/{0}/human.pal", subdir), (uint)(j * 256 * 4));
                // palette 16 (#17) should be generated manually.
                Color[] pixels = _HumanPalettes[i, 0].GetPixels();
                for (int j = 0; j < pixels.Length; j++)
                {
                    float avg = (pixels[j].r * 0.21f + pixels[j].g * 0.72f + pixels[j].b * 0.07f);
                    pixels[j].r = avg;
                    pixels[j].g = avg;
                    pixels[j].b = avg;
                }
                _HumanPalettes[i, 16] = new Texture2D(pixels.Length, 1, TextureFormat.ARGB32, false);
                _HumanPalettes[i, 16].filterMode = FilterMode.Point;
                _HumanPalettes[i, 16].SetPixels(pixels);
                _HumanPalettes[i, 16].Apply();
            }
        }

        return _HumanPalettes[type, LogicHuman.Player.Color];
    }

    // humans have separate picture mechanism
    // 
    private static Dictionary<int, Texture2D> _HumanPictures = new Dictionary<int, Texture2D>();
    public Texture2D UpdateHumanPic()
    {
        int dictKey = ((int)LogicHuman.Gender << 8) + LogicHuman.Face;
        if (_HumanPictures.ContainsKey(dictKey))
            return _HumanPictures[dictKey];

        string subdir;
        if ((LogicHuman.Gender & MapHuman.GenderFlags.Male) != 0)
            subdir = "m";
        else if ((LogicHuman.Gender & MapHuman.GenderFlags.Female) != 0)
            subdir = "f";
        else return null;
        if ((LogicHuman.Gender & MapHuman.GenderFlags.Fighter) != 0)
            subdir += "fighter";
        else if ((LogicHuman.Gender & MapHuman.GenderFlags.Mage) != 0)
            subdir += "mage";
        else return null;

        return _HumanPictures[dictKey] = Images.Load256AsTexture(string.Format("graphics/equipment/{0}/{1}.256", subdir, LogicHuman.Face));
    }

    // special handling for displaypic
    private static GameObject HumanTexObject;
    private static MeshRenderer HumanTexRenderer;
    private static MeshFilter HumanTexFilter;
    private static Material[] HumanTexMaterials;

    private static Texture2D[] HumanCloak = new Texture2D[2]; // male, female
    private static Texture2D[] HumanSprites = new Texture2D[16];
    private static Item[] HumanItems = new Item[16];

    // the most epic perversion so far
    private void UpdateItemPic()
    {
        for (int i = 0; i < 16; i++)
        {
            HumanSprites[i] = null;
            HumanItems[i] = null;
        }

        HumanSprites[1] = UpdateHumanPic();
        for (int i = 0; i < LogicHuman.ItemsBody.Length; i++)
        {
            Item item = LogicHuman.ItemsBody[i];
            if (item == null)
                continue;

            int slot = item.Class.Option.Slot;
            if (slot == 0 || slot == 3 || slot == 11 || (slot >= 13 && slot <= 15))
                continue;

            ItemFile file1;
            ItemFile file2;

            if ((LogicHuman.Gender & MapHuman.GenderFlags.Fighter) != 0) // fighter
            {
                // order of fighter equipment:
                // cloak* -> FACE -> ring1 -> ring2 -> amulet -> boots -> mail -> bracers2 -> gauntlets2 -> cuirass
                //        -> bracers1 -> gauntlets1 -> helm -> weapon -> shield

                if ((LogicHuman.Gender & MapHuman.GenderFlags.Male) != 0)
                {
                    file1 = item.Class.File_BodyMF1;
                    file2 = item.Class.File_BodyMF2;
                }
                else
                {
                    file1 = item.Class.File_BodyFF1;
                    file2 = item.Class.File_BodyFF2;
                }

                file1.UpdateSprite();

                if (LogicHuman.IsHero)
                {
                    int fighter_cloak_num = ((LogicHuman.Gender & MapHuman.GenderFlags.Male) != 0) ? 0 : 1;
                    if (HumanCloak[fighter_cloak_num] == null)
                        HumanCloak[fighter_cloak_num] = Images.Load256AsTexture((fighter_cloak_num == 0) ? "graphics/interface/heroback/backm.256" : "graphics/interface/heroback/backf.256");
                    HumanSprites[0] = HumanCloak[fighter_cloak_num];
                }

                switch (slot)
                {
                    case 9: // bracers
                        file2.UpdateSprite();
                        HumanSprites[7] = file1.File;
                        HumanSprites[10] = file2.File;
                        HumanItems[7] = HumanItems[10] = item;
                        break;

                    case 10: // gauntlets
                        file2.UpdateSprite();
                        HumanSprites[8] = file1.File;
                        HumanSprites[11] = file2.File;
                        HumanItems[8] = HumanItems[11] = item;
                        break;

                    case 4: // ring
                        file2.UpdateSprite();
                        HumanSprites[2] = file1.File;
                        HumanSprites[3] = file2.File;
                        HumanItems[2] = HumanItems[3] = item;
                        break;

                    case 5: // amulet
                        HumanSprites[4] = file1.File;
                        HumanItems[4] = item;
                        break;

                    case 6: // helm
                        HumanSprites[12] = file1.File;
                        HumanItems[12] = item;
                        break;

                    case 7: // mail
                        HumanSprites[6] = file1.File;
                        HumanItems[6] = item;
                        break;

                    case 8: // cuirass
                        HumanSprites[9] = file1.File;
                        HumanItems[9] = item;
                        break;

                    case 12: // boots
                        HumanSprites[5] = file1.File;
                        HumanItems[5] = item;
                        break;

                    case 1: // weapon
                        HumanSprites[13] = file1.File;
                        HumanItems[13] = item;
                        break;

                    case 2: // shield
                        HumanSprites[14] = file1.File;
                        HumanItems[14] = item;
                        break;
                }
            }
            else if ((LogicHuman.Gender & MapHuman.GenderFlags.Mage) != 0)
            {
                // order of mage equipment:
                // cloak1 -> face -> ring1 -> ring2 -> boots ->
                //   gloves1 -> robe -> amulet -> cloak2 -> gloves2 -> hat -> staff

                if ((LogicHuman.Gender & MapHuman.GenderFlags.Male) != 0)
                {
                    file1 = item.Class.File_BodyMM1;
                    file2 = item.Class.File_BodyMM2;
                }
                else
                {
                    file1 = item.Class.File_BodyFM1;
                    file2 = item.Class.File_BodyFM2;
                }

                file1.UpdateSprite();

                switch (slot)
                {
                    case 4: // ring
                        file2.UpdateSprite();
                        HumanSprites[2] = file1.File;
                        HumanSprites[3] = file2.File;
                        HumanItems[2] = HumanItems[3] = item;
                        break;

                    case 10: // gloves
                        file2.UpdateSprite();
                        HumanSprites[5] = file1.File;
                        HumanSprites[9] = file2.File;
                        HumanItems[5] = HumanItems[9] = item;
                        break;

                    case 8: // cloak
                        file2.UpdateSprite();
                        HumanSprites[0] = file1.File;
                        HumanSprites[8] = file2.File;
                        HumanItems[0] = HumanItems[8] = item;
                        break;

                    case 5: // amulet
                        HumanSprites[7] = file1.File;
                        HumanItems[7] = item;
                        break;

                    case 6: // hat
                        HumanSprites[10] = file1.File;
                        HumanItems[10] = item;
                        break;

                    case 7: // robe
                        HumanSprites[6] = file1.File;
                        HumanItems[6] = item;
                        break;

                    case 12: // shoes
                        HumanSprites[4] = file1.File;
                        HumanItems[4] = item;
                        break;

                    case 1: // staff
                        HumanSprites[11] = file1.File;
                        HumanItems[11] = item;
                        break;
                }
            }
        }

        // generate supermesh of 16 materials
        if (HumanTexMaterials == null)
        {
            HumanTexMaterials = new Material[16];
            for (int i = 0; i < 16; i++)
                HumanTexMaterials[i] = new Material(MainCamera.MainShader);
        }

        for (int i = 0; i < 16; i++)
            HumanTexMaterials[i].mainTexture = HumanSprites[i];

        // materials done. make mesh itself
        if (HumanTexObject == null)
        {
            HumanTexObject = Utils.CreateObject();
            HumanTexObject.name = "MapViewHuman$InfoPic";
            HumanTexRenderer = HumanTexObject.AddComponent<MeshRenderer>();
            HumanTexFilter = HumanTexObject.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            int pp = 0, ppc = 0, ppt = 0;

            Vector3[] qv = new Vector3[4 * 16];
            Vector2[] quv = new Vector2[4 * 16];

            for (int i = 0; i < 16; i++)
                Utils.PutQuadInMesh(qv, quv, null, ref pp, ref ppt, ref ppc, 0, 0, 160, 240, new Rect(0, 0, 1, 1), new Color());

            mesh.vertices = qv;
            mesh.uv = quv;

            mesh.subMeshCount = 16;
            for (int i = 0; i < 16; i++)
            {
                int[] qt = { i * 4, i * 4 + 1, i * 4 + 2, i * 4 + 3 };
                mesh.SetIndices(qt, MeshTopology.Quads, i);
            }

            HumanTexFilter.mesh = mesh;
            HumanTexRenderer.materials = HumanTexMaterials;
        }

        // update colors every time
        Color[] qc = new Color[4 * 16];
        for (int i = 0; i < 16; i++)
        {
            Color c = (HumanSprites[i] == null) ? new Color(0, 0, 0, 0) : new Color(1, 1, 1, 1);
            for (int j = 0; j < 4; j++)
                qc[i * 4 + j] = c;
        }

        HumanTexFilter.mesh.colors = qc;
    }

    public override void DisplayPic(bool on, Transform parent)
    {
        base.DisplayPic(false, parent); // disable monster pic
        if (on)
        {
            // load image
            UpdateItemPic();
            // init infowindow
            HumanTexObject.SetActive(true);

            HumanTexRenderer.transform.parent = parent;
            HumanTexRenderer.transform.localPosition = new Vector3(0, 2, -0.01f);
        }
        else
        {
            if (HumanTexObject != null)
                HumanTexObject.SetActive(false);
        }
    }

    private static Item GetHumanItemByPoint(int x, int y)
    {
        for (int i = 15; i >= 0; i--)
        {
            if (HumanSprites[i] == null)
                continue;
            if (HumanSprites[i].GetPixel(x, y).a > 0.5f)
                return HumanItems[i];
        }

        return null;
    }

    public override bool ProcessEventPic(Event e, float mousex, float mousey)
    {
        // don't show any tooltips and obviously don't allow item dragging if its not our unit
        if (e.rawType == EventType.MouseMove && e.commandName == "tooltip")
        {
            if (MapLogic.Instance.ConsolePlayer != null &&
                (LogicHuman.Player.Diplomacy[MapLogic.Instance.ConsolePlayer.ID] & DiplomacyFlags.Vision) == 0) return false;

            Item item = GetHumanItemByPoint((int)mousex, (int)mousey);
            if (item != null)
                UiManager.Instance.SetTooltip(item.Class.VisualName);
            return true;
        }

        return false;
    }
}