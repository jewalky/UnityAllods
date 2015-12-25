﻿using UnityEngine;
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

    public MapCursorSettings CurrentCursor = null;
    public Texture2D tex = null;
    private float LastCursorTime = 0;
    private int CurrentCursorFrame = 0;
    
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
        MouseCursor instance = GameObject.FindObjectOfType<MouseCursor>();
        if (instance.CurrentCursor == mcs)
            return;
        instance.CurrentCursor = mcs;
        if (instance.CurrentCursor == null)
            return;
        instance.LastCursorTime = Time.unscaledTime;
        instance.CurrentCursorFrame = 0;
        instance.tex = instance.CurrentCursor.Sprite.Atlas;
    }

    //public MapCus

    //Images.AllodsSprite sprite;
    //Texture2D[] spritePalettes = new Texture2D[5];
    // Use this for initialization
    public static MapCursorSettings CurDefault = null;
    public static MapCursorSettings CurWait = null;

    private SpriteRenderer Renderer = null;
    void Start ()
    {
        if (GameManager.Instance.IsHeadless) // don't use cursor in graphics-less mode
            return;

        if (!Application.isEditor) // if we remove cursor in editor, it'll affect WHOLE DESKTOP
            Cursor.visible = false;

        CurDefault = CreateCursor("graphics/cursors/default/sprites.16a", 4, 4, 0);
        CurWait = CreateCursor("graphics/cursors/wait/sprites.16a", 16, 16, 0.05f);
        SetCursor(CurDefault);

        transform.localScale = new Vector3(1, 1);
        Renderer = GetComponent<SpriteRenderer>();
    }
	
	// Update is called once per frame
	void Update ()
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
        Vector3 mPos = Utils.Vec3InvertY(Input.mousePosition);
        transform.position = new Vector3(mPos.x - (float)CurrentCursor.Xoffs / 100, mPos.y - (float)CurrentCursor.Yoffs / 100, MainCamera.MouseZ);
        Renderer.sprite = CurrentCursor.Sprites[CurrentCursorFrame];
        Renderer.material.shader = MainCamera.MainShaderPaletted;
        Renderer.material.SetTexture("_Palette", CurrentCursor.Sprite.OwnPalette);
    }
}
