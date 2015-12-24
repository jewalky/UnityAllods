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

    private MeshRenderer Renderer = null;
    private MeshFilter Filter = null;
    private Mesh ObstacleMesh = null;
    private GameObject ShadowObject = null;
    private MeshRenderer ShadowRenderer = null;
    private MeshFilter ShadowFilter = null;
    private Mesh ShadowMesh = null;

    private void UpdateMesh(Images.AllodsSprite sprite, int frame, Mesh mesh, float shadowOffs, bool shadow)
    {
        if (!shadow) shadowOffs = 0;
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

        if ((!shadow && ObstacleMesh == null) || (shadow && ShadowMesh == null))
        {
            Color[] qc = new Color[4];
            qc[0] = qc[1] = qc[2] = qc[3] = (shadow ? new Color(0, 0, 0, 0.5f) : new Color(1, 1, 1, 1));
            mesh.colors = qc;

            int[] qt = new int[6];
            pp = 0;
            for (int i = 0; i < qv.Length; i += 4)
            {
                qt[pp] = i;
                qt[pp + 1] = i + 1;
                qt[pp + 2] = i + 3;
                qt[pp + 3] = i + 3;
                qt[pp + 4] = i + 1;
                qt[pp + 5] = i + 2;
                pp += 6;
            }
            mesh.triangles = qt;
        }

        ObstacleMesh = mesh;
    }

    public void Start()
    {
        name = string.Format("Obstacle (ID={0}, Class={1})", LogicObstacle.ID, LogicObstacle.Class.DescText);
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
        else if (!oldVisibility && !LogicObstacle.DoUpdateView)
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

            LogicObstacle.Class.File.UpdateSprite();
            Renderer.material = LogicObstacle.Class.File.FileMaterial;
            ShadowRenderer.material = LogicObstacle.Class.File.FileMaterial;

            Images.AllodsSprite sprites = LogicObstacle.Class.File.File;

            int actualFrame = LogicObstacle.Class.Frames[LogicObstacle.CurrentFrame].Frame + LogicObstacle.Class.Index;
            Vector2 xP = MapView.Instance.LerpCoords(LogicObject.X + 0.5f, LogicObject.Y + 0.5f);
            transform.localPosition = new Vector3(xP.x - sprites.Sprites[actualFrame].rect.width * LogicObstacle.Class.CenterX,
                                                    xP.y - sprites.Sprites[actualFrame].rect.height * LogicObstacle.Class.CenterY,
                                                    MakeZFromY(xP.y)); // order sprites by y coordinate basically
            //Debug.Log(string.Format("{0} {1} {2}", xP.x, sprites.Sprites[0].rect.width, LogicObstacle.Class.CenterX));
            //Renderer.sprite = sprites.Sprites[actualFrame];
            UpdateMesh(sprites, actualFrame, Filter.mesh, 0, false);
            UpdateMesh(sprites, actualFrame, ShadowFilter.mesh, 0.3f, true); // 0.3 of sprite height

            if (!spriteSet)
            {
                Renderer.material.SetTexture("_Palette", sprites.OwnPalette); // no palette swap for this one
                ShadowRenderer.material.SetTexture("_Palette", sprites.OwnPalette); // no palette swap for this one
                spriteSet = true;
            }

            LogicObstacle.DoUpdateView = false;
        }
    }

    void OnDestroy()
    {
        GameObject.DestroyImmediate(Filter.mesh, true);
        GameObject.DestroyImmediate(ShadowFilter.mesh, true);
    }
}