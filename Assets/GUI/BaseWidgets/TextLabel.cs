using UnityEngine;

public class TextLabel : Widget
{
    AllodsTextRenderer EditRendererA;
    GameObject EditObject;
    MeshRenderer EditRenderer;

    public Font Font;
    public string Value;

    public void Start()
    {
        if (Font == null) Font = Fonts.Font1;
        EditRendererA = new AllodsTextRenderer(Font, Font.Align.LeftRight, Width, Height, true);
        EditRendererA.Text = Value;
        EditObject = EditRendererA.GetNewGameObject(0.01f, transform, 100, 1);
        EditObject.transform.localPosition = new Vector3(0, 0, 0);
        EditRenderer = EditObject.GetComponent<MeshRenderer>();
        EditRenderer.material.color = new Color32(214, 214, 214, 255);
    }

    public void Update()
    {
        EditRendererA.Text = Value;
    }
}