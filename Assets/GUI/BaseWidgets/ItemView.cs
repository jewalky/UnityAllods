using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class ItemView : Widget, IUiEventProcessor, IUiItemDragger, IUiItemAutoDropper
{
    public delegate bool ProcessDropDelegate(Item item, int position);

    private ItemPack _Pack = null;
    public ItemPack Pack
    {
        get
        {
            return _Pack;
        }

        set
        {
            _Pack = value;
            ResetFromPack();
        }
    }

    public IUiItemAutoDropper AutoDropTarget = null;

    private void ResetFromPack()
    {
        // todo: reset scrolling and possibly other values
        Scroll = 0;
    }

    private class MGlowPart
    {
        public float x;
        public float y;
        public float state;
        public float delta;
    }

    private List<MGlowPart> MGlowParts = new List<MGlowPart>();

    // magic item glow animation
    private float MGlowTimer = 0.05f;
    private void UpdateMGlow()
    {
        // animate.
        MGlowTimer += Time.unscaledDeltaTime;
        if (MGlowTimer < 0.05f)
            return;

        if (UnityEngine.Random.Range(0, 128) < 64) // 1/2 chance every 50ms
        {
            float rx = UnityEngine.Random.Range(10f, 70f) * InvScale;
            float ry = UnityEngine.Random.Range(10f, 70f) * InvScale;
            MGlowPart newPart = new MGlowPart();
            newPart.x = rx;
            newPart.y = ry;
            newPart.state = 0;
            newPart.delta = 1;
            MGlowParts.Add(newPart);
        }

        while (MGlowTimer >= 0.025f)
        {
            for (int i = 0; i < MGlowParts.Count; i++)
            {
                MGlowPart part = MGlowParts[i];
                if (part.delta > 0)
                {
                    part.state += 0.1f;
                    if (part.state > 1)
                        part.delta = -1;
                }
                else
                {
                    part.state -= 0.1f;
                    if (part.state < 0)
                    {
                        MGlowParts.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
            }

            MGlowTimer -= 0.025f;
        }
    }

    private Utils.MeshBuilder Builder = new Utils.MeshBuilder();
    private static Texture2D img_BackInv; // own inventory
    private static Texture2D img_BackInvB; // can buy
    private static Texture2D img_BackInvG; // no item
    private static Texture2D img_BackInvS; // can't buy

    // shop price images
    private static Texture2D[] img_Prices;
    private static int[] img_PriceSizes;

    private MeshRenderer Renderer;
    private MeshFilter Filter;

    private int _InvWidth = 0;
    private int _InvHeight = 0;

    public int Scroll = 0;

    public int InvWidth
    {
        get { return _InvWidth; }
        set { if (_InvWidth == 0) _InvWidth = value; }
    }

    public int InvHeight
    {
        get { return _InvHeight; }
        set { if (_InvHeight == 0) _InvHeight = value; }
    }

    public float InvScale = 1f;

    public bool ShowMoney = false;
    private bool _ShopMode = false;
    public int ShopLockedMoney = 0;
    public MapUnit ShopViewer = null;

    public bool ShopMode
    {
        get { return _ShopMode && ShopViewer != null; }
        set { _ShopMode = value; }
    }

    public ProcessDropDelegate OnProcessDrop = null;

    private List<GameObject> TextObjects = new List<GameObject>();
    private List<AllodsTextRenderer> TextRenderers = new List<AllodsTextRenderer>();

    private List<GameObject> ShopTextObjects = new List<GameObject>();
    private List<AllodsTextRenderer> ShopTextRenderers = new List<AllodsTextRenderer>();

    public void OnDestroy()
    {
        UiManager.Instance.Unsubscribe(this);
    }

    public void Start()
    {
        UiManager.Instance.Subscribe(this);

        Width = (int)(InvWidth * 80 * InvScale);
        Height = (int)(InvHeight * 80 * InvScale);

        Renderer = gameObject.AddComponent<MeshRenderer>();
        Filter = gameObject.AddComponent<MeshFilter>();

        // load necessary images
        if (img_BackInv == null) img_BackInv = Images.LoadImage("graphics/interface/backinv.bmp", Images.ImageType.AllodsBMP);
        if (img_BackInvB == null) img_BackInvB = Images.LoadImage("graphics/interface/backinvb.bmp", Images.ImageType.AllodsBMP);
        if (img_BackInvG == null) img_BackInvG = Images.LoadImage("graphics/interface/backinvg.bmp", Images.ImageType.AllodsBMP);
        if (img_BackInvS == null) img_BackInvS = Images.LoadImage("graphics/interface/backinvs.bmp", Images.ImageType.AllodsBMP);

        if (img_Prices == null)
        {
            img_Prices = new Texture2D[7];
            for (int i = 0; i < 7; i++)
                img_Prices[i] = Images.LoadImage(string.Format("graphics/interface/costs{0}.bmp", i+1), 0, Images.ImageType.AllodsBMP);
            img_PriceSizes = new int[] { 15, 21, 27, 36, 42, 48, 57 };
        }

        // create InvWidth*InvHeight*2+1 materials
        List<Material> materials = new List<Material>();
        for (int i = 0; i < InvWidth * InvHeight; i++)
            materials.Add(new Material(MainCamera.MainShader));
        for (int i = 0; i < InvWidth * InvHeight; i++)
            materials.Add(new Material(MainCamera.MainShaderPaletted));
        materials.Add(new Material(MainCamera.MainShader));
        for (int i = 0; i < InvWidth * InvHeight; i++)
            materials.Add(new Material(MainCamera.MainShader));
        Renderer.materials = materials.ToArray();
    }

    private static string FormatMoney(long money)
    {
        string s = "";
        string ins = money.ToString();
        for (int j = 0; j < ins.Length; j++)
        {
            int offpos = ins.Length - j + ((money < 0) ? 1 : 0);
            if (offpos % 3 == 0 && j > 0)
                s += ",";
            s += ins[j];
        }
        return s;
    }

    public void Update()
    {
        // update text
        if (TextObjects.Count != InvWidth * InvHeight ||
            TextRenderers.Count != InvWidth * InvHeight)
        {
            for (int i = 0; i < TextRenderers.Count; i++)
                TextRenderers[i].DestroyImmediate();
            TextObjects.Clear();
            TextRenderers.Clear();

            for (int ly = 0; ly < InvHeight; ly++)
            {
                for (int lx = 0; lx < InvWidth; lx++)
                {
                    AllodsTextRenderer atr = new AllodsTextRenderer(Fonts.Font2);
                    atr.Text = "";
                    atr.Material.color = new Color32(0xBD, 0x9E, 0x4A, 0xFF);
                    GameObject go = atr.GetNewGameObject(0.01f, transform, 100, 0.01f);
                    TextObjects.Add(go);
                    TextRenderers.Add(atr);

                    go.transform.localPosition = new Vector3((lx * 80 + 6) * InvScale, (ly * 80 + 80 - 4) * InvScale - atr.Font.LineHeight, -0.2f);
                }
            }
        }

        // update text
        if (ShopTextObjects.Count != InvWidth * InvHeight ||
            ShopTextRenderers.Count != InvWidth * InvHeight)
        {
            for (int i = 0; i < ShopTextRenderers.Count; i++)
                ShopTextRenderers[i].DestroyImmediate();
            ShopTextObjects.Clear();
            ShopTextRenderers.Clear();

            for (int ly = 0; ly < InvHeight; ly++)
            {
                for (int lx = 0; lx < InvWidth; lx++)
                {
                    AllodsTextRenderer atr = new AllodsTextRenderer(Fonts.Font2);
                    atr.Text = "";
                    atr.Material.color = new Color32(0xBD, 0x9E, 0x4A, 0xFF);
                    atr.Align = Font.Align.Right;
                    GameObject go = atr.GetNewGameObject(0.01f, transform, 100, 0.01f);
                    ShopTextObjects.Add(go);
                    ShopTextRenderers.Add(atr);

                    go.transform.localPosition = new Vector3((lx * 80 + 80 - 6) * InvScale, (ly * 80) * InvScale, -0.2f);
                }
            }
        }

        // first submesh = quads, item background
        // second submesh = quads, item pictures
        // third submesh = lines, item magic glow
        // fourth submesh = quads, price backgrounds. only if in shop mode

        if (Pack == null)
        {
            Filter.mesh.Clear();
            return;
        }

        Builder.Reset();

        int start = Math.Max(Math.Min(Scroll, GetVisualPackCount() - InvWidth * InvHeight), 0);
        int end = Math.Min(start + InvWidth * InvHeight, GetVisualPackCount());
        int x = 0;
        int y = 0;
        for (int i = start; i < end; i++)
        {
            Item item = (i == Pack.Count && ShowMoney) ? GetVisualMoneyItem() : Pack[i];

            Builder.AddQuad(y * InvWidth + x, (x * 80) * InvScale, (y * 80) * InvScale, 80 * InvScale, 80 * InvScale, new Rect(0, 0, 1, 1));
            Texture2D bg;
            if (item != null)
            {
                if (ShopMode)
                {
                    switch (item.NetParent)
                    {
                        case ServerCommands.ItemMoveLocation.UnitBody:
                        case ServerCommands.ItemMoveLocation.UnitPack:
                            if (!ShopViewer.IsItemUsable(item))
                                bg = img_BackInvG;
                            else bg = img_BackInv;
                            break;

                        default:
                            if (!ShopViewer.IsItemUsable(item) || item.Price > ShopViewer.Player.Money - ShopLockedMoney)
                                bg = img_BackInvG;
                            else bg = img_BackInvS;
                            break;
                    }
                }
                else bg = img_BackInv;
            }
            else bg = img_BackInvG;

            Renderer.materials[y * InvWidth + x].mainTexture = bg;

            x++;
            if (x >= InvWidth)
            {
                x = 0;
                y++;
            }
        }

        for (int i = 0; i < TextObjects.Count; i++)
        {
            TextObjects[i].SetActive(false);
            ShopTextObjects[i].SetActive(false);
        }

        // now add magic glow where it should be
        Builder.CurrentZ = -0.1f;
        x = 0;
        y = 0;
        UpdateMGlow(); // per-widget unique animation is used.
        int rnd = 0;
        for (int i = start; i < end; i++)
        {
            // check if item has special effects
            Item item = (i == Pack.Count && ShowMoney) ? GetVisualMoneyItem() : Pack[i];

            if (item != null)
            {
                if (item.MagicEffects.Count > 0)
                {
                    float baseX = x * 80 * InvScale;
                    float baseY = y * 80 * InvScale;
                    Builder.CurrentMesh = InvWidth * InvHeight * 2;

                    foreach (MGlowPart part in MGlowParts)
                    {
                        // draw all glow parts
                        // left
                        Builder.CurrentPosition = new Vector3(baseX + part.x, baseY + part.y);
                        Builder.CurrentColor = new Color32(208, 0, 208, 255);
                        Builder.NextVertex();
                        Builder.CurrentPosition = new Vector3(baseX + part.x - 2f * part.state, baseY + part.y);
                        Builder.CurrentColor = new Color32(64, 0, 64, 255);
                        Builder.NextVertex();
                        // right
                        Builder.CurrentPosition = new Vector3(baseX + part.x, baseY + part.y);
                        Builder.CurrentColor = new Color32(208, 0, 208, 255);
                        Builder.NextVertex();
                        Builder.CurrentPosition = new Vector3(baseX + part.x + 2f * part.state, baseY + part.y);
                        Builder.CurrentColor = new Color32(64, 0, 64, 255);
                        Builder.NextVertex();
                        // top
                        Builder.CurrentPosition = new Vector3(baseX + part.x, baseY + part.y);
                        Builder.CurrentColor = new Color32(208, 0, 208, 255);
                        Builder.NextVertex();
                        Builder.CurrentPosition = new Vector3(baseX + part.x, baseY + part.y - 2f * part.state);
                        Builder.CurrentColor = new Color32(64, 0, 64, 255);
                        Builder.NextVertex();
                        // bottom
                        Builder.CurrentPosition = new Vector3(baseX + part.x, baseY + part.y);
                        Builder.CurrentColor = new Color32(208, 0, 208, 255);
                        Builder.NextVertex();
                        Builder.CurrentPosition = new Vector3(baseX + part.x, baseY + part.y + 2f * part.state);
                        Builder.CurrentColor = new Color32(64, 0, 64, 255);
                        Builder.NextVertex();
                    }
                }

                if (item.IsMoney)
                {
                    // format money: split by every 3 digits with ,
                    long money = item.Price;
                    if (UiManager.Instance.DragItem == item)
                        money -= UiManager.Instance.DragMoneyCount;
                    TextRenderers[rnd].Text = FormatMoney(money);
                    TextObjects[rnd].SetActive(true);
                }
                else
                {
                    int dcount = item.Count;
                    if (UiManager.Instance.DragItem == item)
                        dcount -= UiManager.Instance.DragItemCount;

                    if (dcount > 1)
                    {
                        TextRenderers[rnd].Text = dcount.ToString();
                        TextObjects[rnd].SetActive(true);
                    }
                    else
                    {
                        TextRenderers[rnd].Text = "";
                        TextObjects[rnd].SetActive(false);
                    }
                }

                if (ShopMode && !item.IsMoney)
                {
                    long price = item.Price;
                    switch (item.NetParent)
                    {
                        case ServerCommands.ItemMoveLocation.UnitBody:
                        case ServerCommands.ItemMoveLocation.UnitPack:
                            price /= 2;
                            break;

                        default:
                            break;
                    }
                    if (price < 1) price = 1;
                    ShopTextRenderers[rnd].Text = FormatMoney(price);
                    ShopTextObjects[rnd].SetActive(true);
                }
            }

            rnd++;
            x++;
            if (x >= InvWidth)
            {
                x = 0;
                y++;
            }
        }

        Builder.CurrentZ = -0.2f;

        // add item pictures
        x = 0;
        y = 0;
        for (int i = start; i < end; i++)
        {
            Item item = (i == Pack.Count && ShowMoney) ? GetVisualMoneyItem() : Pack[i];

            if (item != null)
            {
                item.Class.File_Pack.UpdateSprite();
                // check texture.
                // for now, just put generic background
                Renderer.materials[InvWidth * InvHeight + y * InvWidth + x].mainTexture = item.Class.File_Pack.File.Atlas;
                Renderer.materials[InvWidth * InvHeight + y * InvWidth + x].SetTexture("_Palette", item.Class.File_Pack.File.OwnPalette);
                Color color = new Color(1, 1, 1, (item == UiManager.Instance.DragItem) ? 0.25f : 1); // draw dragged items half transparent
                Builder.AddQuad(InvWidth * InvHeight + y * InvWidth + x, (x * 80) * InvScale, (y * 80) * InvScale, 80 * InvScale, 80 * InvScale, item.Class.File_Pack.File.AtlasRects[0], color);
            }

            x++;
            if (x >= InvWidth)
            {
                x = 0;
                y++;
            }
        }

        if (ShopMode)
        {
            // add price backgrounds
            Builder.CurrentZ = -0.3f;
            x = 0;
            y = 0;
            rnd = 0;
            for (int i = start; i < end; i++)
            {
                if (ShopTextObjects[rnd].activeSelf)
                {
                    // look at text renderer of this item
                    AllodsTextRenderer atr = ShopTextRenderers[rnd];
                    // get width of price field.
                    // if price fits, use one of the existing images. we require text width + 12
                    int requiredSize = atr.ActualWidth + 9;
                    Texture2D image = null;
                    int imageSize = -1;

                    for (int j = 0; j < 7; j++)
                    {
                        if (img_PriceSizes[j] >= requiredSize)
                        {
                            image = img_Prices[j];
                            imageSize = img_PriceSizes[j];
                            break;
                        }
                    }

                    if (image == null)
                        image = img_Prices[img_Prices.Length - 1];

                    int submesh = InvWidth * InvHeight * 2 + 1 + y * InvWidth + x;
                    Renderer.materials[submesh].mainTexture = image;
                    if (imageSize != -1)
                    {
                        Builder.AddQuad(submesh, ((x + 1) * 80) * InvScale - image.width, (y * 80) * InvScale, image.width, image.height);
                    }
                    else
                    {
                        // we could not find a pre-made image that fits our required size.
                        // this means we need to take largest existing image, and just crop it by parts to produce arbitrary sized background
                        imageSize = atr.ActualWidth + 9;
                        float texX = ((x + 1) * 80) * InvScale - imageSize;
                        float texY = (y * 80) * InvScale;
                        Builder.AddQuad(submesh, texX, texY, 5, image.height, new Rect(3f / image.width, 0, 5f / image.width, 1), new Color(1, 1, 1, 1));
                        Builder.AddQuad(submesh, texX + 5, texY, imageSize - (5 + 7), image.height, new Rect(8f / image.width, 0, 45f / image.width, 1), new Color(1, 1, 1, 1));
                        Builder.AddQuad(submesh, texX + imageSize - 7, texY, 7, image.height, new Rect(53f / image.width, 0, 7f / image.width, 1), new Color(1, 1, 1, 1));
                    }
                }

                rnd++;
                x++;
                if (x >= InvWidth)
                {
                    x = 0;
                    y++;
                }
            }
        }

        MeshTopology[] topologies = new MeshTopology[InvWidth * InvHeight * 3 + 1];
        for (int i = 0; i < InvWidth * InvHeight; i++)
        {
            topologies[i] = MeshTopology.Quads;
            topologies[InvWidth * InvHeight + i] = MeshTopology.Quads;
            topologies[InvWidth * InvHeight * 2 + 1 + i] = MeshTopology.Quads;
        }
        topologies[InvWidth * InvHeight * 2] = MeshTopology.Lines;

        Builder.CurrentMesh = topologies.Length - 1;
        Filter.mesh = Builder.ToMesh(topologies);
    }

    public int GetVisualPackCount()
    {
        return Pack.Count + (ShowMoney ? 1 : 0);
    }

    private Item _MoneyItem = null;
    public Item GetVisualMoneyItem()
    {
        if (_MoneyItem == null)
        {
            _MoneyItem = new Item("Gold");
            _MoneyItem.Parent = Pack;
        }
        _MoneyItem.Count = 0;
        _MoneyItem.Price = Pack.Parent.Player.Money;
        return _MoneyItem;
    }

    public bool ProcessEvent(Event ev)
    {
        if (Pack == null)
            return false;

        if (ev.rawType == EventType.MouseDown ||
            ev.rawType == EventType.MouseUp ||
            ev.rawType == EventType.MouseMove)
        {
            // check if its inside this widget
            Vector2 mPos = Utils.GetMousePosition();
            if (!new Rect(transform.position.x, transform.position.y, Width, Height).Contains(mPos))
                return false;

            Vector2 mPosLocal = new Vector2(mPos.x - transform.position.x,
                                            mPos.y - transform.position.y);

            int itemHoveredX = (int)(mPosLocal.x / 80 / InvScale);
            int itemHoveredY = (int)(mPosLocal.y / 80 / InvScale);

            int start = Math.Max(Math.Min(Scroll, GetVisualPackCount() - InvWidth * InvHeight), 0);

            MouseCursor.SetCursor(MouseCursor.CurDefault);

            int itemHovered = itemHoveredY * InvWidth + itemHoveredX + start;
            if (itemHovered < 0 || itemHovered >= GetVisualPackCount())
                return true;

            Item item = (itemHovered == Pack.Count && ShowMoney) ? GetVisualMoneyItem() : Pack[itemHovered];

            if (ev.rawType == EventType.MouseMove &&
                ev.commandName == "tooltip")
            {
                UiManager.Instance.SetTooltip(item.ToVisualString());
            }
            else if (ev.rawType == EventType.MouseDown &&
                ev.commandName == "double")
            {
                if (AutoDropTarget != null)
                {
                    Item newItem = Pack.TakeItem(itemHovered, 1);
                    if (newItem != null)
                    {
                        if (!AutoDropTarget.ProcessAutoDrop(newItem))
                            Pack.PutItem(itemHovered, newItem);
                        else UiManager.Instance.UnsetTooltip(); // remove tooltip if item was changed
                    }
                }
            }

            return true;
        }

        return false;
    }

    public bool ProcessCustomEvent(CustomEvent ce)
    {
        return false;
    }

    public bool ProcessStartDrag(float x, float y)
    {
        if (Pack == null)
            return false;

        if (!new Rect(transform.position.x, transform.position.y, Width, Height).Contains(new Vector2(x, y)))
            return false;

        // if this pack belongs to the unit and consoleplayer doesn't control it
        if (Pack.Parent != null &&
            Pack.Parent.Player != MapLogic.Instance.ConsolePlayer) return false;

        Vector2 mPosLocal = new Vector2(x - transform.position.x,
                                        y - transform.position.y);

        int itemHoveredX = (int)(mPosLocal.x / 80 / InvScale);
        int itemHoveredY = (int)(mPosLocal.y / 80 / InvScale);

        int start = Math.Max(Math.Min(Scroll, GetVisualPackCount() - InvWidth * InvHeight), 0);
        int end = Math.Min(start + InvWidth * InvHeight, GetVisualPackCount());

        int itemHovered = itemHoveredY * InvWidth + itemHoveredX + start;
        if (itemHovered < 0 || itemHovered >= GetVisualPackCount())
            return false;

        ItemPack cPack = Pack;
        Item item = (itemHovered == Pack.Count && ShowMoney) ? GetVisualMoneyItem() : Pack[itemHovered];

        if (item == null)
            return false;

        int count = 1;

        // alt = 100
        // ctrl = 1000
        // ctrl+alt = 100000
        // shift = all
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
            Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) count *= 1000;
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) count *= 100;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) count = item.Count;

        if (count > item.Count) count = item.Count;

        long moneyCount = 0;
        if (item.IsMoney)
        {
            moneyCount = 1;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) moneyCount *= 1000;
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) moneyCount *= 100;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) moneyCount = item.Price;
            count = 0;
        }
             
        UiManager.Instance.StartDrag(item, count, moneyCount, () =>
        {
            // 
            //cPack.PutItem(Math.Min(itemHovered, Pack.Count), item);
        });

        return true;
    }

    public bool ProcessDrag(Item item, float x, float y)
    {
        if (Pack == null)
            return false;

        if (!new Rect(transform.position.x, transform.position.y, Width, Height).Contains(new Vector2(x, y)))
            return false;

        if (item.IsMoney)
            return false;

        if (Pack.Parent != item.Parent.Parent &&
            Pack.LocationHint != ServerCommands.ItemMoveLocation.ShopTable &&
            !(Pack.LocationHint != ServerCommands.ItemMoveLocation.Undefined && item.NetParent != ServerCommands.ItemMoveLocation.Undefined &&
              item.NetParent == Pack.LocationHint))
            return false;

        return true;
    }

    public bool ProcessDrop(Item item, float x, float y)
    {
        if (Pack == null)
            return false;

        if (!new Rect(transform.position.x, transform.position.y, Width, Height).Contains(new Vector2(x, y)))
            return false;

        Vector2 mPosLocal = new Vector2(x - transform.position.x,
                                        y - transform.position.y);

        int itemHoveredX = (int)(mPosLocal.x / 80 / InvScale);
        int itemHoveredY = (int)(mPosLocal.y / 80 / InvScale);

        // "Drop" happens after removing item from source pack. this means, that pack is one item less here...
        int start = Math.Max(Math.Min(Scroll, GetVisualPackCount() - InvWidth * InvHeight + ((item.Parent == Pack) ? 1 : 0)), 0);
        int end = Math.Min(start + InvWidth * InvHeight, GetVisualPackCount());

        int itemHovered = itemHoveredY * InvWidth + itemHoveredX + start;
        if (itemHovered < 0)
            itemHovered = 0;
        else if (itemHovered > Pack.Count)
            itemHovered = Pack.Count;

        if (OnProcessDrop != null)
            return OnProcessDrop(item, itemHovered);

        return false;
    }

    public void ProcessEndDrag()
    {

    }

    public void ProcessFailDrag()
    {

    }

    public void ProcessRollbackDrag(Item item)
    {
        Pack.PutItem(item.Index, item);
    }

    public Item ProcessVerifyEndDrag()
    {
        // check if item still exists in pack in sufficient count (> 0)
        if (Pack == null)
            return null;

        if (UiManager.Instance.DragItem.IsMoney && UiManager.Instance.DragItem.Parent == Pack)
        {
            Item newGold = new Item("Gold");
            newGold.Price = UiManager.Instance.DragMoneyCount;
            newGold.Count = 0;
            UiManager.Instance.DragItem.Price = (Pack.Parent.Player.Money -= UiManager.Instance.DragMoneyCount);
            return newGold;
        }

        return Pack.TakeItem(UiManager.Instance.DragItem, UiManager.Instance.DragItemCount);
    }

    public bool ProcessAutoDrop(Item item)
    {
        if (Pack == null)
            return false;

        if (OnProcessDrop != null)
            return OnProcessDrop(new Item(item, 1), Pack.Count);

        return false;
    }
}