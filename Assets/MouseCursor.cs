using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MouseCursor : Graphic {

    public class MapCursorSettings
    {
        internal int Xoffs;
        internal int Yoffs;
        internal Images.AllodsSprite Sprite;
        internal float Delay;
    }

    private static MouseCursor _Instance;
    public static MouseCursor Instance
    {
        get
        {
            if (_Instance == null) _Instance = FindObjectOfType<MouseCursor>();
            return _Instance;
        }
    }

    public bool Visible
    {
        get
        {
            return _Visible;
        }

        set
        {
            _Visible = value;
        }
    }

    public Material _Material = null;
    public override Material material
    {
        get
        {
            if (CurrentCursor != null)
            {
                if (_Material == null)
                    _Material = new Material(MainCamera.MainShaderPaletted);
                _Material.mainTexture = CurrentCursor.Sprite.Atlas;
                _Material.SetTexture("_Palette", CurrentCursor.Sprite.OwnPalette);
                return _Material;
            }
            else
            {
                return null;
            }
        }
    }

    public override Texture mainTexture
    {
        get
        {
            if (CurrentCursor != null)
            {
                return CurrentCursor.Sprite.Atlas;
            }
            else
            {
                return null;
            }
        }
    }

    public static MapCursorSettings CurrentCursor = null;
    public static Texture2D tex = null;
    private static float LastCursorTime = 0;
    private static int CurrentCursorFrame = 0;
    private static bool _Visible = false;
    private static MapCursorSettings NextCursor = null;

    public static MapCursorSettings CreateCursor(string filename, int x, int y, float delay)
    {
        MapCursorSettings mcs = new MapCursorSettings();
        mcs.Xoffs = x;
        mcs.Yoffs = y;
        Images.AllodsSprite sprite = Images.LoadSprite(filename);
        mcs.Sprite = sprite;
        mcs.Delay = delay;
        return mcs;
    }

    public static void SetCursor(MapCursorSettings mcs)
    {
        NextCursor = mcs;
    }

    public static void SetCursor(Images.AllodsSprite sprite)
    {
        CurItem.Sprite = sprite;
        SetCursor(CurItem);
    }

    public static void UnsetCursor()
    {
        SetCursor((MapCursorSettings)null);
    }

    //public MapCus

    //Images.AllodsSprite sprite;
    //Texture2D[] spritePalettes = new Texture2D[5];
    // Use this for initialization
    public static MapCursorSettings CurDefault = null;
    public static MapCursorSettings CurSelect = null;
    public static MapCursorSettings CurSelectStructure = null;
    public static MapCursorSettings CurMove = null;
    public static MapCursorSettings CurAttack = null;
    public static MapCursorSettings CurMoveAttack = null;
    public static MapCursorSettings CurWait = null;
    public static MapCursorSettings CurCantPut = null; // cursor for when item drag-drop is impossible
    public static MapCursorSettings CurPickup = null;
    public static MapCursorSettings CurCast = null;
    public static MapCursorSettings CurSmallDefault = null;

    // pseudo-cursor for items
    private static MapCursorSettings CurItem = null;

    override protected void Start ()
    {
        if (GameManager.Instance.IsHeadless) // don't use cursor in graphics-less mode
            return;

        if (!Application.isEditor)
        {
            Cursor.visible = false;
            //Cursor.lockState = CursorLockMode.Locked;
            Cursor.lockState = CursorLockMode.Confined;
        }

        CurDefault = CreateCursor("graphics/cursors/default/sprites.16a", 4, 4, 0);
        CurSelect = CreateCursor("graphics/cursors/select/sprites.16a", 3, 3, 0);
        CurSelectStructure = CreateCursor("graphics/cursors/town/sprites.16a", 16, 16, 0);
        CurMove = CreateCursor("graphics/cursors/move/sprites.16a", 3, 3, 0.08f);
        CurAttack = CreateCursor("graphics/cursors/attack/sprites.16a", 3, 3, 0.08f);
        CurMoveAttack = CreateCursor("graphics/cursors/swarm/sprites.16a", 3, 3, 0.08f);
        CurWait = CreateCursor("graphics/cursors/wait/sprites.16a", 16, 16, 0.05f);
        CurCantPut = CreateCursor("graphics/cursors/cantput/sprites.16a", 40, 40, 0);
        CurPickup = CreateCursor("graphics/cursors/pickup/sprites.16a", 13, 13, 0.08f);
        CurCast = CreateCursor("graphics/cursors/cast/sprites.16a", 15, 15, 0.08f);
        CurSmallDefault = CreateCursor("graphics/cursors/sdefault/sprites.16a", 2, 2, 0);
        SetCursor(CurDefault);

        CurItem = new MapCursorSettings();
        CurItem.Xoffs = 40;
        CurItem.Yoffs = 40;
        CurItem.Sprite = null;
        CurItem.Delay = 0;
    }

    public void Update()
    {
        /*Vector3 deltaPos = Input.mousePosition - oldPosition;
        Utils.SetMousePosition(deltaPos.x, deltaPos.y);*/
        float xDelta = Input.GetAxis("MouseX") * 2.5f;
        float yDelta = Input.GetAxis("MouseY") * 2.5f;
        Utils.SetMousePosition(xDelta, yDelta);
    }

    private static void CheckNextCursor()
    {
        if (NextCursor != CurrentCursor)
        {
            Debug.LogFormat("NextCursor != CurrentCursor");
            LastCursorTime = Time.unscaledTime;
            CurrentCursorFrame = 0;
            CurrentCursor = NextCursor;
            Instance.UpdateMaterial();
            Instance.UpdateGeometry();
        }
    }

    // set / display cursor (called from the camera)
	public static void OnPreRenderCursor()
    {
        if (GameManager.Instance.IsHeadless)
            return;

        CheckNextCursor();

        if (CurrentCursor == null)
            return;

        bool markForUpdate = false;
        if (CurrentCursor.Delay > 0)
        {
            LastCursorTime += Time.unscaledDeltaTime;
            if (LastCursorTime >= CurrentCursor.Delay)
            {
                markForUpdate = true;
                while (LastCursorTime >= CurrentCursor.Delay)
                {
                    CurrentCursorFrame = ++CurrentCursorFrame % CurrentCursor.Sprite.AtlasRects.Length;
                    LastCursorTime -= CurrentCursor.Delay;
                }
            }
        }
        else CurrentCursorFrame = 0;

        Vector3 mPos = Utils.GetMousePosition();
        Sprite s = CurrentCursor.Sprite.Sprites[CurrentCursorFrame];
        float Xoffs = CurrentCursor.Xoffs / s.rect.width;
        float Yoffs = 1f - (CurrentCursor.Yoffs / s.rect.height);
        Instance.rectTransform.pivot = new Vector2(Xoffs, Yoffs);
        Instance.rectTransform.sizeDelta = s.rect.size;
        Instance.rectTransform.localPosition = new Vector3(mPos.x, mPos.y, MainCamera.MouseZ);
        if (markForUpdate)
        {
            Instance.UpdateMaterial();
            Instance.UpdateGeometry();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (Application.isEditor)
            return;
        if (CurrentCursor == null)
            return;

        float x = rectTransform.rect.width * rectTransform.pivot.x;
        float y = rectTransform.rect.height * rectTransform.pivot.y;

        float w = rectTransform.rect.width;
        float h = rectTransform.rect.height;
        float halfW = w * rectTransform.pivot.x;
        float halfH = h * rectTransform.pivot.y;

        Rect r = CurrentCursor.Sprite.AtlasRects[CurrentCursorFrame];
        float minU = r.xMin, minV = r.yMin, maxU = r.xMax, maxV = r.yMax;

        vh.AddVert(new Vector3(-halfW, -halfH + h, 0), new Color(1, 1, 1, 1), new Vector2(minU, minV));
        vh.AddVert(new Vector3(-halfW + w, -halfH + h, 0), new Color(1, 1, 1, 1), new Vector2(maxU, minV));
        vh.AddVert(new Vector3(-halfW + w, -halfH, 0), new Color(1, 1, 1, 1), new Vector2(maxU, maxV));
        vh.AddVert(new Vector3(-halfW, -halfH, 0), new Color(1, 1, 1, 1), new Vector2(minU, maxV));
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }
}
