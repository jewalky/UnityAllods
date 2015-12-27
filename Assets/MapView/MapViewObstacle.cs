using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewObstacle : MapViewObject
{
    public MapLogicObstacle LogicObstacle
    {
        get
        {
            return (MapLogicObstacle)LogicObject;
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
        Rect texRect = sprite.AtlasRects[frame];

        float sW = sprite.Sprites[frame].rect.width;
        float sH = sprite.Sprites[frame].rect.height;

        float shadowOffsReal = shadowOffs * sH;
        float shadowOffsXLeft = -shadowOffsReal * (1f - LogicObstacle.Class.CenterY);

        Vector3[] qv = new Vector3[4];
        int pp = 0;
        qv[pp++] = new Vector3(shadowOffsReal, 0, 0);
        qv[pp++] = new Vector3(shadowOffsReal + sW, 0, 0);
        qv[pp++] = new Vector3(shadowOffsXLeft + sW, sH, 0);
        qv[pp++] = new Vector3(shadowOffsXLeft, sH, 0);

        Vector2[] quv = new Vector2[4];
        quv[0] = new Vector2(texRect.xMin, texRect.yMin);
        quv[1] = new Vector2(texRect.xMax, texRect.yMin);
        quv[2] = new Vector2(texRect.xMax, texRect.yMax);
        quv[3] = new Vector2(texRect.xMin, texRect.yMax);

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

        return mesh;
    }

    public void Start()
    {
        if (LogicObstacle.Class != null)
            name = string.Format("Obstacle (ID={0}, Class={1})", LogicObstacle.ID, LogicObstacle.Class.DescText);
        else name = string.Format("Obstacle (ID={0}, Class=<INVALID>)", LogicObstacle.ID);
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

    private bool spriteSet = false;
    private bool oldVisibility = false;
    public void Update()
    {
        if (LogicObstacle.GetVisibility() == 0)
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

        if (LogicObstacle.DoUpdateView)
        {
            Renderer.enabled = true;
            ShadowRenderer.enabled = true;

            Images.AllodsSprite sprites = LogicObstacle.Class.File.File;

            if (!spriteSet)
            {
                LogicObstacle.Class.File.UpdateSprite();
                sprites = LogicObstacle.Class.File.File;
                Renderer.material = LogicObstacle.Class.File.FileMaterial;
                Renderer.material.SetTexture("_Palette", sprites.OwnPalette); // no palette swap for this one
                ShadowRenderer.material = LogicObstacle.Class.File.FileMaterial;
                ShadowRenderer.material.color = new Color(0, 0, 0, 0.5f);
                ShadowRenderer.material.SetTexture("_Palette", sprites.OwnPalette); // no palette swap for this one
                spriteSet = true;
            }

            int actualFrame = LogicObstacle.Class.Frames[LogicObstacle.CurrentFrame].Frame + LogicObstacle.Class.Index;
            Vector2 xP = MapView.Instance.MapToScreenCoords(LogicObject.X + 0.5f, LogicObject.Y + 0.5f, 1, 1);
            transform.localPosition = new Vector3(xP.x - sprites.Sprites[actualFrame].rect.width * LogicObstacle.Class.CenterX,
                                                    xP.y - sprites.Sprites[actualFrame].rect.height * LogicObstacle.Class.CenterY,
                                                    MakeZFromY(xP.y)); // order sprites by y coordinate basically
            //Debug.Log(string.Format("{0} {1} {2}", xP.x, sprites.Sprites[0].rect.width, LogicObstacle.Class.CenterX));
            //Renderer.sprite = sprites.Sprites[actualFrame];
            ObstacleMesh = UpdateMesh(sprites, actualFrame, Filter.mesh, 0, (ObstacleMesh == null));
            ShadowMesh = UpdateMesh(sprites, actualFrame, ShadowFilter.mesh, 0.3f, (ShadowMesh == null)); // 0.3 of sprite height

            LogicObstacle.DoUpdateView = false;
        }
    }

    void OnDestroy()
    {
        if (Filter.mesh != null)
            DestroyImmediate(Filter.mesh, true);
        if (ShadowFilter.mesh != null)
            DestroyImmediate(ShadowFilter.mesh, true);
    }
}