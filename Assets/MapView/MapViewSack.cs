using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewSack : MapViewObject, IObjectManualUpdate
{
    private MapSack LogicSack
    {
        get
        {
            return (MapSack)LogicObject;
        }
    }

    private MeshRenderer Renderer;
    private MeshFilter Filter;
    private Mesh ObstacleMesh;

    private GameObject ShadowObject;
    private MeshRenderer ShadowRenderer;
    private MeshFilter ShadowFilter;
    private Mesh ShadowMesh;

    private Mesh UpdateMesh(Images.AllodsSprite sprite, int frame, Mesh mesh, float shadowOffs, bool first)
    {
        Texture2D sTex = sprite.Atlas;
        float sW = sprite.Sprites[frame].rect.width;
        float sH = sprite.Sprites[frame].rect.height;
        float tMinX = sprite.AtlasRects[frame].xMin;
        float tMinY = sprite.AtlasRects[frame].yMin;
        float tMaxX = sprite.AtlasRects[frame].xMax;
        float tMaxY = sprite.AtlasRects[frame].yMax;

        float centerY = 0.9f;

        float shadowOffsReal = shadowOffs * sH;
        float shadowOffsXLeft = -shadowOffsReal * (1f - centerY);

        Vector3[] qv = new Vector3[4];
        int pp = 0;
        qv[pp++] = new Vector3(shadowOffsReal, 0, 0);
        qv[pp++] = new Vector3(shadowOffsReal + sW, 0, 0);
        qv[pp++] = new Vector3(shadowOffsXLeft + sW, sH, 0);
        qv[pp++] = new Vector3(shadowOffsXLeft, sH, 0);

        Vector2[] quv = new Vector2[4];
        quv[0] = new Vector2(tMinX, tMinY);
        quv[1] = new Vector2(tMaxX, tMinY);
        quv[2] = new Vector2(tMaxX, tMaxY);
        quv[3] = new Vector2(tMinX, tMaxY);

        mesh.vertices = qv;
        mesh.uv = quv;

        if (first)
        {
            Color[] qc = new Color[4];
            qc[0] = qc[1] = qc[2] = qc[3] = new Color(1, 1, 1, 1);
            mesh.colors = qc;

            int[] qt = new int[4];
            for (int i = 0; i < qt.Length; i++)
                qt[i] = i;
            mesh.SetIndices(qt, MeshTopology.Quads, 0);
        }

        Renderer.material.mainTexture = sTex;
        ShadowRenderer.material.mainTexture = sTex;

        return mesh;
    }

    public void Start()
    {
        name = string.Format("Sack (ID={0}, Price={1})", LogicSack.ID, LogicSack.Pack.Price);
        // let's give ourselves a sprite renderer first.
        Renderer = gameObject.AddComponent<MeshRenderer>();
        Renderer.enabled = false;
        Filter = gameObject.AddComponent<MeshFilter>();
        Filter.mesh = new Mesh();
        transform.localScale = new Vector3(1, 1, 1);

        ShadowObject = Utils.CreateObject();
        ShadowObject.name = "Shadow";
        ShadowObject.transform.parent = transform;
        ShadowRenderer = ShadowObject.AddComponent<MeshRenderer>();
        ShadowRenderer.enabled = false;
        ShadowFilter = ShadowObject.AddComponent<MeshFilter>();
        ShadowFilter.mesh = new Mesh();
        ShadowObject.transform.localScale = new Vector3(1, 1, 1);
        ShadowObject.transform.localPosition = new Vector3(0, 0, 16);
    }

    private static Images.AllodsSprite _SackSprite = null;
    private bool spriteSet = false;
    private bool oldVisibility = false;
    public void OnUpdate()
    {
        if (Renderer == null)
            return;

        if (LogicSack.GetVisibility() < 2)
        {
            oldVisibility = false;
            Renderer.enabled = false;
            ShadowRenderer.enabled = false;
            return;
        }
        else if (!oldVisibility)
        {
            Renderer.enabled = true;
            ShadowRenderer.enabled = true;
            oldVisibility = true;
            return;
        }

        if (LogicSack.DoUpdateView)
        {
            Renderer.enabled = true;
            ShadowRenderer.enabled = true;

            if (_SackSprite == null)
                _SackSprite = Images.Load256("graphics/backpack/sprites.256");

            Images.AllodsSprite sprites = _SackSprite;

            if (!spriteSet)
            {
                Renderer.material = new Material(MainCamera.MainShaderPaletted);
                Renderer.material.SetTexture("_Palette", sprites.OwnPalette); // no palette swap for this one
                ShadowRenderer.material = Renderer.material;
                ShadowRenderer.material.color = new Color(0, 0, 0, 0.5f);
                spriteSet = true;
            }

            // http://archive.allods2.eu/homeunix/article_sacks.php.htm
            int actualFrame = 0;
            if (LogicSack.Pack.Price >= 100000)
                actualFrame = 5;
            else if (LogicSack.Pack.Price >= 10000)
                actualFrame = 4;
            else if (LogicSack.Pack.Price >= 1000)
                actualFrame = 3;
            else if (LogicSack.Pack.Price >= 100)
                actualFrame = 2;
            else if (LogicSack.Pack.Price >= 10)
                actualFrame = 1;
            // always centered
            Vector2 xP = MapView.Instance.MapToScreenCoords(LogicObject.X + 0.5f, LogicObject.Y + 0.5f, 1, 1);
            transform.localPosition = new Vector3(xP.x - (float)sprites.Sprites[actualFrame].rect.width * 0.5f,
                                                    xP.y - (float)sprites.Sprites[actualFrame].rect.height * 0.5f,
                                                    MakeZFromY(xP.y) + 32); // order sprites by y coordinate basically
            //Debug.Log(string.Format("{0} {1} {2}", xP.x, sprites.Sprites[0].rect.width, LogicObstacle.Class.CenterX));
            //Renderer.sprite = sprites.Sprites[actualFrame];
            ObstacleMesh = UpdateMesh(sprites, actualFrame, Filter.mesh, 0, (ObstacleMesh == null));
            ShadowMesh = UpdateMesh(sprites, actualFrame, ShadowFilter.mesh, 0.3f, (ShadowMesh == null)); // 0.3 of sprite height

            LogicSack.DoUpdateView = false;
        }
    }

    void OnDestroy()
    {
        if (Filter != null && Filter.mesh != null)
            DestroyImmediate(Filter.mesh, true);
        if (ShadowFilter != null && ShadowFilter.mesh != null)
            DestroyImmediate(ShadowFilter.mesh, true);
    }
}