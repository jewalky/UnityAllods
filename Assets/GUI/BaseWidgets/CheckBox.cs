using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CheckBox : Widget, IUiEventProcessor, IFocusableWidget
{
    AllodsTextRenderer LabelRendererA;
    GameObject LabelObject;
    MeshRenderer LabelRenderer;
    GameObject LabelSubObject;

    private string _Text = "";
    public string Text
    {
        get
        {
            return LabelRendererA.Text;
        }

        set
        {
            _Text = value;
            if (LabelRendererA != null)
                LabelRendererA.Text = value;
        }
    }

    public void OnDestroy()
    {
        UiManager.Instance.Unsubscribe(this);
    }

    private static Images.AllodsSprite imgRadiob = null;

    public void Start()
    {
        UiManager.Instance.Subscribe(this);

        if (imgRadiob == null)
            imgRadiob = Images.Load256("graphics/interface/radiob.256");

        Renderer = gameObject.AddComponent<MeshRenderer>();
        Renderer.material = new Material(MainCamera.MainShaderPaletted);
        Renderer.material.mainTexture = imgRadiob.Atlas;
        Renderer.material.SetTexture("_Palette", imgRadiob.OwnPalette);
        Filter = gameObject.AddComponent<MeshFilter>();

        LabelRendererA = new AllodsTextRenderer(Fonts.Font1, Font.Align.Left, Width);
        LabelRendererA.Text = _Text;
        LabelObject = LabelRendererA.GetNewGameObject(0.01f, transform, 100, 1);
        LabelObject.transform.localPosition = new Vector3(32, Height / 2 - 8, 0);
        LabelSubObject = LabelObject.transform.GetChild(0).gameObject;
        LabelRenderer = LabelObject.GetComponent<MeshRenderer>();

        UpdateMesh();
    }

    public void OnFocus()
    {

    }

    public void OnBlur()
    {

    }

    public bool ProcessEvent(Event e)
    {
        if (e.rawType == EventType.MouseDown)
        {
            // detect position
            if (!_Hovered)
                return false;

            // focus field
            IsFocused = true;
            _Clicked = true;

            return true;
        }
        else if (e.rawType == EventType.MouseUp)
        {
            if (_Clicked && _Hovered)
            {
                // call handler
                Checked = !Checked;
            }

            _Clicked = false;
        }
        else if (e.rawType == EventType.MouseMove)
        {
            Vector2 mpos = Utils.GetMousePosition();
            _Hovered = new Rect(transform.position.x, transform.position.y, Width, Height).Contains(mpos);
        }
        else if (e.type == EventType.KeyDown)
        {
            if (!IsFocused)
                return false;

            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                Checked = !Checked;
            else if (e.keyCode == KeyCode.Space)
                _Clicked = true;
        }
        else if (e.type == EventType.KeyUp)
        {
            if (e.keyCode == KeyCode.Space && _Clicked)
            {
                Checked = !Checked;
                _Clicked = false;
            }
        }

        return false;
    }

    private MeshRenderer Renderer;
    private MeshFilter Filter;
    private Utils.MeshBuilder Builder;

    public bool Checked = false;
    private bool _Clicked = false;
    private bool _Hovered = false;

    private bool LastChecked = false;
    private bool LastClicked = false;
    private int LastWidth = -1;
    private int LastHeight = -1;
    public void UpdateMesh()
    {
        if (Filter.mesh != null && LastWidth == Width && LastHeight == Height && LastClicked == _Clicked && LastChecked == Checked)
            return;

        LastChecked = Checked;
        LastClicked = _Clicked;
        LastWidth = Width;
        LastHeight = Height;

        if (Builder == null) Builder = new Utils.MeshBuilder();

        // 
        Color col_inactive = new Color(1, 1, 1, 1);
        Color col_active = new Color(1, 1, 1, 0);

        if (_Clicked)
            col_active = new Color(1, 1, 1, 0.5f);
        else if (Checked)
        {
            col_active = new Color(1, 1, 1, 1);
            col_inactive = new Color(0, 0, 0, 1);
        }

        Builder.AddQuad(0, 0, 0, imgRadiob.Sprites[2].rect.width, imgRadiob.Sprites[2].rect.height, imgRadiob.AtlasRects[2], col_inactive); // disabled
        Builder.AddQuad(0, 0, 0, imgRadiob.Sprites[3].rect.width, imgRadiob.Sprites[3].rect.height, imgRadiob.AtlasRects[3], col_active); // disabled

        Filter.mesh = Builder.ToMesh(MeshTopology.Quads);
    }

    public void Update()
    {
        if (!IsFocused)
            _Clicked = false;

        UpdateMesh();
        LabelRendererA.Width = Width;
        LabelObject.transform.localPosition = new Vector3(32, Height / 2 - 8, 0);

        //
        if (_Hovered || _Clicked || IsFocused)
            LabelRenderer.material.color = new Color32(189, 158, 74, 255);
        else LabelRenderer.material.color = new Color32(214, 211, 214, 255);
    }
}