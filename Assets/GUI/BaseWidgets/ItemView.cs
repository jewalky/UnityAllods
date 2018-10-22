using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class ItemView : Widget, IUiEventProcessor, IUiItemDragger, IUiItemAutoDropper
{
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
            float rx = UnityEngine.Random.Range(10, 70);
            float ry = UnityEngine.Random.Range(10, 70);
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

    private List<GameObject> TextObjects = new List<GameObject>();
    private List<AllodsTextRenderer> TextRenderers = new List<AllodsTextRenderer>();

    public void OnDestroy()
    {
        UiManager.Instance.Unsubscribe(this);
    }

    public void Start()
    {
        UiManager.Instance.Subscribe(this);

        Width = InvWidth * 80;
        Height = InvHeight * 80;

        Renderer = gameObject.AddComponent<MeshRenderer>();
        Filter = gameObject.AddComponent<MeshFilter>();

        // load necessary images
        if (img_BackInv == null) img_BackInv = Images.LoadImage("graphics/interface/backinv.bmp", Images.ImageType.AllodsBMP);
        if (img_BackInvB == null) img_BackInvB = Images.LoadImage("graphics/interface/backinvb.bmp", Images.ImageType.AllodsBMP);
        if (img_BackInvG == null) img_BackInvG = Images.LoadImage("graphics/interface/backinvg.bmp", Images.ImageType.AllodsBMP);
        if (img_BackInvS == null) img_BackInvS = Images.LoadImage("graphics/interface/backinvs.bmp", Images.ImageType.AllodsBMP);

        // create InvWidth*InvHeight*2+1 materials
        List<Material> materials = new List<Material>();
        for (int i = 0; i < InvWidth * InvHeight; i++)
            materials.Add(new Material(MainCamera.MainShader));
        for (int i = 0; i < InvWidth * InvHeight; i++)
            materials.Add(new Material(MainCamera.MainShaderPaletted));
        materials.Add(new Material(MainCamera.MainShader));
        Renderer.materials = materials.ToArray();
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

                    go.transform.localPosition = new Vector3(lx * 80 + 3, ly * 80 + 80 - atr.Font.LineHeight - 3, -0.2f);
                }
            }
        }

        // first submesh = quads, item background
        // second submesh = lines, item magic glow
        // third submesh = quads, item pictures

        if (Pack == null)
        {
            Filter.mesh.Clear();
            return;
        }

        Builder.Reset();

        int start = Math.Max(Math.Min(Scroll, Pack.Count - InvWidth * InvHeight - 1), 0);
        int end = Math.Min(start + InvWidth * InvHeight, Pack.Count);
        int x = 0;
        int y = 0;
        for (int i = start; i < end; i++)
        {
            Builder.AddQuad(y * InvWidth + x, x * 80, y * 80, 80, 80, new Rect(0, 0, 1, 1));
            // check texture.
            // for now, just put generic background
            Renderer.materials[y * InvWidth + x].mainTexture = img_BackInv;

            x++;
            if (x >= InvWidth)
            {
                x = 0;
                y++;
            }
        }

        for (int i = 0; i < TextObjects.Count; i++)
            TextObjects[i].SetActive(false);

        // now add magic glow where it should be
        x = 0;
        y = 0;
        UpdateMGlow(); // per-widget unique animation is used.
        for (int i = start; i < end; i++)
        {
            // check if item has special effects
            Item item = Pack[i];
            if (item.MagicEffects.Count > 0)
            {
                float baseX = x * 80;
                float baseY = y * 80;
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

            int dcount = item.Count;
            if (UiManager.Instance.DragItem == item)
                dcount -= UiManager.Instance.DragItemCount;

            if (dcount > 1)
            {
                TextRenderers[i].Text = dcount.ToString();
                TextObjects[i].SetActive(true);
            }

            x++;
            if (x >= InvWidth)
            {
                x = 0;
                y++;
            }
        }

        // add item pictures
        x = 0;
        y = 0;
        for (int i = start; i < end; i++)
        {
            Item item = Pack[i];
            item.Class.File_Pack.UpdateSprite();
            // check texture.
            // for now, just put generic background
            Renderer.materials[InvWidth * InvHeight + y * InvWidth + x].mainTexture = item.Class.File_Pack.File.Atlas;
            Renderer.materials[InvWidth * InvHeight + y * InvWidth + x].SetTexture("_Palette", item.Class.File_Pack.File.OwnPalette);
            Color color = new Color(1, 1, 1, (item == UiManager.Instance.DragItem) ? 0.25f : 1); // draw dragged items half transparent
            Builder.AddQuad(InvWidth * InvHeight + y * InvWidth + x, x * 80, y * 80, 80, 80, item.Class.File_Pack.File.AtlasRects[0], color);

            x++;
            if (x >= InvWidth)
            {
                x = 0;
                y++;
            }
        }

        MeshTopology[] topologies = new MeshTopology[InvWidth * InvHeight * 2 + 1];
        for (int i = 0; i < InvWidth * InvHeight; i++)
        {
            topologies[i] = MeshTopology.Quads;
            topologies[InvWidth * InvHeight + i] = MeshTopology.Quads;
        }
        topologies[InvWidth * InvHeight * 2] = MeshTopology.Lines;

        Builder.CurrentMesh = topologies.Length - 1;
        Filter.mesh = Builder.ToMesh(topologies);
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

            int itemHoveredX = (int)(mPosLocal.x / 80);
            int itemHoveredY = (int)(mPosLocal.y / 80);

            int start = Math.Max(Math.Min(Scroll, Pack.Count - InvWidth * InvHeight - 1), 0);
            int end = Math.Min(start + InvWidth * InvHeight, Pack.Count);

            int itemHovered = itemHoveredY * InvWidth + itemHoveredX + start;
            if (itemHovered < 0 || itemHovered >= Pack.Count)
                return false;

            MouseCursor.SetCursor(MouseCursor.CurDefault);
            Item item = Pack[itemHovered];

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
                    if (!AutoDropTarget.ProcessAutoDrop(newItem))
                        Pack.PutItem(itemHovered, newItem);
                }
            }

            return true;
        }

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

        int itemHoveredX = (int)(mPosLocal.x / 80);
        int itemHoveredY = (int)(mPosLocal.y / 80);

        int start = Math.Max(Math.Min(Scroll, Pack.Count - InvWidth * InvHeight - 1), 0);
        int end = Math.Min(start + InvWidth * InvHeight, Pack.Count);

        int itemHovered = itemHoveredY * InvWidth + itemHoveredX + start;
        if (itemHovered < 0 || itemHovered >= Pack.Count)
            return false;

        ItemPack cPack = Pack;
        Item item = Pack[itemHovered];
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
             
        UiManager.Instance.StartDrag(item, count, () =>
        {
            // 
            //cPack.PutItem(Math.Min(itemHovered, cPack.Count), item);
        });

        return true;
    }

    public bool ProcessDrag(Item item, float x, float y)
    {
        if (Pack == null)
            return false;

        if (!new Rect(transform.position.x, transform.position.y, Width, Height).Contains(new Vector2(x, y)))
            return false;

        return true;
    }

    private void SendItemMoveCommand(Item item, int index)
    {
        // check if this pack is unit's pack.
        if (Pack.Parent == null ||
            Pack != Pack.Parent.ItemsPack) return;

        // send command.
        // first off, determine move source.
        ServerCommands.ItemMoveLocation from;
        int fromIndex = -1;
        ServerCommands.ItemMoveLocation to;
        int toIndex = -1;

        MapUnit unit = Pack.Parent;

        if (item.Parent == unit.ItemsBody)
        {
            from = ServerCommands.ItemMoveLocation.UnitBody;
            fromIndex = item.Class.Option.Slot;
        }
        else if (item.Parent == unit.ItemsPack)
        {
            from = ServerCommands.ItemMoveLocation.UnitPack;
            fromIndex = item.Index;
        }
        else from = ServerCommands.ItemMoveLocation.Ground;

        to = ServerCommands.ItemMoveLocation.UnitPack;
        toIndex = index;

        Client.SendItemMove(from, to, fromIndex, toIndex, item.Count, unit, MapView.Instance.MouseCellX, MapView.Instance.MouseCellY);
    }

    public bool ProcessDrop(Item item, float x, float y)
    {
        if (Pack == null)
            return false;

        if (!new Rect(transform.position.x, transform.position.y, Width, Height).Contains(new Vector2(x, y)))
            return false;

        Vector2 mPosLocal = new Vector2(x - transform.position.x,
                                        y - transform.position.y);

        int itemHoveredX = (int)(mPosLocal.x / 80);
        int itemHoveredY = (int)(mPosLocal.y / 80);

        int start = Math.Max(Math.Min(Scroll, Pack.Count - InvWidth * InvHeight - 1), 0);
        int end = Math.Min(start + InvWidth * InvHeight, Pack.Count);

        int itemHovered = itemHoveredY * InvWidth + itemHoveredX + start;
        if (itemHovered < 0)
            itemHovered = 0;
        else if (itemHovered > Pack.Count)
            itemHovered = Pack.Count;

        SendItemMoveCommand(item, itemHovered);
        Pack.PutItem(itemHovered, item);
        return true;
    }

    public void ProcessEndDrag()
    {

    }

    public void ProcessFailDrag()
    {

    }

    public Item ProcessVerifyEndDrag()
    {
        // check if item still exists in pack in sufficient count (> 0)
        if (Pack == null)
            return null;
        return Pack.TakeItem(UiManager.Instance.DragItem, UiManager.Instance.DragItemCount);
    }

    public bool ProcessAutoDrop(Item item)
    {
        if (Pack == null)
            return false;

        SendItemMoveCommand(item, Pack.Count);
        Pack.PutItem(Pack.Count, new Item(item, 1));
        return true;
    }
}