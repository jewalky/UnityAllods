using UnityEngine;
using System.Collections;

public class MouseCursor : MonoBehaviour {

    public class MapCursorSettings
    {
        internal int Xoffs;
        internal int Yoffs;
        internal Images.AllodsSprite Sprite;
        internal Sprite[] Sprites;
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

    public static MapCursorSettings CurrentCursor = null;
    public static Texture2D tex = null;
    private static float LastCursorTime = 0;
    private static int CurrentCursorFrame = 0;
    
    public static MapCursorSettings CreateCursor(string filename, int x, int y, float delay)
    {
        MapCursorSettings mcs = new MapCursorSettings();
        mcs.Xoffs = x;
        mcs.Yoffs = y;
        Images.AllodsSprite sprite = Images.LoadSprite(filename);
        mcs.Sprite = sprite;
        mcs.Sprites = mcs.Sprite.Sprites;
        mcs.Delay = delay;
        return mcs;
    }

    public static void SetCursor(MapCursorSettings mcs)
    {
        if (CurrentCursor == mcs)
            return;
        CurrentCursor = mcs;
        if (CurrentCursor == null)
            return;
        LastCursorTime = Time.unscaledTime;
        CurrentCursorFrame = 0;
    }

    public static void SetCursor(Images.AllodsSprite sprite)
    {
        CurItem.Sprite = sprite;
        CurItem.Sprites = CurItem.Sprite.Sprites;
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

    // pseudo-cursor for items
    private static MapCursorSettings CurItem = null;

    private static SpriteRenderer Renderer = null;
    void Start ()
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
        SetCursor(CurDefault);

        CurItem = new MapCursorSettings();
        CurItem.Xoffs = 40;
        CurItem.Yoffs = 40;
        CurItem.Sprite = null;
        CurItem.Sprites = null;
        CurItem.Delay = 0;

        transform.localScale = new Vector3(100, 100, 1);
        Renderer = GetComponent<SpriteRenderer>();
    }

    public void Update()
    {
        /*Vector3 deltaPos = Input.mousePosition - oldPosition;
        Utils.SetMousePosition(deltaPos.x, deltaPos.y);*/
        float xDelta = Input.GetAxis("MouseX") * 2.5f;
        float yDelta = Input.GetAxis("MouseY") * 2.5f;
        Utils.SetMousePosition(xDelta, yDelta);
    }

    // set / display cursor (called from the camera)
	public static void OnPreRenderCursor()
    {
        if (GameManager.Instance.IsHeadless)
            return;

        if (CurrentCursor == null)
        {
            Renderer.enabled = false;
            return;
        }

        if (CurrentCursor.Delay > 0)
        {
            LastCursorTime += Time.unscaledDeltaTime;
            if (LastCursorTime >= CurrentCursor.Delay)
            {
                while (LastCursorTime >= CurrentCursor.Delay)
                {
                    CurrentCursorFrame = ++CurrentCursorFrame % CurrentCursor.Sprites.Length;
                    LastCursorTime -= CurrentCursor.Delay;
                }
            }
        }
        else CurrentCursorFrame = 0;

        Renderer.enabled = true;
        Vector3 mPos = Utils.GetMousePosition();
        Instance.transform.position = new Vector3(mPos.x - CurrentCursor.Xoffs, mPos.y - CurrentCursor.Yoffs, MainCamera.MouseZ);
        Renderer.sprite = CurrentCursor.Sprites[CurrentCursorFrame];
        Renderer.material.shader = MainCamera.MainShaderPaletted;
        Renderer.material.SetTexture("_Palette", CurrentCursor.Sprite.OwnPalette);
    }
}
