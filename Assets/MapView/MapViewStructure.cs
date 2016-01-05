using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewStructure : MapViewObject, IMapViewSelectable, IMapViewSelfie, IObjectManualUpdate
{
    public MapStructure LogicStructure
    {
        get
        {
            return (MapStructure)LogicObject;
        }
    }

    public MapObject GetObject() { return LogicObject; }

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


    // infowindow stuff
    private GameObject TexObject;
    private MeshRenderer TexRenderer;
    private Material TexMaterial;


    private Mesh UpdateMesh(Images.AllodsSprite sprite, int frame, Mesh mesh, int x, int y, int w, int h, float shadowOffs, bool first, bool onlyColors)
    {
        bool isBridge = LogicStructure.Class.VariableSize;
        int totalAnimCount = 0;
        foreach (char ch in LogicStructure.Class.AnimMask)
            if (ch == '+') totalAnimCount++;
        float shadowOffsPerY = shadowOffs * 32;
        float shadowOffsFromReg = -((float)LogicStructure.Class.ShadowY / LogicStructure.Class.FullHeight * shadowOffs);
        int fw = isBridge ? LogicStructure.Width : LogicStructure.Class.TileWidth;
        int fh = isBridge ? LogicStructure.Height : LogicStructure.Class.FullHeight;

        if (isBridge)
        {
            w = fw;
            h = fh;
            x = 0;
            y = 0;
        }

        bool dead = (!LogicStructure.Class.Indestructible && LogicStructure.Health <= 0);

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

                    int realFrame;
                    if (!isBridge)
                    {
                        // handle odd structure animation method
                        realFrame = ly * fw + lx;
                        if (dead)
                        {
                            realFrame = sprite.Sprites.Length - (fw * fh) + realFrame; // last frame is always full flat frame
                        }
                        else if (frame > 0 && LogicStructure.Class.AnimMask[realFrame] == '+')
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
                    }
                    else
                    {
                        // handle resizable things (normally bridges)
                        int bx = 1;
                        int by = 1;

                        if (lx == 0)
                            bx = 0;
                        else if (lx == fw-1)
                            bx = 2;

                        if (ly == 0)
                            by = 0;
                        else if (ly == fh-1)
                            by = 2;

                        realFrame = by * 3 + bx;
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
    public void OnUpdate()
    {
        if (Renderer == null)
            return;

        if (LogicStructure.GetVisibility() == 0)
        {
            oldVisibility = false;
            Renderer.enabled = false;
            OverlayRenderer.enabled = false;
            if (ShadowRenderer != null) ShadowRenderer.enabled = false;
            return;
        }
        else if (!oldVisibility && !LogicStructure.DoUpdateView)
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

            if (LogicStructure.Class.VariableSize)
            {
                StructureMesh = UpdateMesh(sprites, 0, Filter.mesh, 0, 0, LogicStructure.Width, LogicStructure.Height, 0, (StructureMesh == null), false);
            }
            else
            {
                StructureMesh = UpdateMesh(sprites, actualFrame, Filter.mesh, 0, cls.FullHeight - cls.TileHeight, cls.TileWidth, cls.TileHeight, 0, (StructureMesh == null), false);
                OverlayMesh = UpdateMesh(sprites, actualFrame, OverlayFilter.mesh, 0, 0, cls.TileWidth, cls.FullHeight - cls.TileHeight, 0, (OverlayMesh == null), false);
                if (ShadowFilter != null) ShadowMesh = UpdateMesh(sprites, actualFrame, ShadowFilter.mesh, 0, 0, cls.TileWidth, cls.FullHeight, 0.3f, (ShadowMesh == null), false);
            }

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

    public void OnDestroy()
    {
        if (Filter != null && Filter.mesh != null)
            DestroyImmediate(Filter.mesh, true);
        if (OverlayFilter != null && OverlayFilter.mesh != null)
            DestroyImmediate(OverlayFilter.mesh, true);
        if (ShadowFilter != null && ShadowFilter != null && ShadowFilter.mesh != null)
            DestroyImmediate(ShadowFilter.mesh, true);
    }

    public bool IsSelected(int x, int y)
    {
        if (LogicStructure.Class.Flat || (LogicStructure.Class.Indestructible && !LogicStructure.Class.Usable))
            return false; // these can't be selected and don't have a picture
        if (LogicStructure.GetVisibility() < 1)
            return false;
        int cx = x - (int)transform.localPosition.x;
        int cy = y - (int)transform.localPosition.y;
        if (cx > LogicStructure.Class.SelectionX1 &&
            cx < LogicStructure.Class.SelectionX2 &&
            cy > LogicStructure.Class.SelectionY1 &&
            cy < LogicStructure.Class.SelectionY2) return true;
        return false;
    }

    public bool ProcessEventPic(Event e)
    {
        return false;
    }

    public bool ProcessEventInfo(Event e)
    {
        return false;
    }

    public void DisplayPic(bool on, Transform parent)
    {
        if (on)
        {
            // load infowindow texture.
            if (LogicStructure.Class.PictureFile == null)
                LogicStructure.Class.PictureFile = Images.LoadImage(LogicStructure.Class.Picture, 0, Images.ImageType.AllodsBMP);
            // init infowindow
            if (TexMaterial == null)
                TexMaterial = new Material(MainCamera.MainShader);
            TexObject = Utils.CreatePrimitive(PrimitiveType.Quad);
            TexRenderer = TexObject.GetComponent<MeshRenderer>();
            TexRenderer.material = TexMaterial;
            TexRenderer.material.mainTexture = LogicStructure.Class.PictureFile;
            TexRenderer.enabled = true;
            TexRenderer.transform.parent = parent;
            TexRenderer.transform.localPosition = new Vector3((float)LogicStructure.Class.PictureFile.width / 2 + 16,
                                                         (float)LogicStructure.Class.PictureFile.height / 2 + 2, -0.01f);
            TexRenderer.transform.localScale = new Vector3(LogicStructure.Class.PictureFile.width,
                                                           LogicStructure.Class.PictureFile.height, 1);
        }
        else
        {
            Destroy(TexObject);
            TexObject = null;
            TexRenderer = null;
        }
    }

    public void DisplayInfo(bool on, Transform parent)
    {
        
    }
}
 