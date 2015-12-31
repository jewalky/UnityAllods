using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewUnit : MapViewObject, IMapViewSelectable, IMapViewSelfie, IObjectManualUpdate
{
    public MapUnit LogicUnit
    {
        get
        {
            return (MapUnit)LogicObject;
        }
    }

    private MeshRenderer Renderer;
    private MeshFilter Filter;
    private Mesh ObstacleMesh;

    private GameObject ShadowObject;
    private MeshRenderer ShadowRenderer;
    private MeshFilter ShadowFilter;
    private Mesh ShadowMesh;

    private Vector2 CurrentPoint;
    private bool DrawSelected = false;

    private Mesh UpdateMesh(Images.AllodsSpriteSeparate sprite, int frame, Mesh mesh, float shadowOffs, bool first, bool flip)
    {
        Texture2D sTex = sprite.Frames[frame].Texture;
        float sW = sprite.Frames[frame].Width;
        float sH = sprite.Frames[frame].Height;
        float tMaxX = sW / sTex.width;
        float tMaxY = sH / sTex.height;

        bool flying = LogicUnit.Template.IsFlying;

        float shadowOffsReal = shadowOffs * sH;
        float shadowOffsXLeft = -shadowOffsReal * (1f - LogicUnit.Class.CenterY);

        Vector3[] qv = new Vector3[4];
        int pp = 0;
        if (!flying || (shadowOffs == 0))
        {
            qv[pp++] = new Vector3(shadowOffsReal, 0, 0);
            qv[pp++] = new Vector3(shadowOffsReal + sW, 0, 0);
            qv[pp++] = new Vector3(shadowOffsXLeft + sW, sH, 0);
            qv[pp++] = new Vector3(shadowOffsXLeft, sH, 0);
        }
        else
        {
            float shadowOffs1 = shadowOffs * 64;
            float shadowOffs2 = shadowOffs1 + sW;
            shadowOffs = -4;
            qv[pp++] = new Vector3(shadowOffs1, shadowOffs, 0);
            qv[pp++] = new Vector3(shadowOffs2, shadowOffs, 0);
            qv[pp++] = new Vector3(shadowOffs2, shadowOffs + sH, 0);
            qv[pp++] = new Vector3(shadowOffs1, shadowOffs + sH, 0);
        }

        Vector2[] quv = new Vector2[4];
        if (!flip)
        {
            quv[0] = new Vector2(0, 0);
            quv[1] = new Vector2(tMaxX, 0);
            quv[2] = new Vector2(tMaxX, tMaxY);
            quv[3] = new Vector2(0, tMaxY);
        }
        else
        {
            quv[0] = new Vector2(tMaxX, 0);
            quv[1] = new Vector2(0, 0);
            quv[2] = new Vector2(0, tMaxY);
            quv[3] = new Vector2(tMaxX, tMaxY);
        }

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
        if (LogicUnit.Class != null)
            name = string.Format("Unit (ID={0}, Class={1})", LogicUnit.ID, LogicUnit.Template.Name);
        else name = string.Format("Unit (ID={0}, Class=<INVALID>)", LogicUnit.ID);
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
    public void OnUpdate()
    {
        if (Renderer == null)
            return;

        if (LogicUnit.GetVisibility() != 2)
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

        Renderer.material.SetFloat("_Lightness", DrawSelected ? 0.75f : 0.5f);

        if (LogicUnit.DoUpdateView)
        {
            Renderer.enabled = true;
            ShadowRenderer.enabled = true;

            Images.AllodsSpriteSeparate sprites = LogicUnit.Class.File.File;

            if (!spriteSet)
            {
                LogicUnit.Class.File.UpdateSprite();
                sprites = LogicUnit.Class.File.File;
                Renderer.material = new Material(MainCamera.MainShaderPaletted);
                //Renderer.material.SetTexture("_Palette", sprites.OwnPalette); // no palette swap for this one
                Renderer.material.SetTexture("_Palette", LogicUnit.Class.File.UpdatePalette(LogicUnit.Template.Face));
                ShadowRenderer.material = Renderer.material;
                ShadowRenderer.material.color = new Color(0, 0, 0, 0.5f);
                spriteSet = true;
            }

            int actualFrame = LogicUnit.Class.Index; // draw frame 0 of each unit

            // first (idle) state is 0..8 frames. frames 1 to 7 are flipped. frames 0 and 8 aren't.
            //  135 180 225
            //  90      270
            //  45   0  315
            bool doFlip = false;
            if (LogicUnit.VState == UnitVisualState.Rotating || (LogicUnit.VState == UnitVisualState.Idle && LogicUnit.Class.IdlePhases == 1))
            {
                if (LogicUnit.Class.Flip)
                {
                    if (LogicUnit.Angle < 180)
                    {
                        int actualAngle = LogicUnit.Angle * 8 / 180;
                        actualFrame = actualAngle;
                    }
                    else
                    {
                        int actualAngle = (180 - (LogicUnit.Angle - 180)) * 8 / 180;
                        actualFrame = actualAngle;
                        doFlip = true;
                    }
                }
                else
                {
                    int actualAngle = LogicUnit.Angle * 16 / 360;
                    actualFrame = actualAngle;
                }
            }
            else if (LogicUnit.VState == UnitVisualState.Idle)
            {
                if (LogicUnit.Class.IdlePhases > 1)
                {
                    int idlePhasesCount;
                    // 0..4 rotations if flipped. 0..8 if not.
                    if (LogicUnit.Class.Flip)
                    {
                        if (LogicUnit.Angle < 180)
                        {
                            int actualAngle = LogicUnit.Angle * 4 / 180;
                            actualFrame = LogicUnit.Class.IdlePhases * actualAngle;
                        }
                        else
                        {
                            int actualAngle = (180 - (LogicUnit.Angle - 180)) * 4 / 180;
                            actualFrame = LogicUnit.Class.IdlePhases * actualAngle;
                            doFlip = true;
                        }
                        idlePhasesCount = 5 * LogicUnit.Class.IdlePhases;
                    }
                    else
                    {
                        int actualAngle = LogicUnit.Angle * 8 / 360;
                        actualFrame = LogicUnit.Class.IdlePhases * actualAngle;
                        idlePhasesCount = 8 * LogicUnit.Class.IdlePhases;
                    }

                    actualFrame = sprites.Frames.Length - idlePhasesCount + actualFrame;
                    actualFrame += LogicUnit.Class.IdleFrames[LogicUnit.IdleFrame].Frame;
                }
            }
            else if (LogicUnit.VState == UnitVisualState.Moving)
            {
                int moveSize = LogicUnit.Class.MoveBeginPhases + LogicUnit.Class.MovePhases;

                if (LogicUnit.Class.Flip)
                {
                    if (LogicUnit.Angle < 180)
                    {
                        int actualAngle = LogicUnit.Angle * 4 / 180;
                        actualFrame = 8 + moveSize * actualAngle;
                    }
                    else
                    {
                        int actualAngle = (180 - (LogicUnit.Angle - 180)) * 4 / 180;
                        actualFrame = 8 + moveSize * actualAngle;
                        doFlip = true;
                    }
                }
                else
                {
                    int actualAngle = LogicUnit.Angle * 8 / 360;
                    actualFrame = 16 + moveSize * actualAngle;
                }

                actualFrame += LogicUnit.Class.MoveBeginPhases; // movebeginphases, we don't animate this yet
                actualFrame += LogicUnit.Class.MoveFrames[LogicUnit.MoveFrame].Frame;
            }

            Vector2 xP = MapView.Instance.MapToScreenCoords(LogicUnit.X + (float)LogicUnit.Width / 2 + LogicUnit.FracX,
                                                            LogicUnit.Y + (float)LogicUnit.Height / 2 + LogicUnit.FracY,
                                                            LogicUnit.Width, LogicUnit.Height);
            CurrentPoint = xP;
            float zInv = 0;
            if (LogicUnit.Template.IsFlying)
                zInv = -128;
            transform.localPosition = new Vector3(xP.x - (float)sprites.Frames[actualFrame].Width * LogicUnit.Class.CenterX,
                                                    xP.y - (float)sprites.Frames[actualFrame].Height * LogicUnit.Class.CenterY,
                                                    MakeZFromY(xP.y)+zInv); // order sprites by y coordinate basically
            //Debug.Log(string.Format("{0} {1} {2}", xP.x, sprites.Sprites[0].rect.width, LogicUnit.Class.CenterX));
            //Renderer.sprite = sprites.Sprites[actualFrame];
            ObstacleMesh = UpdateMesh(sprites, actualFrame, Filter.mesh, 0, (ObstacleMesh == null), doFlip);
            ShadowMesh = UpdateMesh(sprites, actualFrame, ShadowFilter.mesh, 0.3f, (ShadowMesh == null), doFlip); // 0.3 of sprite height

            LogicUnit.DoUpdateView = false;
        }
    }

    void OnDestroy()
    {
        if (Filter != null && Filter.mesh != null)
            DestroyImmediate(Filter.mesh, true);
        if (ShadowFilter != null && ShadowFilter.mesh != null)
            DestroyImmediate(ShadowFilter.mesh, true);
    }

    public bool IsSelected(int x, int y)
    {
        if (LogicUnit.GetVisibility() < 2)
            return false;

        int cx = x - (int)CurrentPoint.x;
        int cy = y - (int)CurrentPoint.y;
        if (cx > LogicUnit.Class.SelectionX1 &&
            cx < LogicUnit.Class.SelectionX2 &&
            cy > LogicUnit.Class.SelectionY1 &&
            cy < LogicUnit.Class.SelectionY2)
        {
            DrawSelected = true;
            return true;
        }

        DrawSelected = false;
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

    // infowindow stuff
    private static GameObject TexObject;
    private static MeshRenderer TexRenderer;
    private static Material TexMaterial;

    public void DisplayPic(bool on, Transform parent)
    {
        if (on)
        {
            // load infowindow texture.
            Texture2D pic = LogicUnit.Class.UpdateInfoPicture(LogicUnit.Template.Face);
            // init infowindow
            if (TexMaterial == null)
                TexMaterial = new Material(MainCamera.MainShader);
            if (TexObject == null)
            {
                TexObject = Utils.CreatePrimitive(PrimitiveType.Quad);
                TexRenderer = TexObject.GetComponent<MeshRenderer>();
                TexRenderer.enabled = true;
                TexObject.name = "MapViewUnit$InfoPic";
            }
            TexObject.SetActive(true);
            TexRenderer.material = TexMaterial;
            TexRenderer.material.mainTexture = pic;
            TexRenderer.transform.parent = parent;
            TexRenderer.transform.localPosition = new Vector3((float)pic.width / 2 + 16,
                                                         (float)pic.height / 2 + 2, -0.01f);
            TexRenderer.transform.localScale = new Vector3(pic.width,
                                                           pic.height, 1);
        }
        else
        {
            if (TexObject != null)
                TexObject.SetActive(false);
        }
    }

    private static GameObject InfoObject;
    private static AllodsTextRenderer Info_Name;

    public void DisplayInfo(bool on, Transform parent)
    {
        if (on)
        {
            if (InfoObject == null)
            {
                int wd = 176;
                InfoObject = Utils.CreateObject();
                Info_Name = new AllodsTextRenderer(Fonts.Font2, Font.Align.Center, 70, 10, false);
                GameObject Info_NameO = Info_Name.GetNewGameObject(0.01f, InfoObject.transform, 100, 0.2f);
                Info_NameO.transform.localPosition = new Vector3(39, 19, 0);
                Info_Name.Material.color = new Color32(0xBD, 0x9E, 0x4A, 0xFF);
            }

            InfoObject.transform.parent = parent;
            InfoObject.transform.localPosition = new Vector3(0, 0, -0.2f);
            InfoObject.transform.localScale = new Vector3(1, 1, 1);
            InfoObject.SetActive(true);

            Info_Name.Text = "\n" + Locale.UnitName[LogicUnit.Class.ID];
        }
        else
        {
            if (InfoObject != null)
                InfoObject.SetActive(false);
        }
    }
}