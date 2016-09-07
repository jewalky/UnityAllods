using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewHuman : MapViewUnit, IUiItemAutoDropper
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
        for (int i = 0; i < (int)MapUnit.BodySlot.TopSlot; i++)
        {
            Item item = LogicHuman.GetItemFromBody((MapUnit.BodySlot)i);
            if (item == null)
                continue;

            int slot = item.Class.Option.Slot;
            if (slot == 0 || slot == 3 || slot == 11 || (slot >= 13 && slot <= 15))
                continue;

            ItemFile file1 = null;
            ItemFile file2 = null;

            int idx1 = -1;
            int idx2 = -1;

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
                        idx1 = 7;
                        idx2 = 10;
                        break;

                    case 10: // gauntlets
                        file2.UpdateSprite();
                        idx1 = 8;
                        idx2 = 11;
                        break;

                    case 4: // ring
                        file2.UpdateSprite();
                        idx1 = 2;
                        idx2 = 3;
                        break;

                    case 5: // amulet
                        idx1 = 6;
                        break;

                    case 6: // helm
                        idx1 = 12;
                        break;

                    case 7: // mail
                        idx1 = 4;
                        break;

                    case 8: // cuirass
                        idx1 = 9;
                        break;

                    case 12: // boots
                        idx1 = 5;
                        break;

                    case 1: // weapon
                        idx1 = 13;
                        break;

                    case 2: // shield
                        idx1 = 14;
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
                        idx1 = 2;
                        idx2 = 3;
                        break;

                    case 10: // gloves
                        file2.UpdateSprite();
                        idx1 = 5;
                        idx2 = 9;
                        break;

                    case 8: // cloak
                        file2.UpdateSprite();
                        idx1 = 0;
                        idx2 = 8;
                        break;

                    case 5: // amulet
                        idx1 = 7;
                        break;

                    case 6: // hat
                        idx1 = 10;
                        break;

                    case 7: // robe
                        idx1 = 6;
                        break;

                    case 12: // shoes
                        idx1 = 4;
                        break;

                    case 1: // staff
                        idx1 = 11;
                        break;
                }
            }

            if (idx1 >= 0)
            {
                HumanSprites[idx1] = file1.File;
                HumanItems[idx1] = item;
            }

            if (idx2 >= 0)
            {
                HumanSprites[idx2] = file2.File;
                HumanItems[idx2] = item;
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
            //Debug.LogFormat("up: {0} item {1}, dragitem = {2}", i, HumanItems[i], UiManager.Instance.DragItem);
            Color c = (HumanSprites[i] == null) ? new Color(0, 0, 0, 0) : new Color(1, 1, 1, (HumanItems[i] != null && HumanItems[i] == UiManager.Instance.DragItem) ? 0.5f : 1);
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
                UiManager.Instance.SetTooltip(item.ToVisualString());

            return true;
        }
        else if (e.rawType == EventType.MouseDown && e.commandName == "double")
        {
            if (LogicHuman.Player != MapLogic.Instance.ConsolePlayer)
                return false;

            Item item = GetHumanItemByPoint((int)mousex, (int)mousey);
            if (item != null)
            {
                SendItemMoveCommand(item, ServerCommands.ItemMoveLocation.UnitPack, LogicHuman.ItemsPack.Count);
                item = LogicHuman.TakeItemFromBody((MapUnit.BodySlot)item.Class.Option.Slot);
                LogicHuman.ItemsPack.PutItem(LogicHuman.ItemsPack.Count, item);
            }

            return true;
        }

        return false;
    }

    // drag-drop of items in infowindow
    public override bool ProcessStartDrag(float mousex, float mousey)
    {
        //Debug.LogFormat("tried to drag at {0}, {1}", mousex, mousey);
        // can't drag if we don't own this player
        if (LogicHuman.Player != MapLogic.Instance.ConsolePlayer)
            return false;
        Item item = GetHumanItemByPoint((int)mousex, (int)mousey);
        if (item == null)
            return false;
        //item = LogicHuman.TakeItemFromBody((MapUnit.BodySlot)item.Class.Option.Slot);
        LogicHuman.DoUpdateInfo = true;
        UiManager.Instance.StartDrag(item, 1, () =>
        {
            // put item back to body if cancelled
            //LogicHuman.PutItemToBody((MapUnit.BodySlot)item.Class.Option.Slot, item);
            LogicHuman.DoUpdateInfo = true;
        });
        return true;
    }

    public override bool ProcessDrag(Item item, float mousex, float mousey)
    {
        if (LogicHuman.Player != MapLogic.Instance.ConsolePlayer)
            return false;
        return true; // allow drag if this unit belongs to console player
    }

    private void SendItemMoveCommand(Item item, ServerCommands.ItemMoveLocation to = ServerCommands.ItemMoveLocation.UnitBody, int toIndex = -1)
    {
        // send command.
        // first off, determine move source.
        ServerCommands.ItemMoveLocation from;
        int fromIndex = -1;

        if (item.Parent == LogicHuman.ItemsBody)
        {
            from = ServerCommands.ItemMoveLocation.UnitBody;
            fromIndex = item.Class.Option.Slot;
        }
        else if (item.Parent == LogicHuman.ItemsPack)
        {
            from = ServerCommands.ItemMoveLocation.UnitPack;
            fromIndex = item.Index;
        }
        else from = ServerCommands.ItemMoveLocation.Ground;

        if (toIndex < 0) toIndex = item.Class.Option.Slot;

        Client.SendItemMove(from, to, fromIndex, toIndex, item.Count, LogicHuman, MapView.Instance.MouseCellX, MapView.Instance.MouseCellY);
    }

    public override bool ProcessDrop(Item item, float mousex, float mousey)
    {
        if (LogicHuman.Player != MapLogic.Instance.ConsolePlayer)
            return false;
        if (!LogicHuman.IsItemUsable(item))
            return false;
        SendItemMoveCommand(item);
        // put item to body
        LogicHuman.PutItemToBody((MapUnit.BodySlot)item.Class.Option.Slot, item);
        return true;
    }

    public override void ProcessEndDrag()
    {
        LogicHuman.DoUpdateInfo = true;
    }

    public override void ProcessFailDrag()
    {
        
    }

    public override Item ProcessVerifyEndDrag()
    {
        // take item from body
        return LogicHuman.TakeItemFromBody((MapUnit.BodySlot)UiManager.Instance.DragItem.Class.Option.Slot);
    }

    public bool ProcessAutoDrop(Item item)
    {
        if (LogicHuman.Player != MapLogic.Instance.ConsolePlayer)
            return false;
        if (!LogicHuman.IsItemUsable(item))
            return false;
        SendItemMoveCommand(item);
        // put item to body
        LogicHuman.PutItemToBody((MapUnit.BodySlot)item.Class.Option.Slot, new Item(item, 1));
        return true;
    }
}