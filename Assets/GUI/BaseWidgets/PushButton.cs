using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class PushButton : Widget, IUiEventProcessor, IFocusableWidget
{
    public delegate void ClickHandler();
    public ClickHandler OnClick;

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

    public void Start()
    {
        UiManager.Instance.Subscribe(this);
        Renderer = gameObject.AddComponent<MeshRenderer>();
        Renderer.material = new Material(MainCamera.MainShader);
        Filter = gameObject.AddComponent<MeshFilter>();

        LabelRendererA = new AllodsTextRenderer(Fonts.Font1, Font.Align.Center, Width);
        LabelRendererA.Text = _Text;
        LabelObject = LabelRendererA.GetNewGameObject(0.02f, transform, 100, 1);
        LabelObject.transform.localPosition = new Vector3(0, 0, 0);
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
                if (OnClick != null)
                    OnClick();
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
            {
                if (OnClick != null)
                    OnClick();
            }
            else if (e.keyCode == KeyCode.Space)
                _Clicked = true;
        }
        else if (e.type == EventType.KeyUp)
        {
            if (e.keyCode == KeyCode.Space && _Clicked)
            {
                if (OnClick != null)
                    OnClick();
                _Clicked = false;
            }
        }

        return false;
    }

    private MeshRenderer Renderer;
    private MeshFilter Filter;

    private bool _Clicked = false;
    private bool _Hovered = false;

    private bool LastClicked = false;
    private int LastWidth = -1;
    private int LastHeight = -1;
    public void UpdateMesh()
    {
        if (Filter.mesh != null && LastWidth == Width && LastHeight == Height && LastClicked == _Clicked)
            return;

        LastClicked = _Clicked;
        LastWidth = Width;
        LastHeight = Height;

        Mesh mesh = Filter.mesh;
        if (mesh == null) mesh = new Mesh();
        mesh.Clear();

        Vector3[] qv = new Vector3[6 * 2];
        Color32[] qc = new Color32[6 * 2];
        int pp = 0, ppc = pp;

        Color32 color1 = new Color32(90, 113, 99, 255);
        Color32 color2 = new Color32(0, 0, 0, 255);

        if (_Clicked)
        {
            Color32 cint = color2;
            color2 = color1;
            color1 = cint;
        }

        // top line
        qv[pp++] = new Vector3(0, 0);
        qc[ppc++] = color1;
        qv[pp++] = new Vector3(Width - 3, 0);
        qc[ppc++] = color1;

        // left line
        qv[pp++] = new Vector3(0, 0);
        qc[ppc++] = color1;
        qv[pp++] = new Vector3(0, Height - 3);
        qc[ppc++] = color1;

        // right line
        qv[pp++] = new Vector3(Width - 1, 1);
        qc[ppc++] = color2;
        qv[pp++] = new Vector3(Width - 1, Height - 2);
        qc[ppc++] = color2;

        // bottom line
        qv[pp++] = new Vector3(1, Height - 1);
        qc[ppc++] = color2;
        qv[pp++] = new Vector3(Width - 2, Height - 1);
        qc[ppc++] = color2;

        // right line (x2)
        qv[pp++] = new Vector3(Width - 2, 0);
        qc[ppc++] = color2;
        qv[pp++] = new Vector3(Width - 2, Height - 3);
        qc[ppc++] = color2;

        // bottom line (x2)
        qv[pp++] = new Vector3(0, Height - 2);
        qc[ppc++] = color2;
        qv[pp++] = new Vector3(Width - 2, Height - 2);
        qc[ppc++] = color2;

        mesh.vertices = qv;
        mesh.colors32 = qc;
        int[] qt = new int[6 * 2];
        for (int i = 0; i < qt.Length; i++)
            qt[i] = i;
        mesh.SetIndices(qt, MeshTopology.Lines, 0);
        Filter.mesh = mesh;
    }

    public void Update()
    {
        if (!IsFocused)
            _Clicked = false;

        UpdateMesh();
        LabelRendererA.Width = Width;
        LabelObject.transform.localPosition = new Vector3(0, Height/2-9, 0);

        //
        if (_Hovered || _Clicked || IsFocused)
            LabelRenderer.material.color = new Color32(189, 158, 74, 255);
        else LabelRenderer.material.color = new Color32(214, 211, 214, 255);

        if (_Clicked)
            LabelSubObject.transform.localPosition = new Vector3(0.03f, 0.03f, 1);
        else LabelSubObject.transform.localPosition = new Vector3(0.02f, 0.02f, 1);
    }
}