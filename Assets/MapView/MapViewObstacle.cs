using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewObstacle : MapViewObject, IObjectManualUpdate
{
    private MapObstacle LogicObstacle
    {
        get
        {
            return (MapObstacle)LogicObject;
        }
    }

    private MeshRenderer Renderer;
    private MeshFilter Filter;
    private Mesh ObstacleMesh;

    private GameObject ShadowObject;
    private MeshRenderer ShadowRenderer;
    private MeshFilter ShadowFilter;
    private Mesh ShadowMesh;

    private Mesh UpdateMesh(Images.AllodsSpriteSeparate sprite, Images.AllodsSpriteSeparate spriteB, int frame, Mesh mesh, float shadowOffs, bool first)
    {
        // main
        Texture2D sTex = sprite.Frames[frame].Texture;
        float sW = sprite.Frames[frame].Width;
        float sH = sprite.Frames[frame].Height;
        float tMaxX = sW / sTex.width;
        float tMaxY = sH / sTex.height;

        // additional
        Texture2D sTexB = (spriteB != null) ? spriteB.Frames[frame].Texture : null;
        float sWB = (spriteB != null) ? spriteB.Frames[frame].Width : 0;
        float sHB = (spriteB != null) ? spriteB.Frames[frame].Height : 0;
        float tMaxXB = (spriteB != null) ? (sWB / sTexB.width) : 0;
        float tMaxYB = (spriteB != null) ? (sHB / sTexB.height) : 0;

        // main
        float shadowOffsReal = shadowOffs * sH;
        float shadowOffsXLeft = -shadowOffsReal * (1f - LogicObstacle.Class.CenterY);

        // additional
        float shadowOffsRealB = shadowOffs * sHB;
        float shadowOffsXLeftB = -shadowOffsRealB * (1f - LogicObstacle.Class.CenterY);

        //
        int vertexCount = (spriteB != null) ? 8 : 4;

        Vector3[] qv = new Vector3[vertexCount];
        int pp = 0;
        qv[pp++] = new Vector3(shadowOffsReal, 0, 0);
        qv[pp++] = new Vector3(shadowOffsReal + sW, 0, 0);
        qv[pp++] = new Vector3(shadowOffsXLeft + sW, sH, 0);
        qv[pp++] = new Vector3(shadowOffsXLeft, sH, 0);

        if (spriteB != null)
        {
            qv[pp++] = new Vector3(shadowOffsRealB, 0, 0);
            qv[pp++] = new Vector3(shadowOffsRealB + sWB, 0, 0);
            qv[pp++] = new Vector3(shadowOffsXLeftB + sWB, sHB, 0);
            qv[pp++] = new Vector3(shadowOffsXLeftB, sHB, 0);
        }

        Vector2[] quv = new Vector2[vertexCount];
        quv[0] = new Vector2(0, 0);
        quv[1] = new Vector2(tMaxX, 0);
        quv[2] = new Vector2(tMaxX, tMaxY);
        quv[3] = new Vector2(0, tMaxY);

        if (spriteB != null)
        {
            quv[4] = new Vector2(0, 0);
            quv[5] = new Vector2(tMaxXB, 0);
            quv[6] = new Vector2(tMaxXB, tMaxYB);
            quv[7] = new Vector2(0, tMaxYB);
        }

        mesh.subMeshCount = (spriteB != null) ? 2 : 1;

        mesh.vertices = qv;
        mesh.uv = quv;

        Color[] qc = new Color[vertexCount];
        qc[0] = qc[1] = qc[2] = qc[3] = new Color(1, 1, 1, 1);
        if (spriteB != null)
            qc[4] = qc[5] = qc[6] = qc[7] = new Color(1, 1, 1, 0.5f);
        mesh.colors = qc;

        int[] qt = new int[4];
        for (int i = 0; i < qt.Length; i++)
            qt[i] = i;
        mesh.SetIndices(qt, MeshTopology.Quads, 0);

        if (spriteB != null)
        {
            int[] qtB = new int[4];
            for (int i = 0; i < qtB.Length; i++)
                qtB[i] = 4 + i;
            mesh.SetIndices(qtB, MeshTopology.Quads, 1);
        }

        Renderer.materials[0].mainTexture = sTex;
        ShadowRenderer.materials[0].mainTexture = sTex;

        if (spriteB != null)
        {
            Renderer.materials[1].mainTexture = sTexB;
            ShadowRenderer.materials[1].mainTexture = sTexB;
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

        OnUpdate();
    }

    private bool spriteSet = false;
    private bool oldVisibility = false;
    public void OnUpdate()
    {
        if (Renderer == null)
            return;

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

            Images.AllodsSpriteSeparate sprites = LogicObstacle.Class.File.File;
            Images.AllodsSpriteSeparate spritesB = LogicObstacle.Class.File.FileB;

            if (sprites == null)
            {
                LogicObstacle.Class.File.UpdateSprite();
                sprites = LogicObstacle.Class.File.File;
                spritesB = LogicObstacle.Class.File.FileB;
            }

            if (!MapView.Instance.SpritesBEnabled)
                spritesB = null;

            int newMaterialCount = (spritesB != null) ? 2 : 1;

            if (!spriteSet || Renderer.materials.Length != newMaterialCount)
            {
                List<Material> newMats = new List<Material>();
                List<Material> newMatsShadow = new List<Material>();
                for (int i = 0; i < newMaterialCount; i++)
                {
                    newMats.Add(new Material(MainCamera.MainShaderPaletted));
                    newMatsShadow.Add(new Material(MainCamera.MainShaderPaletted));
                }
                Renderer.materials = newMats.ToArray();
                ShadowRenderer.materials = newMats.ToArray();

                for (int i = 0; i < newMaterialCount; i++)
                {
                    Renderer.materials[i].SetTexture("_Palette", sprites.OwnPalette); // no palette swap for this one
                    ShadowRenderer.materials[i].color = new Color(0, 0, 0, 0.5f);
                }

                spriteSet = true;
            }

            for (int i = 0; i < newMaterialCount; i++)
            {
                Renderer.materials[i].SetFloat("_Lightness", 0.5f + Mathf.Min(0.75f, (float)MapLogic.Instance.Nodes[LogicObstacle.X, LogicObstacle.Y].DynLight / 255));
            }

            int actualFrame = LogicObstacle.Class.Frames[LogicObstacle.CurrentFrame].Frame + LogicObstacle.Class.Index;
            Vector2 xP = MapView.Instance.MapToScreenCoords(LogicObject.X + 0.5f, LogicObject.Y + 0.5f, 1, 1);
            transform.localPosition = new Vector3(xP.x - (float)sprites.Frames[actualFrame].Width * LogicObstacle.Class.CenterX,
                                                    xP.y - (float)sprites.Frames[actualFrame].Height * LogicObstacle.Class.CenterY,
                                                    MakeZFromY(xP.y)); // order sprites by y coordinate basically
            //Debug.Log(string.Format("{0} {1} {2}", xP.x, sprites.Sprites[0].rect.width, LogicObstacle.Class.CenterX));
            //Renderer.sprite = sprites.Sprites[actualFrame];
            ObstacleMesh = UpdateMesh(sprites, spritesB, actualFrame, Filter.mesh, 0, (ObstacleMesh == null));
            ShadowMesh = UpdateMesh(sprites, spritesB, actualFrame, ShadowFilter.mesh, 0.3f, (ShadowMesh == null)); // 0.3 of sprite height

            LogicObstacle.DoUpdateView = false;
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