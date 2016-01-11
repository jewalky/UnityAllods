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
    private static Dictionary<int, Images.AllodsSprite> _HumanPictures = new Dictionary<int, Images.AllodsSprite>();
    public Images.AllodsSprite UpdateHumanPic()
    {
        int dictKey = ((int)LogicHuman.Gender << 8) + LogicHuman.Face;
        if (_HumanPictures.ContainsKey(dictKey))
            return _HumanPictures[dictKey];

        string subdir;
        if ((LogicHuman.Gender & MapHuman.HGender.Male) != 0)
            subdir = "m";
        else if ((LogicHuman.Gender & MapHuman.HGender.Female) != 0)
            subdir = "f";
        else return null;
        if ((LogicHuman.Gender & MapHuman.HGender.Fighter) != 0)
            subdir += "fighter";
        else if ((LogicHuman.Gender & MapHuman.HGender.Mage) != 0)
            subdir += "mage";
        else return null;

        Images.AllodsSprite sprite = Images.Load256(string.Format("graphics/equipment/{0}/{1}.256", subdir, LogicHuman.Face));
        _HumanPictures[dictKey] = sprite;
        return sprite;
    }

    // special handling for displaypic
    private static GameObject HumanTexObject;
    private static MeshRenderer HumanTexRenderer;
    private static Material HumanTexMaterial;

    public override void DisplayPic(bool on, Transform parent)
    {
        base.DisplayPic(false, parent); // disable monster pic
        if (on)
        {
            // load image
            Images.AllodsSprite pic = UpdateHumanPic();
            // init infowindow
            if (HumanTexMaterial == null)
                HumanTexMaterial = new Material(MainCamera.MainShaderPaletted);
            if (HumanTexObject == null)
            {
                Utils.MakeTexturedQuad(out HumanTexObject, pic.Atlas, pic.AtlasRects[0]);
                HumanTexRenderer = HumanTexObject.GetComponent<MeshRenderer>();
                HumanTexRenderer.enabled = true;
                HumanTexObject.name = "MapViewHuman$InfoPic";
            }

            HumanTexObject.SetActive(true);

            HumanTexRenderer.transform.parent = parent;
            // load infowindow texture.
            HumanTexRenderer.material = HumanTexMaterial;
            HumanTexRenderer.material.mainTexture = pic.Atlas;
            HumanTexRenderer.material.SetTexture("_Palette", pic.OwnPalette);
            HumanTexRenderer.transform.localPosition = new Vector3(0,
                                                                   2, -0.01f);
        }
        else
        {
            if (HumanTexObject != null)
                HumanTexObject.SetActive(false);
        }
    }
}