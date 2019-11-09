using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

public class Font
{
    internal int[] Widths = new int[224];
    internal Images.AllodsSprite CombinedTexture;
    internal Material CombinedMaterial;
    internal int Spacing = 2;
    public readonly int LineHeight = 16;

    public Font(string filename, int spacing, int line_height, int space_width)
    {
        Spacing = spacing;
        LineHeight = line_height;

        string[] fns = filename.Split('.');
        string fnn = string.Join(".", fns, 0, fns.Length - 1);

        // first, load the data file.
        MemoryStream ms = ResourceManager.OpenRead(fnn + ".dat");
        if (ms == null)
        {
            Core.Abort("Couldn't load \"{0}\" as data file for \"{1}\"", fnn + ".dat", filename);
            return;
        }

        int count = (int)ms.Length / 4;
        BinaryReader br = new BinaryReader(ms);
        for (int i = 0; i < 224; i++)
        {
            if (i < count)
                Widths[i] = br.ReadInt32();
            else Widths[i] = 0;
        }

        br.Close();
        Widths[0] = space_width;

        CombinedTexture = Images.LoadSprite(filename);
        CombinedMaterial = new Material(MainCamera.MainShaderPaletted);
        CombinedMaterial.mainTexture = CombinedTexture.Atlas;
    }

    public enum Align
    {
        Left,
        Right,
        Center,
        LeftRight
    }

    private const char MappedReturn = (char)0xFFFE;
    private const char MappedNewline = (char)0xFFFF;
    private char MapChar(char ch)
    {
        if (ch == '\n')
            return MappedNewline;
        if (ch < 0x20)
            return MappedReturn;
        if (ch <= 0x7F && ch >= 0x20)
            return (char)(ch - 32);
        if (ch >= 0x410 && ch <= 0x43F)
            return (char)(ch - 0x380);
        if (ch >= 0x440 && ch <= 0x44F)
            return (char)(ch - 0x370);
        if (ch == 0x401) return (char)0xA0;
        if (ch == 0x402) return (char)0xA1;
        return (char)0x5F;
    }

    private string[] Wrap(string text, int w, bool wrapping)
    {
        List<string> lines = new List<string>();
        if (wrapping)
        {
            string line = "";
            int line_sep = -1;
            int line_wd = 0;
            int[] line_breakers = new int[]{MapChar('.'), MapChar(','),
                                                MapChar('='), MapChar('-'), MapChar('+'),
                                                MapChar('/'), MapChar('*'), MapChar(' ')};

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == MappedNewline) // \n
                {
                    lines.Add(line);
                    line = "";
                    line_sep = -1;
                    line_wd = 0;
                    continue;
                }

                if (line_wd + Width(c) > w)
                {
                    if (line_sep >= 0)
                    {
                        lines.Add(line.Substring(0, line_sep));
                    }
                    else
                    {
                        line_sep = 0;
                        lines.Add(line);
                    }

                    line = line.Substring(line_sep);
                    line_sep = -1;
                    line_wd = 0;
                }

                if (Array.Exists(line_breakers, element => element == c))
                    line_sep = line.Length + 1;

                line += c;
                line_wd += Width(c);
            }

            if (line.Length > 0)
                lines.Add(line);
        }
        else
        {
            string[] sp = text.Split(new char[] { MappedNewline });
            return sp;
        }

        return lines.ToArray();
    }

    internal int Width(char ch)
    {
        if (ch > Widths.Length)
            return 0;
        if (ch == 0)
            return Widths[0];
        return Widths[ch] + Spacing;
    }

    public int Width(string text)
    {
        int line_wd = 0;
        for (int i = 0; i < text.Length; i++)
        {
            line_wd += Width(MapChar(text[i]));
        }

        return line_wd;
    }

    internal void RenderToExistingMesh(Mesh mesh, string text, Align align, int width, int height, bool wrapping, int overrideLineHeight, out int realwidth, out int realheight)
    {
        if (overrideLineHeight < 0)
            overrideLineHeight = LineHeight;

        // todo: wrap text / output
        string text2 = "";
        for (int i = 0; i < text.Length; i++)
            text2 += MapChar(text[i]);

        string[] wrapped = Wrap(text2, width, wrapping);

        // count of quads in mesh == count of characters
        int mesh_quadcnt = 0;
        for (int i = 0; i < wrapped.Length; i++)
        {
            if (wrapped[i].Length > 0)
            {
                if (wrapped[i][wrapped[i].Length - 1] == 0)
                    wrapped[i] = wrapped[i].Substring(0, wrapped[i].Length - 1); // remove last space if any
                mesh_quadcnt += wrapped[i].Length;
            }
        }

        Vector3[] qv = new Vector3[mesh_quadcnt * 4];
        Color[] qc = new Color[mesh_quadcnt * 4];
        Vector2[] quv = new Vector2[mesh_quadcnt * 4];
        int[] qt = new int[mesh_quadcnt * 4];
        int pp = 0;
        int ppc = 0;
        int ppt = 0;

        realwidth = 0;
        float y = 0f;
        for (int i = 0; i < wrapped.Length; i++)
        {
            if (wrapped[i].Length > 0)
            {
                float x = 0f;

                int line_wd2 = 0;
                int line_wd = 0;
                int line_spccnt = 0;

                float spc_width = Widths[0];

                for (int j = 0; j < wrapped[i].Length; j++)
                {
                    char c = wrapped[i][j];
                    int wd = (c != 0) ? Width(c) : (int)spc_width;
                    line_wd += wd;
                    if (c == 0) // space
                        line_spccnt++;
                    else if (c != 0) line_wd2 += wd;
                }

                if (align == Align.LeftRight && line_spccnt > 0 && i != wrapped.Length - 1)
                    spc_width = (float)(width - line_wd2) / line_spccnt;

                if (align == Align.Right)
                    x = width - line_wd;
                else if (align == Align.Center)
                    x = width / 2 - line_wd / 2;

                for (int j = 0; j < wrapped[i].Length; j++)
                {
                    char c = wrapped[i][j];
                    bool is_invisible = (c >= CombinedTexture.Sprites.Length);

                    // c = index in sprite
                    float cx = x / 100;
                    float cy = y / 100;
                    float cw = CombinedTexture.Sprites[is_invisible ? 0 : c].rect.width / 100;
                    float ch = CombinedTexture.Sprites[is_invisible ? 0 : c].rect.height / 100;

                    qv[pp++] = new Vector3(cx, cy);
                    qv[pp++] = new Vector3(cx + cw, cy);
                    qv[pp++] = new Vector3(cx + cw, cy + ch);
                    qv[pp++] = new Vector3(cx, cy + ch);

                    qc[ppc++] = new Color(1, 1, 1, 1);
                    qc[ppc++] = new Color(1, 1, 1, 1);
                    qc[ppc++] = new Color(1, 1, 1, 1);
                    qc[ppc++] = new Color(1, 1, 1, 1);

                    Rect rec = CombinedTexture.AtlasRects[is_invisible ? 0 : c];
                    quv[ppt++] = new Vector2(rec.xMin, rec.yMin);
                    quv[ppt++] = new Vector2(rec.xMax, rec.yMin);
                    quv[ppt++] = new Vector2(rec.xMax, rec.yMax);
                    quv[ppt++] = new Vector2(rec.xMin, rec.yMax);

                    if (!is_invisible)
                    {
                        x += (c != 0) ? (float)Width(c) : spc_width;
                    }
                }

                if (x > realwidth)
                    realwidth = (int)x;
            }

            y += overrideLineHeight;
        }

        realheight = (int)y;

        for (int i = 0; i < qt.Length; i++)
            qt[i] = i;

        mesh.Clear();
        mesh.vertices = qv;
        mesh.colors = qc;
        mesh.uv = quv;
        mesh.SetIndices(qt, MeshTopology.Quads, 0);
    }

    public GameObject Render(string text, Align align, int width, int height, bool wrapping, int overrideLineHeight)
    {
        int tmp;
        return Render(text, align, width, height, wrapping, overrideLineHeight, out tmp, out tmp);
    }

    public GameObject Render(string text, Align align, int width, int height, bool wrapping, int overrideLineHeight, out int realwidth, out int realheight)
    {
        GameObject go = Utils.CreateObject();

        Mesh mesh = new Mesh();
        RenderToExistingMesh(mesh, text, align, width, height, wrapping, overrideLineHeight, out realwidth, out realheight);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mf.mesh = mesh;
        mr.material = CombinedMaterial;
        go.transform.localPosition = new Vector3(0, 0, 0);
        go.transform.localScale = new Vector3(1, 1, 1);
        return go;
    }
}

public class AllodsTextRenderer
{
    private readonly Font _Font;
    private string _Text = "";
    private int _Width;
    private int _ActualWidth;
    private int _Height;
    private Font.Align _Align;
    private bool _Wrapping;
    private Mesh _Mesh;
    private Material _Material;
    private int _OverrideLineHeight;

    public AllodsTextRenderer(Font font, Font.Align align = Font.Align.Left, int width = 0, int height = 0, bool wrapping = false, int overrideLineHeight = -1)
    {
        _Font = font;
        _Mesh = new Mesh();
        _Material = GameObject.Instantiate(_Font.CombinedMaterial);
        _Align = align;
        _ActualWidth = 0;
        _Width = width;
        _Height = height;
        _Wrapping = wrapping;
        _OverrideLineHeight = overrideLineHeight;
    }
    
    private void UpdateMesh()
    {
        _Font.RenderToExistingMesh(_Mesh, _Text, _Align, _Width, _Height, _Wrapping, _OverrideLineHeight, out _ActualWidth, out _Height);
    }

    public string Text
    {
        get { return _Text; }
        set { if (_Text != value) { _Text = value; UpdateMesh(); } }
    }

    public int Width
    {
        get { return _Width; }
        set { if (_Width != value) { _Width = value; if (_Wrapping) UpdateMesh(); } }
    }

    public int ActualWidth
    {
        get { return _ActualWidth; }
    }

    public int Height
    {
        get { return _Height; }
        set { if (_Height != value) { _Height = value; } } // tbh this doesn't affect anything, useless value
    }

    public Font.Align Align
    {
        get { return _Align; }
        set { if (_Align != value) { _Align = value; UpdateMesh(); } } // tbh this doesn't affect anything, useless value
    }

    public bool Wrapping
    {
        get { return _Wrapping; }
        set { if (_Wrapping != value) { _Wrapping = value; UpdateMesh(); } }
    }

    public Mesh Mesh
    {
        get { return _Mesh; }
    }

    public Font Font
    {
        get { return _Font; }
    }

    public Material Material
    {
        get { return _Material; }
    }

    public GameObject GetNewGameObject(float shadowPos = 0, Transform parent_transform = null, float scale = 1f, float shadowZOffs = 0.01f)
    {
        GameObject go = Utils.CreateObject();
        go.AddComponent<MeshFilter>().mesh = _Mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.material = _Material;
        go.name = "AllodsTextRenderer$GetNewGameObject";
        go.transform.parent = parent_transform;
        go.transform.localPosition = new Vector3(0, 0, 0);
        go.transform.localScale = new Vector3(scale, scale, 1);
        if (shadowPos == 0) return go;

        GameObject shadow = GameObject.Instantiate(go);
        shadow.name = "AllodsTextRenderer$GetNewGameObject$Shadow";
        shadow.transform.parent = go.transform;
        shadow.transform.localPosition = new Vector3(shadowPos, shadowPos, shadowZOffs);
        shadow.transform.localScale = new Vector3(1, 1, 1);
        shadow.GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0, 1);
        return go;
    }

    public void Destroy()
    {
        GameObject.Destroy(_Mesh);
    }

    public void DestroyImmediate()
    {
        GameObject.DestroyImmediate(_Mesh);
    }
}

public class Fonts
{
    private static Font ObjFont1 = null;
    private static Font ObjFont2 = null;
    private static Font ObjFont3 = null;
    private static Font ObjFont4 = null;

    public static Font Font1
    {
        get
        {
            LoadAll();
            return ObjFont1;
        }
    }

    public static Font Font2
    {
        get
        {
            LoadAll();
            return ObjFont2;
        }
    }

    public static Font Font3
    {
        get
        {
            LoadAll();
            return ObjFont3;
        }
    }

    public static Font Font4
    {
        get
        {
            LoadAll();
            return ObjFont4;
        }
    }

    private static void LoadAll()
    {
        if (ObjFont1 == null) ObjFont1 = new Font("graphics/font1/font1.16", 2, 16, 8);
        if (ObjFont2 == null) ObjFont2 = new Font("graphics/font2/font2.16", 2, 10, 6);
        if (ObjFont3 == null) ObjFont3 = new Font("graphics/font3/font3.16", 1, 6, 4);
        if (ObjFont4 == null) ObjFont4 = new Font("graphics/font4/font4.16a", 2, 16, 8);
    }
}
