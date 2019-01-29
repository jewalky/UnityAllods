using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewSpellbook : MonoBehaviour, IUiEventProcessor
{
    private MeshRenderer Renderer;
    private MeshFilter Filter;
    private Utils.MeshBuilder Builder;

    private static Texture2D SpbFrame = null;
    private static Texture2D SpbPlaceholder = null;

    public MapUnit Unit { get; private set; }

    private Spell.Spells _ActiveSpell;
    public Spell.Spells ActiveSpell
    {
        get
        {
            if ((SpellsMask & (1u << (int)_ActiveSpell)) == 0)
                return Spell.Spells.NoneSpell;
            return _ActiveSpell;
        }
        set
        {
            if (_ActiveSpell != value)
            {
                _ActiveSpell = value;
                UpdateMesh();
            }
        }
    }

    public List<Spell> Spells;
    public uint SpellsMask { get; private set; }

    private static Spell.Spells[] SpellRemap = new Spell.Spells[]
    {
        Spell.Spells.Fire_Arrow,
        Spell.Spells.Fire_Ball,
        Spell.Spells.Wall_of_Fire,
        Spell.Spells.Protection_from_Fire,
        Spell.Spells.Heal,
        Spell.Spells.Bless,
        Spell.Spells.Haste,
        Spell.Spells.Drain_Life,
        Spell.Spells.Protection_from_Air,
        Spell.Spells.Invisibility,
        Spell.Spells.Prismatic_Spray,
        Spell.Spells.Lightning,
        Spell.Spells.Ice_Missile,
        Spell.Spells.Poison_Cloud,
        Spell.Spells.Blizzard,
        Spell.Spells.Protection_from_Water,
        Spell.Spells.Summon,
        Spell.Spells.Control_Spirit,
        Spell.Spells.Teleport,
        Spell.Spells.Shield,
        Spell.Spells.Protection_from_Earth,
        Spell.Spells.Stone_Curse,
        Spell.Spells.Wall_of_Earth,
        Spell.Spells.Diamond_Dust
    };

    Spell.Spells RemapToSpell(int x, int y)
    {
        return RemapToSpell(y * 12 + x);
    }

    static Spell.Spells RemapToSpell(int location)
    {
        if (location < 0 || location >= SpellRemap.Length)
            return Spell.Spells.NoneSpell;
        return SpellRemap[location];
    }

    static int RemapFromSpell(Spell.Spells spId)
    {
        for (int i = 0; i < SpellRemap.Length; i++)
        {
            if (SpellRemap[i] == spId)
                return i;
        }

        return -1;
    }

    public void Awake()
    {
        
    }

    public void Start()
    {
        UiManager.Instance.Subscribe(this);

        if (SpbFrame == null) SpbFrame = Images.LoadImage("graphics/interface/spellbook.bmp", 0, Images.ImageType.AllodsBMP);
        if (SpbPlaceholder == null) SpbPlaceholder = Images.LoadImage("graphics/interface/spellback.bmp", 0, Images.ImageType.AllodsBMP);

        //
        Renderer = gameObject.AddComponent<MeshRenderer>();
        Filter = gameObject.AddComponent<MeshFilter>();

        Renderer.materials = new Material[] { new Material(MainCamera.MainShader), new Material(MainCamera.MainShader) };
        Renderer.materials[0].mainTexture = SpbFrame;
        Renderer.materials[1].mainTexture = SpbPlaceholder;

        // generate mesh.
        Builder = new Utils.MeshBuilder();
        UpdateMesh();

        transform.localScale = new Vector3(1, 1, 0.01f);
    }

    public void OnDestroy()
    {
        UiManager.Instance.Unsubscribe(this);
    }

    private void UpdateMesh()
    {
        if (Builder == null || Filter == null || Renderer == null)
            return;

        Builder.Reset();
        Rect internalRect = new Rect(0, 0, 1, 1);
        Builder.AddQuad(0, 0, 0, 480, 85);

        // 
        int loc = RemapFromSpell(ActiveSpell);
        if (loc >= 0)
        {
            int spx = loc % 12;
            int spy = loc / 12;
            Rect irSpellBlock = Utils.DivRect(new Rect(5 + spx * 38, 5 + spy * 38, 37, 37), new Vector2(SpbFrame.width, SpbFrame.height));
            Builder.AddQuad(0, 5 + spx * 38 + 1, 5 + spy * 38 + 1, 37, 37, irSpellBlock, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        for (int i = 0; i < 24; i++)
        {
            if ((SpellsMask & (1u << (int)RemapToSpell(i))) != 0)
                continue;
            int spx = i % 12;
            int spy = i / 12;
            Builder.AddQuad(1, 5 + spx * 38 + 1, 5 + spy * 38 + 1, 36, 36);
        }

        Builder.CurrentMesh = 1;

        Filter.mesh = Builder.ToMesh(MeshTopology.Quads, MeshTopology.Quads);
    }

    // for doubleclick
    private float LastClickTime;
    public bool ProcessEvent(Event e)
    {
        if (e.rawType == EventType.MouseDown ||
            e.rawType == EventType.MouseUp ||
            e.rawType == EventType.MouseMove)
        {
            int lw = 480;
            int lh = 85;

            Vector2 mPos = Utils.GetMousePosition();
            Vector2 mPosLocal = mPos - new Vector2(transform.position.x, transform.position.y);
            if (mPosLocal.x < 0 || mPosLocal.y < 0 ||
                mPosLocal.x > lw || mPosLocal.y > lh)
            {
                return false;
            }

            Vector2 spLocal = new Vector2((mPosLocal.x - 5) / 38, (mPosLocal.y - 5) / 38);
            int spd = (int)spLocal.y * 12 + (int)spLocal.x;
            if (spd > 24 || spd < 0)
                spd = -1;
            Spell.Spells sp = RemapToSpell(spd);

            if (e.rawType == EventType.MouseDown)
            {
                ActiveSpell = sp;
                MapView.Instance.OneTimeCast = null;

                float ctime = Time.unscaledTime;
                if (ctime - LastClickTime < 0.25f && (SpellsMask & (int)ActiveSpell) != 0 && !Spell.IsAttackSpell(ActiveSpell))
                {
                    MapObject curObject = MapView.Instance.SelectedObject;
                    if (curObject is MapUnit)
                        Client.SendCastToUnit((MapUnit)curObject, new global::Spell((int)sp, (MapUnit)curObject), (MapUnit)curObject, curObject.X, curObject.Y);
                }

                LastClickTime = ctime;
            }

            if (e.rawType == EventType.MouseMove &&
                e.commandName == "tooltip" &&
                (SpellsMask & (int)sp) != 0)
            {
                if (spd >= 0 && spd < Locale.Spells.Count)
                {
                    string tt = Locale.Spells[spd];
                    MapObject curObject = MapView.Instance.SelectedObject;
                    if (curObject is MapUnit)
                    {
                        tt += "\n"+new Spell((int)sp, (MapUnit)curObject).ToVisualString();
                    }
                    UiManager.Instance.SetTooltip(tt);
                }
            }

            MouseCursor.SetCursor(MouseCursor.CurDefault);
            return true;
        }

        return false;
    }

    public void SetSpells(MapUnit unit)
    {
        uint oldSpellsMask = SpellsMask;
        if (unit != null)
        {
            Unit = unit;
            Spells = unit.SpellBook;
            SpellsMask = 0;
            for (int i = 0; i < 32; i++)
            {
                if (unit.GetSpell((Spell.Spells)i) != null)
                    SpellsMask |= 1u << i;
            }
        }
        else
        {
            Spells = null;
            SpellsMask = 0;
        }

        if (oldSpellsMask != SpellsMask)
            UpdateMesh();
    }

    public void Update()
    {
        if (MapView.Instance == null)
            return;
        int dOffset = 0;
        if (MapView.Instance.InventoryVisible)
            dOffset -= 90;
        transform.localPosition = new Vector3((Screen.width - 176) / 2 - 240 + 8, Screen.height - 85 + dOffset, MainCamera.InterfaceZ + 0.99f); // on this layer all map UI is drawn
        if (Unit != null)
            SetSpells(Unit);
    }
}