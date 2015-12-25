using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewStructure : MapViewObject
{
    public MapLogicStructure LogicStructure
    {
        get
        {
            return (MapLogicStructure)LogicObject;
        }
    }

    // base mesh (TileWidth * TileHeight)
    private MeshRenderer Renderer;
    private MeshFilter Filter;
    private Mesh StructureMesh;

    // overlay mesh. can be null. (TileWidth * (RealHeight - TileHeight))
    private GameObject OverlayObject;
    private MeshRenderer OverlayRenderer;
    private MeshFilter OverlayFilter;
    private Mesh OverlayMesh;


    // shadow mesh. (TileWidth * RealHeight)
    private GameObject ShadowObject = null;
    private MeshRenderer ShadowRenderer = null;
    private MeshFilter ShadowFilter = null;
    private Mesh ShadowMesh = null;

    private Mesh UpdateMesh(Images.AllodsSprite sprite, int frame, Mesh mesh, int x, int y, int w, int h, float shadowOffs, bool first, bool onlyColors)
    {
        int totalAnimCount = 0;
        foreach (char ch in LogicStructure.Class.AnimMask)
            if (ch == '+') totalAnimCount++;
        float shadowOffsPerY = shadowOffs * 32;
        float shadowOffsFromReg = -((float)LogicStructure.Class.ShadowY / LogicStructure.Class.FullHeight * shadowOffs);
        int fw = LogicStructure.Class.TileWidth;
        int fh = LogicStructure.Class.FullHeight;

        if (!onlyColors || first)
        {
            Vector3[] qv = new Vector3[w * h * 4];
            Vector2[] quv = new Vector2[w * h * 4];

            // populate vertices
            int pp = 0;
            int ppt = 0;
            for (int ly = y; ly < y + h; ly++)
            {
                float actualOffsetX = shadowOffsFromReg + shadowOffsPerY * (fh - ly - 1);
                for (int lx = x; lx < x + w; lx++)
                {
                    qv[pp++] = new Vector3(lx * 32 + shadowOffsPerY + actualOffsetX, ly * 32, 0);
                    qv[pp++] = new Vector3(lx * 32 + 32 + shadowOffsPerY + actualOffsetX, ly * 32, 0);
                    qv[pp++] = new Vector3(lx * 32 + 32 + actualOffsetX, ly * 32 + 32, 0);
                    qv[pp++] = new Vector3(lx * 32 + actualOffsetX, ly * 32 + 32, 0);

                    // handle odd structure animation method
                    int realFrame = ly * fw + lx;
                    if (frame > 0 && LogicStructure.Class.AnimMask[realFrame] == '+')
                    {
                        if (fw == 1 && fh == 1)
                            realFrame = frame;
                        else
                        {
                            int preAnimCount = 0;
                            for (int i = realFrame - 1; i >= 0; i--)
                                if (LogicStructure.Class.AnimMask[i] == '+') preAnimCount++;
                            realFrame = fw * fh + totalAnimCount * (frame - 1) + preAnimCount;
                        }
                    }

                    Rect texRect = sprite.AtlasRects[realFrame];
                    quv[ppt++] = new Vector2(texRect.xMin, texRect.yMin);
                    quv[ppt++] = new Vector2(texRect.xMax, texRect.yMin);
                    quv[ppt++] = new Vector2(texRect.xMax, texRect.yMax);
                    quv[ppt++] = new Vector2(texRect.xMin, texRect.yMax);
                }
            }

            mesh.vertices = qv;
            mesh.uv = quv;
        }

        Color[] qc = new Color[w * h * 4];
        int ownDynLight = LogicStructure.GetLightValue();
        int ppc = 0;
        for (int ly = y; ly < y + h; ly++)
        {
            int lTy = ly - (LogicStructure.Class.FullHeight - LogicStructure.Class.TileHeight);
            for (int lx = x; lx < x + w; lx++)
            {
                float cellLight = 0.5f;
                // check if terrain has dynlights. if so, add to current lightness.
                int currentLightAtNode = Mathf.Max(MapLogic.Instance.Nodes[LogicStructure.X + lx, LogicStructure.Y + lTy].DynLight, ownDynLight);
                if (currentLightAtNode > 0)
                    cellLight += (float)currentLightAtNode / 255;

                qc[ppc++] = new Color(cellLight, cellLight, cellLight);
                qc[ppc++] = new Color(cellLight, cellLight, cellLight);
                qc[ppc++] = new Color(cellLight, cellLight, cellLight);
                qc[ppc++] = new Color(cellLight, cellLight, cellLight);
            }
        }

        mesh.colors = qc;

        if (first)
        {
            int[] qt = new int[w * h * 4];
            for (int i = 0; i < qt.Length; i++)
                qt[i] = i;
            mesh.SetIndices(qt, MeshTopology.Quads, 0);
        }
        

        return mesh;
    }

    public void Start()
    {
        name = string.Format("Structure (ID={0}, Tag={1}, Class={2})", LogicStructure.ID, LogicStructure.Tag, LogicStructure.Template.Name);

        Renderer = gameObject.AddComponent<MeshRenderer>();
        Renderer.enabled = false;
        Filter = gameObject.AddComponent<MeshFilter>();
        Filter.mesh = new Mesh();
        transform.localScale = new Vector3(1, 1, 1);

        OverlayObject = Utils.CreateObject();
        OverlayObject.name = "Overlay";
        OverlayObject.transform.parent = transform;
        OverlayRenderer = OverlayObject.AddComponent<MeshRenderer>();
        OverlayRenderer.enabled = false;
        OverlayFilter = OverlayObject.AddComponent<MeshFilter>();
        OverlayFilter.mesh = new Mesh();
        OverlayObject.transform.localScale = new Vector3(1, 1, 1);
        OverlayObject.transform.localPosition = new Vector3(0, 0, -32);

        if (!LogicStructure.Class.Flat)
        {
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
    }

    private bool spriteSet = false;
    private bool oldVisibility = false;
    public void Update()
    {
        if (LogicStructure.GetVisibility() == 0)
        {
            oldVisibility = false;
            Renderer.enabled = false;
            OverlayRenderer.enabled = false;
            if (ShadowRenderer != null) ShadowRenderer.enabled = false;
            return;
        }
        else if (!oldVisibility)
        {
            Renderer.enabled = true;
            OverlayRenderer.enabled = true;
            if (ShadowRenderer != null) ShadowRenderer.enabled = true;
            oldVisibility = true;
            return;
        }

        if (LogicStructure.DoUpdateView)
        {
            Renderer.enabled = true;
            OverlayRenderer.enabled = true;
            if (ShadowRenderer != null) ShadowRenderer.enabled = true;

            StructureClass cls = LogicStructure.Class;
            Images.AllodsSprite sprites = LogicStructure.Class.File.File;

            if (!spriteSet)
            {
                LogicStructure.Class.File.UpdateSprite();
                sprites = LogicStructure.Class.File.File;
                Renderer.material = LogicStructure.Class.File.FileMaterial;
                Renderer.material.SetTexture("_Palette", sprites.OwnPalette);
                Renderer.material.SetFloat("_Lightness", 1f);
                OverlayRenderer.material = LogicStructure.Class.File.FileMaterial;
                OverlayRenderer.material.SetTexture("_Palette", sprites.OwnPalette);
                OverlayRenderer.material.SetFloat("_Lightness", 1f);
                if (ShadowRenderer != null)
                {
                    ShadowRenderer.material = LogicStructure.Class.File.FileMaterial;
                    ShadowRenderer.material.SetTexture("_Palette", sprites.OwnPalette);
                    ShadowRenderer.material.color = new Color(0, 0, 0, 0.5f);
                }
                spriteSet = true;
            }

            int actualFrame = cls.Frames[LogicStructure.CurrentFrame].Frame;
            Vector2 xP = MapView.Instance.MapToScreenCoords(LogicObject.X + 0.5f, LogicObject.Y + 0.5f, LogicStructure.Width, LogicStructure.Height);
            transform.localPosition = new Vector3(xP.x - 16,
                                                  xP.y - 16 - (cls.FullHeight - cls.TileHeight) * 32,
                                                  MakeZFromY(xP.y));
            //Debug.Log(string.Format("{0} {1} {2}", xP.x, sprites.Sprites[0].rect.width, LogicObstacle.Class.CenterX));
            //Renderer.sprite = sprites.Sprites[actualFrame];
            //UpdateMesh(sprites, actualFrame, Filter.mesh, 0, false);
            //UpdateMesh(sprites, actualFrame, ShadowFilter.mesh, 0.3f, true); // 0.3 of sprite height
            StructureMesh = UpdateMesh(sprites, actualFrame, Filter.mesh, 0, cls.FullHeight - cls.TileHeight, cls.TileWidth, cls.TileHeight, 0, (StructureMesh == null), false);
            OverlayMesh = UpdateMesh(sprites, actualFrame, OverlayFilter.mesh, 0, 0, cls.TileWidth, cls.FullHeight - cls.TileHeight, 0, (OverlayMesh == null), false);
            if (ShadowFilter != null) ShadowMesh = UpdateMesh(sprites, actualFrame, ShadowFilter.mesh, 0, 0, cls.TileWidth, cls.FullHeight, 0.3f, (ShadowMesh == null), false);

            LogicStructure.DoUpdateView = false;
        }
        else if (Renderer != null)
        {
            StructureClass cls = LogicStructure.Class;
            Images.AllodsSprite sprites = LogicStructure.Class.File.File;
            int actualFrame = LogicStructure.Class.Frames[LogicStructure.CurrentFrame].Frame;

            StructureMesh = UpdateMesh(sprites, actualFrame, Filter.mesh, 0, cls.FullHeight - cls.TileHeight, cls.TileWidth, cls.TileHeight, 0, (StructureMesh == null), false);
            OverlayMesh = UpdateMesh(sprites, actualFrame, OverlayFilter.mesh, 0, 0, cls.TileWidth, cls.FullHeight - cls.TileHeight, 0, (OverlayMesh == null), false);
        }
    }
}
 