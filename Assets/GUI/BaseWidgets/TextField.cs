using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class TextField : MonoBehaviour, IUiEventProcessor
{
    public delegate void ReturnHandler();

    AllodsTextRenderer EditRendererA;
    GameObject EditObject;
    MeshRenderer EditRenderer;
    
    // Mesh for cursor, mesh for selection, mesh for cat...owait.
    GameObject SelectionObject;
    Mesh SelectionMesh;
    MeshRenderer SelectionRenderer;

    int Selection1;
    int Selection2;
    bool EditCursor;
    float EditCursorTimer;

    public Font Font;
    public ReturnHandler OnReturn;
    public int Width;
    public int Height;
    public string Prefix = "";
    public string _Value = "";
    public string Value
    {
        get
        {
            return _Value;
        }

        set
        {
            _Value = value;
            if (Selection1 > _Value.Length)
                Selection1 = _Value.Length;
            if (Selection2 > _Value.Length)
                Selection2 = _Value.Length;
            EditCursor = true;
        }
    }

    public int CursorPosition
    {
        get
        {
            return Selection2;
        }

        set
        {
            Selection1 = Selection2 = value;
            EditCursor = true;
        }
    }

    public void OnDestroy()
    {
        UiManager.Instance.Unsubscribe(this);
    }

    public bool Visible
    {
        get
        {
            return (EditRenderer.enabled && SelectionRenderer.enabled);
        }

        set
        {
            Utils.SetRendererEnabledWithChildren(gameObject, value);
        }
    }

    public void Start()
    {
        UiManager.Instance.Subscribe(this);
        if (Font == null) Font = Fonts.Font1;

        EditRendererA = new AllodsTextRenderer(Font);
        EditObject = EditRendererA.GetNewGameObject(0.01f, transform, 100, 1);
        EditObject.transform.localPosition = new Vector3(0, 0, 0);
        EditRenderer = EditObject.GetComponent<MeshRenderer>();
        EditRenderer.material.color = new Color(1, 1, 1);

        SelectionObject = Utils.CreateObject();
        SelectionObject.transform.parent = transform;
        SelectionObject.transform.localScale = new Vector3(1, 1, 1);
        SelectionObject.transform.localPosition = new Vector3(0, 0, 0.1f);
        SelectionMesh = new Mesh();
        MeshFilter selectionFilter = SelectionObject.AddComponent<MeshFilter>();
        SelectionRenderer = SelectionObject.AddComponent<MeshRenderer>();
        selectionFilter.mesh = SelectionMesh;
        SelectionRenderer.material = new Material(MainCamera.MainShader);

        Selection1 = Selection2 = 0;
        Visible = false;
    }

    public bool ProcessEvent(Event e)
    {
        // handle input events here
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.V && e.control)
            {
                // paste
                int ss1 = Mathf.Min(Selection1, Selection2);
                int ss2 = Mathf.Max(Selection1, Selection2);
                if (ss1 != ss2)
                {
                    _Value = _Value.Remove(ss1, ss2 - ss1);
                    Selection2 = Selection1 = ss1;
                }

                string cstr = GUIUtility.systemCopyBuffer;
                string possibleText2 = _Value;
                for (int i = 0; i < cstr.Length; i++)
                {
                    char ch = cstr[i];
                    if (ch < 0x20) // disallow pasting of control characters and paste space instead
                        ch = ' ';
                    string possibleText = possibleText2.Insert(Selection2, "" + ch);
                    if (EditRendererA.Font.Width(possibleText) <= Width)
                    {
                        possibleText2 = possibleText; // don't allow inserting characters if we don't have space
                        Selection2 = ++Selection1;
                    }
                }

                _Value = possibleText2;
                EditCursor = true;
                return true;
            }
            else if (e.keyCode == KeyCode.C && e.control)
            {
                // copy
                int ss1 = Mathf.Min(Selection1, Selection2);
                int ss2 = Mathf.Max(Selection1, Selection2);
                if (ss1 != ss2)
                {
                    string selected = _Value.Substring(ss1, ss2 - ss1);
                    GUIUtility.systemCopyBuffer = selected;
                }
            }

            switch (e.keyCode)
            {
                case KeyCode.Backspace:
                case KeyCode.Delete:
                    int s1 = Mathf.Min(Selection1, Selection2);
                    int s2 = Mathf.Max(Selection1, Selection2);
                    if (s1 != s2)
                    {
                        _Value = _Value.Remove(s1, s2 - s1);
                        Selection2 = Selection1 = s1;
                    }
                    else
                    {
                        if (e.keyCode == KeyCode.Backspace &&
                            s1 > 0)
                        {
                            _Value = _Value.Remove(Selection2 - 1, 1);
                            Selection1 = --Selection2;
                            if (e.control)
                            {
                                while (Selection2 > 0 && (Selection2 > _Value.Length || _Value[Selection2 - 1] != ' '))
                                {
                                    _Value = _Value.Remove(Selection2 - 1, 1);
                                    Selection1 = --Selection2;
                                }
                            }
                        }
                        else if (e.keyCode == KeyCode.Delete &&
                            s1 < _Value.Length)
                        {
                            _Value = _Value.Remove(Selection2, 1);
                            if (e.control)
                            {
                                while (Selection2 < _Value.Length && _Value[Selection2] != ' ')
                                    _Value = _Value.Remove(Selection2, 1);
                            }
                        }
                    }
                    EditCursor = true;
                    return true;
                case KeyCode.RightArrow:
                    if (Selection2 < _Value.Length)
                        Selection2++;
                    if (e.control)
                    {
                        while (Selection2 < _Value.Length && (Selection2 < 0 || _Value[Selection2] != ' '))
                            Selection2++;
                    }
                    if (!e.shift) Selection1 = Selection2;
                    EditCursor = true;
                    return true;
                case KeyCode.LeftArrow:
                    if (Selection2 > 0)
                        Selection2--;
                    if (e.control)
                    {
                        while (Selection2 > 0 && (Selection2 > _Value.Length || _Value[Selection2 - 1] != ' '))
                            Selection2--;
                    }
                    if (!e.shift) Selection1 = Selection2;
                    EditCursor = true;
                    return true;
                case KeyCode.Home:
                case KeyCode.PageUp:
                    Selection2 = 0;
                    if (!e.shift) Selection1 = Selection2;
                    EditCursor = true;
                    return true;
                case KeyCode.End:
                case KeyCode.PageDown:
                    Selection2 = _Value.Length;
                    if (!e.shift) Selection1 = Selection2;
                    EditCursor = true;
                    return true;
                default:
                    if (e.character >= 0x20 && e.character != '`' && e.character != '~')
                    {
                        int ss1 = Mathf.Min(Selection1, Selection2);
                        int ss2 = Mathf.Max(Selection1, Selection2);
                        if (ss1 != ss2)
                        {
                            _Value = _Value.Remove(ss1, ss2 - ss1);
                            Selection2 = Selection1 = ss1;
                        }

                        // type!
                        string possibleText = _Value.Insert(Selection2, "" + e.character);
                        if (EditRendererA.Font.Width(possibleText) <= Width)
                        {
                            _Value = possibleText; // don't allow inserting characters if we don't have space
                            Selection2 = ++Selection1;
                        }
                        EditCursor = true;
                        return true;
                    }
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (OnReturn != null)
                    {
                        OnReturn();
                        return true;
                    }
                    break;
            }
        }

        return false;
    }

    private void UpdateMesh()
    {
        int s1 = Mathf.Min(Selection1, Selection2);
        int s2 = Mathf.Max(Selection1, Selection2);
        float s1pos = EditRendererA.Font.Width(EditRendererA.Text.Substring(0, s1 + Prefix.Length));
        float s2pos = EditRendererA.Font.Width(EditRendererA.Text.Substring(0, s2 + Prefix.Length));
        float cursorPos = EditRendererA.Font.Width(EditRendererA.Text.Substring(0, Selection2 + Prefix.Length));
        float selectionHeight = EditRendererA.Font.LineHeight;
        float curLeft = 0;
        float curRight = 1; // cursor offset in pixels, to left and right from actual position. defines cursor width.
        if (selectionHeight > 10)
            curLeft = -1;

        // selectionMesh 
        SelectionMesh.Clear();
        Vector3[] qv = new Vector3[8];
        Color[] qc = new Color[8];
        int[] qt = new int[8];
        for (int i = 0; i < 8; i++)
            qt[i] = i;
        int pp = 0;
        qv[pp++] = new Vector3(s1pos, -1, 0);
        qv[pp++] = new Vector3(s2pos, -1, 0);
        qv[pp++] = new Vector3(s2pos, selectionHeight+1, 0);
        qv[pp++] = new Vector3(s1pos, selectionHeight+1, 0);
        qv[pp++] = new Vector3(cursorPos + curLeft, 0, 0);
        qv[pp++] = new Vector3(cursorPos + curRight, 0, 0);
        qv[pp++] = new Vector3(cursorPos + curRight, selectionHeight, 0);
        qv[pp++] = new Vector3(cursorPos + curLeft, selectionHeight, 0);

        for (int i = 0; i < 8; i++)
        {
            if (i < 4) qc[i] = new Color(0, 0, 0, (s1 != s2) ? 1 : 0); // only show this part if selection rect exists!
            else qc[i] = new Color(1, 1, 1, EditCursor ? 1 : 0);
        }

        SelectionMesh.vertices = qv;
        SelectionMesh.colors = qc;
        SelectionMesh.SetIndices(qt, MeshTopology.Quads, 0);
    }

    public void Update()
    {
        EditRendererA.Text = Prefix + _Value;
        UpdateMesh();

        EditCursorTimer += Time.unscaledDeltaTime;
        if (EditCursorTimer >= 0.25)
        {
            EditCursorTimer = 0;
            EditCursor = !EditCursor;
        }
    }
}