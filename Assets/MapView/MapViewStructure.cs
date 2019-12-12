using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewStructure : MapViewObject, IMapViewSelectable, IMapViewSelfie, IObjectManualUpdate
{
    private MapStructure LogicStructure
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
    private static GameObject TexObject;
    private static MeshRenderer TexRenderer;
    private static Material TexMaterial;

    private Mesh UpdateMesh(Images.AllodsSprite sprite, Images.AllodsSprite spriteB, int frame, Mesh mesh, int x, int y, int w, int h, float shadowOffs, bool first, bool onlyColors)
    {
        Texture2D sTex = sprite.Atlas;
        Texture2D sTexB = (spriteB != null) ? spriteB.Atlas : null;

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

        int vertexCount = (spriteB != null) ? 8 : 4;

        int subMeshCount = (spriteB != null) ? 2 : 1;

        if (!onlyColors || first || (mesh.subMeshCount != subMeshCount))
        {
            mesh.subMeshCount = subMeshCount;

            Vector3[] qv = new Vector3[w * h * vertexCount];
            Vector2[] quv = new Vector2[w * h * vertexCount];

            // populate vertices
            int pp = 0;
            int ppt = 0;
            for (int j = 0; j < vertexCount / 4; j++)
            {
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
                            else if (lx == fw - 1)
                                bx = 2;

                            if (ly == 0)
                                by = 0;
                            else if (ly == fh - 1)
                                by = 2;

                            realFrame = by * 3 + bx;
                        }

                        Rect texRect = (j == 0) ? sprite.AtlasRects[realFrame] : spriteB.AtlasRects[realFrame];

                        quv[ppt++] = new Vector2(texRect.xMin, texRect.yMin);
                        quv[ppt++] = new Vector2(texRect.xMax, texRect.yMin);
                        quv[ppt++] = new Vector2(texRect.xMax, texRect.yMax);
                        quv[ppt++] = new Vector2(texRect.xMin, texRect.yMax);
                    }
                }
            }

            mesh.vertices = qv;
            mesh.uv = quv;
        }

        Color[] qc = new Color[w * h * vertexCount];
        int ownDynLight = LogicStructure.GetLightValue();
        int ppc = 0;
        for (int i = 0; i < vertexCount / 4; i++)
        {
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
                    if (cellLight > 1f)
                        cellLight = 1f;

                    if (i == 0)
                    {
                        qc[ppc++] = new Color(cellLight, cellLight, cellLight, 1f);
                        qc[ppc++] = new Color(cellLight, cellLight, cellLight, 1f);
                        qc[ppc++] = new Color(cellLight, cellLight, cellLight, 1f);
                        qc[ppc++] = new Color(cellLight, cellLight, cellLight, 1f);
                    }
                    else
                    {
                        qc[ppc++] = new Color(cellLight, cellLight, cellLight, 0.5f);
                        qc[ppc++] = new Color(cellLight, cellLight, cellLight, 0.5f);
                        qc[ppc++] = new Color(cellLight, cellLight, cellLight, 0.5f);
                        qc[ppc++] = new Color(cellLight, cellLight, cellLight, 0.5f);
                    }
                }
            }
        }

        mesh.colors = qc;

        int[] qt = new int[w * h * 4];
        for (int i = 0; i < qt.Length; i++)
            qt[i] = i;
        mesh.SetIndices(qt, MeshTopology.Quads, 0);

        if (spriteB != null)
        {
            int[] qtB = new int[w * h * 4];
            for (int i = 0; i < qtB.Length; i++)
                qtB[i] = qt.Length+i;
            mesh.SetIndices(qtB, MeshTopology.Quads, 1);
        }

        Renderer.materials[0].mainTexture = sTex;
        OverlayRenderer.materials[0].mainTexture = sTex;
        if (ShadowRenderer != null)
            ShadowRenderer.materials[0].mainTexture = sTex;

        if (spriteB != null)
        {
            Renderer.materials[1].mainTexture = sTexB;
            OverlayRenderer.materials[1].mainTexture = sTexB;
            if (ShadowRenderer != null)
                ShadowRenderer.materials[1].mainTexture = sTexB;
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

        OnUpdate();
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
            Images.AllodsSprite spritesB = LogicStructure.Class.File.FileB;

            if (sprites == null)
            {
                LogicStructure.Class.File.UpdateSprite();
                sprites = LogicStructure.Class.File.File;
                spritesB = LogicStructure.Class.File.FileB;
            }

            if (!MapView.Instance.SpritesBEnabled)
                spritesB = null;

            int newMaterialCount = (spritesB != null) ? 2 : 1;

            if (!spriteSet || Renderer.materials.Length != newMaterialCount)
            {

                List<Material> newMats = new List<Material>();
                List<Material> newMatsShadow = new List<Material>();
                List<Material> newMatsOverlay = new List<Material>();
                for (int i = 0; i < newMaterialCount; i++)
                {
                    newMats.Add(new Material(MainCamera.MainShaderPaletted));
                    newMatsOverlay.Add(new Material(MainCamera.MainShaderPaletted));
                    if (ShadowRenderer != null)
                        newMatsShadow.Add(new Material(MainCamera.MainShaderPaletted));
                }
                Renderer.materials = newMats.ToArray();
                OverlayRenderer.materials = newMatsOverlay.ToArray();
                if (ShadowRenderer != null)
                    ShadowRenderer.materials = newMats.ToArray();

                for (int i = 0; i < newMaterialCount; i++)
                {
                    Renderer.materials[i].SetTexture("_Palette", sprites.OwnPalette);
                    Renderer.materials[i].SetFloat("_Lightness", 1f);
                    OverlayRenderer.materials[i].SetTexture("_Palette", sprites.OwnPalette);
                    OverlayRenderer.materials[i].SetFloat("_Lightness", 1f);
                    if (ShadowRenderer != null)
                    {
                        ShadowRenderer.materials[i].SetTexture("_Palette", sprites.OwnPalette);
                        ShadowRenderer.materials[i].color = new Color(0, 0, 0, 0.5f);
                    }
                }
                spriteSet = true;
            }

            int actualFrame = cls.Frames[LogicStructure.CurrentFrame].Frame;
            Vector2 xP = MapView.Instance.MapToScreenCoords(LogicObject.X + 0.5f, LogicObject.Y + 0.5f, LogicStructure.Width, LogicStructure.Height);
            transform.localPosition = new Vector3(xP.x - 16,
                                                  xP.y - 16 - (cls.FullHeight - cls.TileHeight) * 32,
                                                  MakeZFromY(xP.y) + 4);

            if (LogicStructure.Class.VariableSize)
            {
                StructureMesh = UpdateMesh(sprites, spritesB, 0, Filter.mesh, 0, 0, LogicStructure.Width, LogicStructure.Height, 0, (StructureMesh == null), false);
            }
            else
            {
                StructureMesh = UpdateMesh(sprites, spritesB, actualFrame, Filter.mesh, 0, cls.FullHeight - cls.TileHeight, cls.TileWidth, cls.TileHeight, 0, (StructureMesh == null), false);
                OverlayMesh = UpdateMesh(sprites, spritesB, actualFrame, OverlayFilter.mesh, 0, 0, cls.TileWidth, cls.FullHeight - cls.TileHeight, 0, (OverlayMesh == null), false);
                if (ShadowFilter != null) ShadowMesh = UpdateMesh(sprites, spritesB, actualFrame, ShadowFilter.mesh, 0, 0, cls.TileWidth, cls.FullHeight, 0.3f, (ShadowMesh == null), false);
            }

            LogicStructure.DoUpdateView = false;
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

    public bool ProcessEventPic(Event e, float mousex, float mousey)
    {
        return false;
    }

    public bool ProcessEventInfo(Event e, float mousex, float mousey)
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
            if (TexObject == null)
            {
                TexObject = Utils.CreatePrimitive(PrimitiveType.Quad);
                TexRenderer = TexObject.GetComponent<MeshRenderer>();
                TexRenderer.enabled = true;
                TexObject.name = "MapViewStructure$InfoPic";
            }

            TexObject.SetActive(true);

            TexRenderer.transform.parent = parent;
            // load infowindow texture.
            TexRenderer.material = TexMaterial;
            TexRenderer.material.mainTexture = LogicStructure.Class.PictureFile;
            TexRenderer.transform.localPosition = new Vector3((float)LogicStructure.Class.PictureFile.width / 2,
                                                         (float)LogicStructure.Class.PictureFile.height / 2 + 2, -0.01f);
            TexRenderer.transform.localScale = new Vector3(LogicStructure.Class.PictureFile.width,
                                                           LogicStructure.Class.PictureFile.height, 1);
        }
        else
        {
            if (TexObject != null)
                TexObject.SetActive(false);
        }
    }

    public void DisplayInfo(bool on, Transform parent)
    {
        
    }

    public bool ProcessStartDrag(float mousex, float mousey)
    {
        return false;
    }

    public bool ProcessDrag(Item item, float mousex, float mousey)
    {
        return false;
    }

    public bool ProcessDrop(Item item, float mousex, float mousey)
    {
        return false;
    }

    public void ProcessEndDrag()
    {

    }

    public void ProcessFailDrag()
    {

    }

    public Item ProcessVerifyEndDrag()
    {
        return null;
    }
}
 