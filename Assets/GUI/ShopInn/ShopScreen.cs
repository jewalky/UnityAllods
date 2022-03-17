using System;
using System.Collections.Generic;
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

    public override void OnStart()
    {

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
        PositionObject(o_ShopFrame, o_ShopBaseOffset, new Vector3(164, 0, 0));

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
        PositionObject(o_UnitInventory.gameObject, o_ShopBaseOffset, new Vector3(0, 390, 0));

        // generate ItemView for items
        o_ShelfItems = Utils.CreateObjectWithScript<ItemView>();
        o_ShelfItems.InvWidth = 2;
        o_ShelfItems.InvHeight = 3;
        o_ShelfItems.Pack = ((ShopStructure)Shop.Logic).Shelves[0].Items;
        PositionObject(o_ShelfItems.gameObject, o_ShopBaseOffset, new Vector3(1, 32, 0));

    }

    private void PositionObject(GameObject obj, GameObject parent, Vector3 location)
    {
        obj.transform.parent = parent.transform;
        obj.transform.localScale = new Vector3(1, 1, 1);
        obj.transform.localPosition = location;
    }

    public override bool ProcessEvent(Event e)
    {
        if (e.type == EventType.KeyDown)
        {
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    Client.SendLeaveStructure();
                    break;
            }
        }
        else if (e.rawType == EventType.MouseMove)
        {
            MouseCursor.SetCursor(MouseCursor.CurDefault);
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
        
    }

}

