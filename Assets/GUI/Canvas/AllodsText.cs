using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

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
    public Color Color = new Color(1, 1, 1, 1);
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

    public override Texture mainTexture
    {
        get
        {
            CheckRenderer();
            return Renderer.Material.mainTexture;
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        CheckRenderer();

        material.color = Color;

        Renderer.Text = Text;
        Renderer.Width = (int)rectTransform.rect.width;
        Renderer.Wrapping = Wrap;
        Renderer.Align = Align;
        Renderer.OverrideLineHeight = (int)(Renderer.Font.LineHeight * LineHeight);
        Renderer.UpdateMesh();

        Mesh m = Renderer.Mesh;

        float offsX = rectTransform.rect.width*rectTransform.pivot.x;
        float offsY = -rectTransform.rect.height * (1f - rectTransform.pivot.y);

        vh.Clear();

        int triOffset = 0;

        if (ShadowOffset >= 0)
        {
            triOffset = m.vertices.Length;
            for (int i = 0; i < m.vertices.Length; i++)
                vh.AddVert(Vector3.Scale(m.vertices[i], new Vector3(100f, -100f, 100f)) - new Vector3(offsX-ShadowOffset, offsY+ShadowOffset, 0), new Color(0, 0, 0, 1), m.uv[i]);
            for (int i = 0; i < m.triangles.Length; i += 3)
                vh.AddTriangle(m.triangles[i], m.triangles[i + 1], m.triangles[i + 2]);
        }

        for (int i = 0; i < m.vertices.Length; i++)
            vh.AddVert(Vector3.Scale(m.vertices[i], new Vector3(100f, -100f, 100f)) - new Vector3(offsX, offsY, 0), m.colors[i], m.uv[i]);
        for (int i = 0; i < m.triangles.Length; i += 3)
            vh.AddTriangle(m.triangles[i] + triOffset, m.triangles[i + 1] + triOffset, m.triangles[i + 2] + triOffset);

    }
}