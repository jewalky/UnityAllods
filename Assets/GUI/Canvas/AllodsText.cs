using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEditor;

[AddComponentMenu("Allods UI/Text")]
public class AllodsText : MaskableGraphic
{
    [SerializeField]
    [TextArea(1, 8)]
    public string Text = "Text Element";
    [SerializeField]
    public bool Wrap = false;
    [SerializeField]
    public Font.Align Align = Font.Align.Left;
    [SerializeField]
    [Range(-1, 16)]
    public int ShadowOffset = -1;
    [SerializeField]
    [Range(0.5f, 5f)]
    public float LineHeight = 1; 

    private AllodsTextRenderer Renderer;

    private void CheckRenderer()
    {
        if (Renderer == null)
        {
            Renderer = new AllodsTextRenderer(Fonts.Font1);
            Renderer.ManualUpdate = true;
        }
    }

    public override Material material
    {
        get
        {
            CheckRenderer();
            return Renderer.Material;
        }

        set
        {
            // do nothing
        }
    }

    public override Texture mainTexture
    {
        get
        {
            CheckRenderer();
            return Renderer.Material.mainTexture;
        }
    }

    private string ResolveText(string input)
    {
        string outs = "";
        int lastTranslate = 0;
        while (true)
        {
            int nextTranslate = input.IndexOf("${", lastTranslate);
            if (nextTranslate >= 0)
            {
                outs += input.Substring(lastTranslate, nextTranslate - lastTranslate);
                int endTranslate = input.IndexOf('}', nextTranslate + 2);
                if (endTranslate < 0)
                    return outs + "<invalid translation>";

                lastTranslate = endTranslate + 1;

                string translateStr = input.Substring(nextTranslate + 2, endTranslate - nextTranslate - 2);
                outs += Locale.TranslateString(translateStr);
            }
            else break;
        }

        if (lastTranslate != input.Length)
            outs += input.Substring(lastTranslate);

        return outs;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        CheckRenderer();

        material.color = this.color;

        Renderer.Text = ResolveText(Text);
        Renderer.Width = (int)rectTransform.rect.width;
        Renderer.Wrapping = Wrap;
        Renderer.Align = Align;
        Renderer.OverrideLineHeight = (int)(Renderer.Font.LineHeight * LineHeight);
        Renderer.UpdateMesh();

        Mesh m = Renderer.Mesh;

        float offsX = rectTransform.rect.width*rectTransform.pivot.x;
        float offsY = rectTransform.rect.height*rectTransform.pivot.y;

        vh.Clear();

        int triOffset = 0;

        if (ShadowOffset >= 0)
        {
            triOffset = m.vertices.Length;
            for (int i = 0; i < m.vertices.Length; i++)
                vh.AddVert(Vector3.Scale(m.vertices[i], new Vector3(100f, 100f, 100f)) - new Vector3(offsX-ShadowOffset, offsY+ShadowOffset, 0), new Color(0, 0, 0, 1), m.uv[i]);
            for (int i = 0; i < m.triangles.Length; i += 3)
                vh.AddTriangle(m.triangles[i], m.triangles[i + 1], m.triangles[i + 2]);
        }

        for (int i = 0; i < m.vertices.Length; i++)
            vh.AddVert(Vector3.Scale(m.vertices[i], new Vector3(100f, 100f, 100f)) - new Vector3(offsX, offsY, 0), m.colors[i], m.uv[i]);
        for (int i = 0; i < m.triangles.Length; i += 3)
            vh.AddTriangle(m.triangles[i] + triOffset, m.triangles[i + 1] + triOffset, m.triangles[i + 2] + triOffset);

    }

}