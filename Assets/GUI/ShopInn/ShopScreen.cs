using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class ShopScreen : FullscreenView
{

    public MapStructure Shop;
    public MapUnit Unit;

    private enum ShopType
    {
        Plagat = 0,
        Kaarg = 1,
        Druid = 2
    }

    private ShopType Type = ShopType.Plagat;
    private int TypeInt = 0;

    // shared pictures
    private static Texture2D[] shop_ShopMenu = { null, null, null };
    private static Texture2D[] shop_ShopTable = { null, null, null };
    private static Texture2D[] shop_ShopInv = { null, null, null };
    private static Texture2D[] shop_ShopFrame = { null, null, null };
    private static Texture2D[][] shop_ShopButton = { null, null, null };
    private static Texture2D[][] shop_ShopArrow = { null, null, null };
    private static Texture2D[] shop_ShopMain = { null, null, null };
    // for plagat shop
    private static Texture2D[,] shop_PlagatShelves = null;
    // for kaarg shop
    private static Texture2D[] shop_KaargShelves = null;
    private static Texture2D[,,] shop_KaargFire = null;
    // for druid shop
    private static Texture2D[] shop_DruidShelves = null;
    private static Texture2D shop_DruidElven = null;

    // objects
    private GameObject o_ShopBaseOffset;
    private GameObject o_ShopMain;
    private GameObject o_ShopTable;
    private GameObject o_ShopButtonsBg;
    private GameObject o_ShopInventory;
    private GameObject o_ShopFrame;
    private MapViewInfowindow o_UnitView;
    private MapViewInventory o_UnitInventory;
    private ItemView o_ShelfItems;
    private ItemView o_TableItems;
    private AllodsTextRenderer[] o_ButtonTextRenderers = new AllodsTextRenderer[4];
    private GameObject[] o_ButtonTextObjects = new GameObject[4];
    private GameObject[] o_ButtonObjects = new GameObject[4];
    private GameObject[] o_ArrowObjects = new GameObject[2];
    private MeshRenderer[] o_ArrowRenderers = new MeshRenderer[2];

    private static Rect[] ButtonPositions =
        new Rect[]
        {
            new Rect(502, 20, 105, 42),
            new Rect(488, 68, 128, 41),
            new Rect(488, 118, 128, 41),
            new Rect(502, 166, 105, 42)
        };
    private static Vector2[] ButtonTextPositions =
        new Vector2[]
        {
            new Vector2(488, 27),
            new Vector2(488, 75),
            new Vector2(488, 124),
            new Vector2(488, 172)
        };
    private static Vector2[] ButtonPressedPositions =
        new Vector2[]
        {
            new Vector2(494, 15),
            new Vector2(483, 67),
            new Vector2(483, 114),
            new Vector2(494, 160)
        };

    //
    private int HoveredButton = -1;
    private int ClickedButton = -1;

    //
    private int HoveredArrow = -1;
    private int ClickedArrow = -1;
    private Stopwatch LastScroll = null;

    //
    private int CurrentShelf = 0;

    private string GetGraphicsPrefix(string filename)
    {
        switch (Type)
        {
            default:
            case ShopType.Plagat:
                return string.Format("graphics/interface/" + filename);
            case ShopType.Kaarg:
                return string.Format("graphics/interface/shop_kaarg/" + filename);
            case ShopType.Druid:
                return string.Format("graphics/interface/shop_druid/" + filename);
        }
    }

    private void SendItemMoveCommand(Item item, ServerCommands.ItemMoveLocation to, int toIndex)
    {
        // send command.
        // first off, determine move source.
        ServerCommands.ItemMoveLocation from;
        int fromIndex = -1;

        if (item.Parent == Unit.ItemsBody)
        {
            from = ServerCommands.ItemMoveLocation.UnitBody;
            fromIndex = item.Class.Option.Slot;
        }
        else if (item.Parent == Unit.ItemsPack)
        {
            from = ServerCommands.ItemMoveLocation.UnitPack;
            fromIndex = item.Index;
        }
        else if (item.Parent.LocationHint != ServerCommands.ItemMoveLocation.Undefined)
        {
            from = item.Parent.LocationHint;
            fromIndex = item.Index;
        }
        else from = ServerCommands.ItemMoveLocation.Ground;

        Client.SendItemMove(from, to, fromIndex, toIndex, item.Count, Unit, MapView.Instance.MouseCellX, MapView.Instance.MouseCellY);
    }

    public override void OnStart()
    {
        LastScroll = new Stopwatch();

        // detect visual shop type to display
        ShopType type = ShopType.Plagat;
        if (Shop != null && Shop.Class != null)
        {
            int id = Shop.Class.ID;
            if (id >= 93 && id <= 95)
                type = ShopType.Kaarg;
            else if (id >= 105 && id <= 107)
                type = ShopType.Druid;
        }
        Type = type;
        TypeInt = (int)type;

        // load images
        if (shop_ShopMenu[TypeInt] == null)
            shop_ShopMenu[TypeInt] = Images.LoadImage(GetGraphicsPrefix("shopmenu.bmp"), 0, Images.ImageType.AllodsBMP);
        if (shop_ShopMain[TypeInt] == null)
        {
            switch (Type)
            {
                default:
                case ShopType.Plagat:
                    shop_ShopMain[TypeInt] = Images.LoadImage("graphics/interface/shopanim/shopmain.bmp", 0, Images.ImageType.AllodsBMP);
                    break;
                case ShopType.Kaarg:
                case ShopType.Druid:
                    shop_ShopMain[TypeInt] = Images.LoadImage(GetGraphicsPrefix("shopmain.bmp"), 0, Images.ImageType.AllodsBMP);
                    break;
            }
        }
        if (shop_ShopTable[TypeInt] == null)
            shop_ShopTable[TypeInt] = Images.LoadImage(GetGraphicsPrefix("shoptable.bmp"), 0, Images.ImageType.AllodsBMP);
        if (shop_ShopInv[TypeInt] == null)
            shop_ShopInv[TypeInt] = Images.LoadImage(GetGraphicsPrefix("shopinv.bmp"), 0, Images.ImageType.AllodsBMP);
        if (shop_ShopFrame[TypeInt] == null)
            shop_ShopFrame[TypeInt] = Images.LoadImage(GetGraphicsPrefix("shopframe.bmp"), 0, Images.ImageType.AllodsBMP);
        if (shop_ShopButton[TypeInt] == null)
        {
            shop_ShopButton[TypeInt] = new Texture2D[4];
            for (int i = 0; i < 4; i++)
                shop_ShopButton[TypeInt][i] = Images.LoadImage(GetGraphicsPrefix(string.Format("shopbutton{0}.bmp", i+1)), 0, Images.ImageType.AllodsBMP);
        }
        if (shop_ShopArrow[TypeInt] == null)
        {
            shop_ShopArrow[TypeInt] = new Texture2D[4];
            for (int i = 0; i < 4; i++)
                shop_ShopArrow[TypeInt][i] = Images.LoadImage(GetGraphicsPrefix(string.Format("shoparrow{0}.bmp", i + 1)), 0, Images.ImageType.AllodsBMP);
        }

        if (Type == ShopType.Plagat)
        {
            if (shop_PlagatShelves == null)
            {
                shop_PlagatShelves = new Texture2D[4, 11];
                for (int i = 0; i < 4; i++)
                    for (int j = 0; j < 11; j++)
                        shop_PlagatShelves[i, j] = Images.LoadImage(string.Format("graphics/interface/shopanim/{0:D2}/{1}.bmp", i+1, j+1), 0, Images.ImageType.AllodsBMP);
            }
        }
        else if (Type == ShopType.Kaarg)
        {
            shop_KaargShelves = new Texture2D[4];
            shop_KaargShelves[0] = Images.LoadImage("graphics/interface/shop_kaarg/hili_armor.bmp", 0, Images.ImageType.AllodsBMP);
            shop_KaargShelves[1] = Images.LoadImage("graphics/interface/shop_kaarg/hili_magic.bmp", 0, Images.ImageType.AllodsBMP);
            shop_KaargShelves[2] = Images.LoadImage("graphics/interface/shop_kaarg/hili_potion.bmp", 0, Images.ImageType.AllodsBMP);
            shop_KaargShelves[3] = Images.LoadImage("graphics/interface/shop_kaarg/hili_weapon.bmp", 0, Images.ImageType.AllodsBMP);
            // 0-dark
            // 1-select
            // ---
            // 0-burn
            // 1-cycle
            // 2-lite
            // ---
            // 0-6-frames
            shop_KaargFire = new Texture2D[2, 3, 6];
            string[] t1 = { "dark", "select" };
            string[] t2 = { "burn", "cicle", "lite" };
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 6; k++)
                        shop_KaargFire[i, j, k] = Images.LoadImage(string.Format("graphics/interface/shop_kaarg/fire/{0}/{1}{2}.bmp", t1[i], t2[j], k + 1), 0, Images.ImageType.AllodsBMP);
        }
        else if (Type == ShopType.Druid)
        {
            shop_DruidShelves = new Texture2D[4];
            shop_DruidShelves[0] = Images.LoadImage("graphics/interface/shop_druid/hili_armor.bmp", 0, Images.ImageType.AllodsBMP);
            shop_DruidShelves[1] = Images.LoadImage("graphics/interface/shop_druid/hili_magic.bmp", 0, Images.ImageType.AllodsBMP);
            shop_DruidShelves[2] = Images.LoadImage("graphics/interface/shop_druid/hili_potion.bmp", 0, Images.ImageType.AllodsBMP);
            shop_DruidShelves[3] = Images.LoadImage("graphics/interface/shop_druid/hili_elven.bmp", 0, Images.ImageType.AllodsBMP);
            shop_DruidElven = Images.LoadImage("graphics/interface/shop_druid/elven.bmp", 0, Images.ImageType.AllodsBMP);
        }

        // generate generic view objects
        o_ShopBaseOffset = Utils.CreateObject();
        PositionObject(o_ShopBaseOffset, gameObject, new Vector3(MainCamera.Width / 2 - 320, MainCamera.Height / 2 - 240, 0));

        Utils.MakeTexturedQuad(out o_ShopInventory, shop_ShopInv[TypeInt]);
        PositionObject(o_ShopInventory, o_ShopBaseOffset, new Vector3(0, 0, 0));

        Utils.MakeTexturedQuad(out o_ShopTable, shop_ShopTable[TypeInt]);
        PositionObject(o_ShopTable, o_ShopBaseOffset, new Vector3(0, 303, 0));

        Utils.MakeTexturedQuad(out o_ShopFrame, shop_ShopFrame[TypeInt]);
        PositionObject(o_ShopFrame, o_ShopBaseOffset, new Vector3(164, 0, -1));

        Utils.MakeTexturedQuad(out o_ShopButtonsBg, shop_ShopMenu[TypeInt]);
        PositionObject(o_ShopButtonsBg, o_ShopBaseOffset, new Vector3(464, 0, 0));

        Utils.MakeTexturedQuad(out o_ShopMain, shop_ShopMain[TypeInt]);
        PositionObject(o_ShopMain, o_ShopBaseOffset, new Vector3(169, 8, 0));

        // generate Infowindow and Inventory for the unit that entered shop.
        o_UnitView = Utils.CreateObjectWithScript<MapViewInfowindow>();
        o_UnitView.ForceSmall = true;
        o_UnitView.BookAvailable = false;
        o_UnitView.PackAvailable = false;
        PositionObject(o_UnitView.gameObject, o_ShopBaseOffset, new Vector3(464, 238, 0));
        Unit.CheckAllocateObject();
        o_UnitView.Viewer = (IMapViewSelfie)Unit.GameScript;

        o_UnitInventory = Utils.CreateObjectWithScript<MapViewInventory>();
        o_UnitInventory.SetPack((MapHuman)Unit);
        o_UnitInventory.ItemCount = 5;
        o_UnitInventory.View.ShopMode = true;
        o_UnitInventory.View.ShopViewer = Unit;
        PositionObject(o_UnitInventory.gameObject, o_ShopBaseOffset, new Vector3(0, 390, 0));

        // generate ItemView for items
        o_ShelfItems = Utils.CreateObjectWithScript<ItemView>();
        o_ShelfItems.InvWidth = 2;
        o_ShelfItems.InvHeight = 3;
        o_ShelfItems.ShopMode = true;
        o_ShelfItems.ShopViewer = Unit;
        o_ShelfItems.Pack = ((ShopStructure)Shop.Logic).Shelves[CurrentShelf].Items;
        PositionObject(o_ShelfItems.gameObject, o_ShopBaseOffset, new Vector3(1, 32, -1));

        o_ShelfItems.OnProcessDrop = (Item item, int index) =>
        {
            SendItemMoveCommand(item, o_ShelfItems.Pack.LocationHint, index);
            o_ShelfItems.Pack.PutItem(index, item);
            return true;
        };

        //
        o_TableItems = Utils.CreateObjectWithScript<ItemView>();
        o_TableItems.InvWidth = 5;
        o_TableItems.InvHeight = 1;
        o_TableItems.ShopMode = true;
        o_TableItems.ShopViewer = Unit;
        o_TableItems.Pack = ((ShopStructure)Shop.Logic).GetTableFor(Unit.Player);
        PositionObject(o_TableItems.gameObject, o_ShopBaseOffset, new Vector3(32, 305, -1));

        o_TableItems.OnProcessDrop = (Item item, int index) =>
        {
            SendItemMoveCommand(item, ServerCommands.ItemMoveLocation.ShopTable, index);
            o_TableItems.Pack.PutItem(index, item);
            return true;
        };

        // generate button texts
        for (int i = 0; i < 4; i++)
        {
            o_ButtonTextRenderers[i] = new AllodsTextRenderer(Fonts.Font4, Font.Align.Center, 128, Fonts.Font4.LineHeight * 2, false);
            o_ButtonTextRenderers[i].Material.color = new Color32(0xBD, 0x9E, 0x4A, 0xFF);
            o_ButtonTextObjects[i] = o_ButtonTextRenderers[i].GetNewGameObject();
            
            PositionObject(o_ButtonTextObjects[i], o_ShopBaseOffset, new Vector3(ButtonTextPositions[i].x, ButtonTextPositions[i].y, -2), 100);
        }

        // generate button images (pressed)
        for (int i = 0; i < 4; i++)
        {
            Utils.MakeTexturedQuad(out o_ButtonObjects[i], shop_ShopButton[TypeInt][i]);
            PositionObject(o_ButtonObjects[i], o_ShopBaseOffset, new Vector3(ButtonPressedPositions[i].x, ButtonPressedPositions[i].y, -1));
            o_ButtonObjects[i].SetActive(false);
        }

        for (int i = 0; i < 2; i++)
        {
            Utils.MakeTexturedQuad(out o_ArrowObjects[i], shop_ShopArrow[TypeInt][i]);
            PositionObject(o_ArrowObjects[i], o_ShopBaseOffset, i == 0 ? new Vector3(46, 0, -1) : new Vector3(46, 271, -1));
            o_ArrowObjects[i].SetActive(false);
            o_ArrowRenderers[i] = o_ArrowObjects[i].GetComponent<MeshRenderer>();
        }
    }

    private void PositionObject(GameObject obj, GameObject parent, Vector3 location, float scale = 1)
    {
        obj.transform.parent = parent.transform;
        obj.transform.localScale = new Vector3(scale, scale, scale);
        obj.transform.localPosition = location;
    }

    public override bool ProcessEvent(Event e)
    {
        if (e.type == EventType.KeyDown)
        {
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    ProcessQuit();
                    break;
            }
        }
        else if (e.rawType == EventType.MouseMove)
        {
            MouseCursor.SetCursor(MouseCursor.CurDefault);

            // check if its inside this widget
            Vector2 mPos = Utils.GetMousePosition();
            if (!new Rect(o_ShopBaseOffset.transform.position.x, o_ShopBaseOffset.transform.position.y, 640, 480).Contains(mPos))
                return true;

            Vector2 mPosLocal = new Vector2(mPos.x - o_ShopBaseOffset.transform.position.x,
                                            mPos.y - o_ShopBaseOffset.transform.position.y);

            // check if inside buttons
            for (int i = 0; i < ButtonPositions.Length; i++)
            {
                Material mat = o_ButtonTextRenderers[i].Material;
                if (ButtonPositions[i].Contains(mPosLocal))
                {
                    HoveredButton = i;
                    mat.color = new Color32(0xFF, 0xFF, 0x73, 0xFF);
                    break;
                }
                else
                {
                    mat.color = new Color32(0xBD, 0x9E, 0x4A, 0xFF);
                }
            }

            // check if inside arrows
            HoveredArrow = -1;
            if (new Rect(o_ShopBaseOffset.transform.position.x, o_ShopBaseOffset.transform.position.y, 164, 31).Contains(mPos))
                HoveredArrow = 0;
            if (new Rect(o_ShopBaseOffset.transform.position.x, o_ShopBaseOffset.transform.position.y + 272, 164, 31).Contains(mPos))
                HoveredArrow = 1;

            return true;
        }
        else if (e.rawType == EventType.MouseDown && e.button == 0)
        {
            ClickedButton = HoveredButton;
            ClickedArrow = HoveredArrow;
            return true;
        }
        else if (e.rawType == EventType.MouseUp && e.button == 0)
        {
            if (ClickedButton == HoveredButton && ClickedButton >= 0)
            {
                switch (ClickedButton)
                {
                    case 0:
                        ProcessCancel();
                        break;
                    case 1:
                        ProcessBuy();
                        break;
                    case 2:
                        ProcessSell();
                        break;
                    case 3:
                        ProcessQuit();
                        break;
                }
            }
            ClickedButton = -1;
            ClickedArrow = -1;
            return true;
        }

        return base.ProcessEvent(e);
    }

    public override bool ProcessCustomEvent(CustomEvent ce)
    {
        return base.ProcessCustomEvent(ce);
    }

    public void Update()
    {
        long buyDelta = 0, sellDelta = 0;
        foreach (Item item in ((ShopStructure)Shop.Logic).GetTableFor(Unit.Player))
        {
            switch (item.NetParent)
            {
                case ServerCommands.ItemMoveLocation.UnitBody:
                case ServerCommands.ItemMoveLocation.UnitPack:
                    long price = item.Price / 2;
                    if (price < 1) price = 1;
                    price *= item.Count;
                    sellDelta += price;
                    break;

                default:
                    buyDelta -= item.Price * item.Count;
                    break;
            }
        }
        o_ButtonTextRenderers[0].Text = string.Format("{0}\n{1}", Locale.Main[72], ItemView.FormatMoney(Unit.Player.Money));
        o_ButtonTextRenderers[1].Text = string.Format("{0}\n{1}", Locale.Main[70], ItemView.FormatMoney(buyDelta));
        o_ButtonTextRenderers[2].Text = string.Format("{0}\n{1}", Locale.Main[71], ItemView.FormatMoney(sellDelta));
        o_ButtonTextRenderers[3].Text = string.Format("{0}\n{1}", Locale.Main[73], ItemView.FormatMoney(Unit.Player.Money + buyDelta + sellDelta));
        for (int i = 0; i < 4; i++)
        {
            if (ClickedButton == i)
            {
                PositionObject(o_ButtonTextObjects[i], o_ShopBaseOffset, new Vector3(ButtonTextPositions[i].x, ButtonTextPositions[i].y + 2, -2), 100);
                o_ButtonObjects[i].SetActive(true);
            }
            else
            {
                PositionObject(o_ButtonTextObjects[i], o_ShopBaseOffset, new Vector3(ButtonTextPositions[i].x, ButtonTextPositions[i].y, -2), 100);
                o_ButtonObjects[i].SetActive(false);
            }
        }
        for (int i = 0; i < 2; i++)
        {
            o_ArrowObjects[i].SetActive(HoveredArrow == i);
            o_ArrowRenderers[i].material.mainTexture = ClickedArrow == i ? shop_ShopArrow[TypeInt][2+i] : shop_ShopArrow[TypeInt][i];
        }

        if (ClickedArrow != -1)
        {
            if (!LastScroll.IsRunning || LastScroll.ElapsedMilliseconds > 100)
            {
                if (ClickedArrow == 0) // up arrow
                {
                    if (o_ShelfItems.Scroll > 0)
                        o_ShelfItems.Scroll = Math.Max(0, o_ShelfItems.Scroll - 2);
                }
                else if (ClickedArrow == 1) // down arrow
                {
                    if (o_ShelfItems.Scroll < o_ShelfItems.GetVisualPackCount() - o_ShelfItems.InvWidth * o_ShelfItems.InvHeight)
                        o_ShelfItems.Scroll = Math.Min(o_ShelfItems.Scroll + 2, o_ShelfItems.GetVisualPackCount() - o_ShelfItems.InvWidth * o_ShelfItems.InvHeight);
                }
                LastScroll.Restart();
            }
        }
    }

    private void ProcessCancel()
    {
        ((ShopStructure)Shop.Logic).CancelTransaction(Unit);
    }

    private void ProcessBuy()
    {
        ((ShopStructure)Shop.Logic).ApplyBuy(Unit);
    }

    private void ProcessSell()
    {
        ((ShopStructure)Shop.Logic).ApplySell(Unit);
    }

    private void ProcessQuit()
    {
        Client.SendLeaveStructure();
    }

}

