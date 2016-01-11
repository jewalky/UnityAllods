using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewUnit : MapViewObject, IMapViewSelectable, IMapViewSelfie, IObjectManualUpdate
{
    private MapUnit LogicUnit
    {
        get
        {
            return (MapUnit)LogicObject;
        }
    }

    public MapObject GetObject() { return LogicObject; }

    private MeshRenderer Renderer;
    private MeshFilter Filter;
    private Mesh UnitMesh;

    private GameObject ShadowObject;
    private MeshRenderer ShadowRenderer;
    private MeshFilter ShadowFilter;
    private Mesh ShadowMesh;

    private static Texture2D HpBall = null;
    private Material HpMat1 = null;
    private Material HpMat2 = null;
    private GameObject HpObject;
    private MeshRenderer HpRenderer;
    private MeshFilter HpFilter;
    private Mesh HpMesh;

    private AllodsTextRenderer PlayerNick = null;
    private GameObject PlayerNickObject = null;

    private void UpdateHpMesh()
    {
        // put player nickname if this unit is an Avatar
        if (LogicUnit.Player != null && LogicUnit.Player.Avatar == LogicUnit)
        {
            if (PlayerNick == null)
            {
                PlayerNick = new AllodsTextRenderer(Fonts.Font2, Font.Align.Center, LogicUnit.Class.SelectionX2 - LogicUnit.Class.SelectionX1, 0, false);
                PlayerNickObject = PlayerNick.GetNewGameObject(0, transform, 100);
                PlayerNickObject.SetActive(LogicUnit.IsAlive);
            }

            PlayerNick.Text = LogicUnit.Player.Name;
            PlayerNick.Material.color = Player.AllColors[LogicUnit.Player.Color];
        }

        if (HpBall == null) HpBall = Images.LoadImage("graphics/interface/ball.bmp", 0, Images.ImageType.AllodsBMP);
        if (HpMat1 == null)
        {
            HpMat1 = new Material(MainCamera.MainShader);
            HpMat1.mainTexture = HpBall;
        }
        if (HpMat2 == null) HpMat2 = new Material(MainCamera.MainShader);

        int hpHeight = 4;
        if (LogicUnit.Stats.ManaMax > 0)
            hpHeight += 4;

        if (HpObject == null)
        {
            HpObject = Utils.CreateObject();
            HpObject.name = "Health";
            HpRenderer = HpObject.AddComponent<MeshRenderer>();
            HpFilter = HpObject.AddComponent<MeshFilter>();
            HpRenderer.enabled = LogicUnit.IsAlive;
            HpMesh = new Mesh();
            HpFilter.mesh = HpMesh;
            HpObject.transform.parent = transform;
            HpObject.transform.localScale = new Vector3(1, 1, 1);
            HpRenderer.materials = new Material[] { HpMat1, HpMat2 };
        }

        HpObject.transform.localPosition = new Vector3(0, -hpHeight, -64);

        HpMesh.Clear();
        int vcnt = 4 * 8; // 
        if (LogicUnit.Stats.ManaMax > 0)
            vcnt += 4 * 8; // 

        Vector3[] qv = new Vector3[vcnt];
        Vector2[] quv = new Vector2[vcnt];
        Color[] qc = new Color[vcnt];
        int pp = 0, ppt = pp, ppc = pp;

        int x = LogicUnit.Class.SelectionX1;
        int y = LogicUnit.Class.SelectionY1;
        int w = LogicUnit.Class.SelectionX2 - LogicUnit.Class.SelectionX1;
        int w2 = w - 8;

        if (PlayerNickObject != null && PlayerNick != null)
            PlayerNickObject.transform.localPosition = new Vector3(x, y - PlayerNick.Height - 1 - hpHeight, -64);

        Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, x, y, 4, 4, new Rect(0, 0, 1, 1), new Color(1, 1, 1, 1));
        Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, x + w - 4, y, 4, 4, new Rect(0, 0, 1, 1), new Color(1, 1, 1, 1));
        if (LogicUnit.Stats.ManaMax > 0)
        {
            Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, x, y + 4, 4, 4, new Rect(0, 0, 1, 1), new Color(1, 1, 1, 1));
            Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, x + w - 4, y + 4, 4, 4, new Rect(0, 0, 1, 1), new Color(1, 1, 1, 1));
        }

        for (int i = 0; i < vcnt; i += 4 * 8)
        {
            int lpp = i + vcnt / 4;
            int lppc = i + vcnt / 4;

            if (i >= 4 * 8) y += 4; // mana is +5px

            qv[lpp++] = new Vector3(x + 4, y);
            qv[lpp++] = new Vector3(x + w - 4, y);
            qv[lpp++] = new Vector3(x + w - 4, y + 4);
            qv[lpp++] = new Vector3(x + 4, y + 4);
            qc[lppc++] = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            qc[lppc++] = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            qc[lppc++] = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            qc[lppc++] = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            float lcnt = (i >= 4 * 8) ? (float)LogicUnit.Stats.Mana / LogicUnit.Stats.ManaMax : (float)LogicUnit.Stats.Health / LogicUnit.Stats.HealthMax;
            if (lcnt < 0) lcnt = 0;
            Color clBase = new Color(0, 1, 0, 1);
            if (i >= 4 * 8) clBase = new Color(0, 0, 1, 1);
            else if (lcnt < 0.33) clBase = new Color(1, 0, 0, 1);
            else if (lcnt < 0.66) clBase = new Color(1, 1, 0, 1);
            Color clDk1 = clBase / 2;
            clDk1.a = 1;
            Color clDk2 = clBase / 3;
            clDk2.a = 1;
            qv[lpp++] = new Vector3(x + 4, y);
            qv[lpp++] = new Vector3(x + 4 + w2 * lcnt, y);
            qv[lpp++] = new Vector3(x + 4 + w2 * lcnt, y + 4);
            qv[lpp++] = new Vector3(x + 4, y + 4);
            qc[lppc++] = clDk2;
            qc[lppc++] = clDk2;
            qc[lppc++] = clDk2;
            qc[lppc++] = clDk2;
            qv[lpp++] = new Vector3(x + 4, y + 1);
            qv[lpp++] = new Vector3(x + 4 + w2 * lcnt, y + 1);
            qv[lpp++] = new Vector3(x + 4 + w2 * lcnt, y + 3);
            qv[lpp++] = new Vector3(x + 4, y + 3);
            qc[lppc++] = clBase;
            qc[lppc++] = clBase;
            qc[lppc++] = clDk1;
            qc[lppc++] = clDk1;
        }

        HpMesh.vertices = qv;
        HpMesh.uv = quv;
        HpMesh.colors = qc;

        HpMesh.subMeshCount = 2;
        int[] qt = new int[vcnt / 4];
        for (int i = 0; i < qt.Length; i++)
            qt[i] = i;
        HpMesh.SetIndices(qt, MeshTopology.Quads, 0);
        int[] qt2 = new int[vcnt - qt.Length];
        for (int i = 0; i < qt2.Length; i++)
            qt2[i] = qt.Length + i;
        HpMesh.SetIndices(qt2, MeshTopology.Quads, 1);
        HpFilter.mesh = HpMesh;
    }

    private Vector2 CurrentPoint;

    private Mesh UpdateMesh(Images.AllodsSpriteSeparate sprite, int frame, Mesh mesh, float shadowOffs, bool first, bool flip)
    {
        Texture2D sTex = sprite.Frames[frame].Texture;
        float sW = sprite.Frames[frame].Width;
        float sH = sprite.Frames[frame].Height;
        float tMaxX = sW / sTex.width;
        float tMaxY = sH / sTex.height;

        bool flying = LogicUnit.IsFlying;

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

        float cx = (float)sprite.Frames[frame].Width * LogicUnit.Class.CenterX;
        float cy = (float)sprite.Frames[frame].Height * LogicUnit.Class.CenterY;
        for (int i = 0; i < qv.Length; i++)
            qv[i] -= new Vector3(cx, cy, 0);

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
        string typeName = (LogicUnit.GetObjectType() == MapObjectType.Monster) ? "Monster" : "Human";
        if (LogicUnit.Class != null)
            name = string.Format("{0} (ID={1}, Class={2})", typeName, LogicUnit.ID, LogicUnit.TemplateName);
        else name = string.Format("{0} (ID={1}, Class=<INVALID>)", typeName, LogicUnit.ID);
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

    protected virtual Texture2D GetPalette()
    {
        return LogicUnit.Class.File.UpdatePalette(LogicUnit.Face);
    }

    private bool spriteSet = false;
    private bool oldVisibility = false;
    public void OnUpdate()
    {
        if (Renderer == null)
            return;

        bool bAlive = LogicUnit.IsAlive && !LogicUnit.IsDying;

        if (LogicUnit.GetVisibility() != 2)
        {
            oldVisibility = false;
            Renderer.enabled = false;
            ShadowRenderer.enabled = false;
            if (HpRenderer != null) HpRenderer.enabled = false;
            if (PlayerNickObject != null) PlayerNickObject.SetActive(false);
            return;
        }
        else if (!oldVisibility)
        {
            Renderer.enabled = true;
            ShadowRenderer.enabled = bAlive;
            if (HpRenderer != null) HpRenderer.enabled = LogicUnit.IsAlive;
            if (PlayerNickObject != null) PlayerNickObject.SetActive(LogicUnit.IsAlive);
            oldVisibility = true;
            return;
        }

        bool hovered = (MapView.Instance.HoveredObject == LogicUnit);
        if (Renderer != null) Renderer.material.SetFloat("_Lightness", hovered ? 0.75f : 0.5f);
        bool selected = (MapView.Instance.SelectedObject == LogicUnit);
        if (HpMat1 != null) HpMat1.color = new Color(1, 1, 1, selected ? 1f : 0.5f);
        if (HpMat2 != null) HpMat2.color = new Color(1, 1, 1, selected ? 1f : 0.5f);

        if (LogicUnit.DoUpdateView)
        {
            Renderer.enabled = true;
            ShadowRenderer.enabled = bAlive;
            if (HpRenderer != null) HpRenderer.enabled = LogicUnit.IsAlive;
            if (PlayerNickObject != null) PlayerNickObject.SetActive(LogicUnit.IsAlive);

            Images.AllodsSpriteSeparate sprites = LogicUnit.Class.File.File;

            if (!spriteSet)
            {
                LogicUnit.Class.File.UpdateSprite();
                sprites = LogicUnit.Class.File.File;
                Renderer.material = new Material(MainCamera.MainShaderPaletted);
                ShadowRenderer.material = Renderer.material;
                ShadowRenderer.material.color = new Color(0, 0, 0, 0.5f);
                spriteSet = true;
            }

            int actualFrame = LogicUnit.Class.Index; // draw frame 0 of each unit

            UnitVisualState actualVState = LogicUnit.VState;
            if (!bAlive && actualVState == UnitVisualState.Idle)
            {
                actualVState = UnitVisualState.Dying;
                UnitClass dCls = LogicUnit.Class;
                while (dCls.Dying != null && dCls.Dying != dCls)
                    dCls = dCls.Dying;
                LogicUnit.DeathFrame = dCls.DyingPhases - 1;
            }

            Renderer.material.SetTexture("_Palette", GetPalette());
            // first (idle) state is 0..8 frames. frames 1 to 7 are flipped. frames 0 and 8 aren't.
            //  135 180 225
            //  90      270
            //  45   0  315
            bool doFlip = false;
            if (actualVState == UnitVisualState.Rotating || (actualVState == UnitVisualState.Idle && LogicUnit.Class.IdlePhases == 1))
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
            else if (actualVState == UnitVisualState.Idle)
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
            else if (actualVState == UnitVisualState.Moving)
            {
                int moveSize = LogicUnit.Class.MoveBeginPhases + LogicUnit.Class.MovePhases;

                if (LogicUnit.Class.Flip)
                {
                    if (LogicUnit.Angle < 180)
                    {
                        int actualAngle = LogicUnit.Angle * 4 / 180;
                        actualFrame = 9 + moveSize * actualAngle;
                    }
                    else
                    {
                        int actualAngle = (180 - (LogicUnit.Angle - 180)) * 4 / 180;
                        actualFrame = 9 + moveSize * actualAngle;
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
            else if (actualVState == UnitVisualState.Attacking)
            {
                int moveSize = LogicUnit.Class.MoveBeginPhases + LogicUnit.Class.MovePhases;
                int attackSize = LogicUnit.Class.AttackPhases;

                if (LogicUnit.Class.Flip)
                {
                    if (LogicUnit.Angle < 180)
                    {
                        int actualAngle = LogicUnit.Angle * 4 / 180;
                        actualFrame = 9 + moveSize * 5 + attackSize * actualAngle;
                    }
                    else
                    {
                        int actualAngle = (180 - (LogicUnit.Angle - 180)) * 4 / 180;
                        actualFrame = 9 + moveSize * 5 + attackSize * actualAngle;
                        doFlip = true;
                    }
                }
                else
                {
                    int actualAngle = LogicUnit.Angle * 8 / 360;
                    actualFrame = 16 + moveSize * 8 + attackSize * actualAngle;
                }

                actualFrame += LogicUnit.Class.AttackFrames[LogicUnit.AttackFrame].Frame;
            }
            else if (actualVState == UnitVisualState.Dying)
            {
                UnitClass dCls = LogicUnit.Class;
                while (dCls.Dying != null && dCls.Dying != dCls)
                    dCls = dCls.Dying;
                dCls.File.UpdateSprite();
                sprites = dCls.File.File;
                Renderer.material.SetTexture("_Palette", dCls.File.UpdatePalette(LogicUnit.Face));

                int moveSize = dCls.MoveBeginPhases + dCls.MovePhases;
                int attackSize = dCls.AttackPhases;
                int dyingSize = dCls.DyingPhases;

                if (LogicUnit.Class.Flip)
                {
                    if (LogicUnit.Angle < 180)
                    {
                        int actualAngle = LogicUnit.Angle * 4 / 180;
                        actualFrame = 9 + moveSize * 5 + attackSize * 5 + dyingSize * actualAngle;
                    }
                    else
                    {
                        int actualAngle = (180 - (LogicUnit.Angle - 180)) * 4 / 180;
                        actualFrame = 9 + moveSize * 5 + attackSize * 5 + dyingSize * actualAngle;
                        doFlip = true;
                    }
                }
                else
                {
                    int actualAngle = LogicUnit.Angle * 8 / 360;
                    actualFrame = 16 + moveSize * 8 + attackSize * 8 + dyingSize * actualAngle;
                }

                actualFrame += LogicUnit.DeathFrame;
            }

            Vector2 xP = MapView.Instance.MapToScreenCoords(LogicUnit.X + LogicUnit.FracX + (float)LogicUnit.Width / 2,
                                                            LogicUnit.Y + LogicUnit.FracY + (float)LogicUnit.Height / 2,
                                                            1, 1);
            CurrentPoint = xP;
            float zInv = 0;
            if (!bAlive)
                zInv = 48;
            else if (LogicUnit.IsFlying)
                zInv = -128;
            transform.localPosition = new Vector3(xP.x, xP.y, MakeZFromY(xP.y) + zInv); // order sprites by y coordinate basically
            //Debug.Log(string.Format("{0} {1} {2}", xP.x, sprites.Sprites[0].rect.width, LogicUnit.Class.CenterX));
            //Renderer.sprite = sprites.Sprites[actualFrame];
            UnitMesh = UpdateMesh(sprites, actualFrame, Filter.mesh, 0, (UnitMesh == null), doFlip);
            ShadowMesh = UpdateMesh(sprites, actualFrame, ShadowFilter.mesh, 0.3f, (ShadowMesh == null), doFlip); // 0.3 of sprite height
            UpdateHpMesh();

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

        if (!LogicUnit.IsAlive)
            return false;

        int cx = x - (int)CurrentPoint.x;
        int cy = y - (int)CurrentPoint.y;
        if (cx > LogicUnit.Class.SelectionX1 &&
            cx < LogicUnit.Class.SelectionX2 &&
            cy > LogicUnit.Class.SelectionY1 &&
            cy < LogicUnit.Class.SelectionY2) return true;

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

    public virtual void DisplayPic(bool on, Transform parent)
    {
        if (on)
        {
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

            TexRenderer.transform.parent = parent;
            // load infowindow texture.
            Texture2D pic = LogicUnit.Class.UpdateInfoPicture(LogicUnit.Face);
            TexRenderer.material = TexMaterial;
            TexRenderer.material.mainTexture = pic;
            TexRenderer.transform.localPosition = new Vector3((float)pic.width / 2,
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
    private static AllodsTextRenderer Info_LifeCaption; // life + mana captions
    private static AllodsTextRenderer Info_Life; // life + mana values
    private static AllodsTextRenderer Info_BRMSCaption; // BRMS (main stats) captions
    private static AllodsTextRenderer Info_BRMS; // BRMS values
    private static AllodsTextRenderer Info_DamageCaption; // damage + tohit captions
    private static AllodsTextRenderer Info_Damage; // damage + tohit values
    private static AllodsTextRenderer Info_DefenseCaption; // defense + absorbtion captions
    private static AllodsTextRenderer Info_Defense; // defense + absorbtion values
    private static AllodsTextRenderer Info_ResistCaptionMain; // magic resist caption
    private static AllodsTextRenderer Info_ResistCaption; // magic resist captions (individual)
    private static AllodsTextRenderer Info_Resist; // magic resist values
    private static AllodsTextRenderer Info_SkillCaptionMain; // skill caption
    private static AllodsTextRenderer Info_SkillCaption; // skill captions (individual)
    private static AllodsTextRenderer Info_Skill; // skill values

    private static AllodsTextRenderer Info_ScanSpeedCaption; // scanrange + speed captions
    private static AllodsTextRenderer Info_ScanSpeed; // scanrange + speed values

    private AllodsTextRenderer DisplayInfoInit(Font.Align align, int x, int y, int w, int h, Color color)
    {
        // 70 10 39 19
        AllodsTextRenderer tr = new AllodsTextRenderer(Fonts.Font2, align, w, h, false);
        GameObject trO = tr.GetNewGameObject(0.01f, InfoObject.transform, 100, 0.2f);
        trO.transform.localPosition = new Vector3(x, y, 0);
        tr.Material.color = color;
        return tr;
    }

    public void DisplayInfo(bool on, Transform parent)
    {
        if (on)
        {
            if (InfoObject == null)
            {
                InfoObject = Utils.CreateObject();
                InfoObject.name = "MapViewUnit$InfoText";

                Color colorCaption = new Color32(0xBD, 0x9E, 0x4A, 0xFF);
                Color colorValue = new Color32(0x6B, 0x9A, 0x7B, 0xFF);

                Info_Name = DisplayInfoInit(Font.Align.Center, 39, 19, 70, 10, colorCaption);
                Info_LifeCaption = DisplayInfoInit(Font.Align.Center, 85, 45, 58, 10, colorCaption);
                Info_Life = DisplayInfoInit(Font.Align.Center, 85, 45, 58, 10, colorValue);
                Info_BRMSCaption = DisplayInfoInit(Font.Align.Left, 7, 45, 73, 39, colorCaption);
                Info_BRMS = DisplayInfoInit(Font.Align.Right, 7, 45, 73, 39, colorValue);
                Info_DamageCaption = DisplayInfoInit(Font.Align.Left, 7, 89, 63, 18, colorCaption);
                Info_Damage = DisplayInfoInit(Font.Align.Right, 7, 89, 63, 18, colorValue);
                Info_DefenseCaption = DisplayInfoInit(Font.Align.Left, 75, 89, 65, 18, colorCaption);
                Info_Defense = DisplayInfoInit(Font.Align.Right, 75, 89, 65, 18, colorValue);
                Info_ResistCaptionMain = DisplayInfoInit(Font.Align.Left, 75, 113, 65, 8, new Color32(0x94, 0x59, 0x00, 0xFF));
                Info_ResistCaption = DisplayInfoInit(Font.Align.Left, 75, 123, 65, 48, colorCaption);
                Info_Resist = DisplayInfoInit(Font.Align.Right, 75, 123, 65, 48, colorValue);
                Info_SkillCaptionMain = DisplayInfoInit(Font.Align.Left, 7, 113, 63, 8, new Color32(0x94, 0x59, 0x00, 0xFF));
                Info_SkillCaption = DisplayInfoInit(Font.Align.Left, 7, 123, 63, 48, colorCaption);
                Info_Skill = DisplayInfoInit(Font.Align.Right, 7, 123, 63, 48, colorValue);
                Info_ScanSpeedCaption = DisplayInfoInit(Font.Align.Left, 41, 201, 65, 18, colorCaption);
                Info_ScanSpeed = DisplayInfoInit(Font.Align.Right, 41, 201, 65, 18, colorValue);
            }

            InfoObject.transform.parent = parent;
            InfoObject.transform.localPosition = new Vector3(0, 0, -0.2f);
            InfoObject.transform.localScale = new Vector3(1, 1, 1);
            InfoObject.SetActive(true);

            if (LogicUnit.Player != null && LogicUnit.Player.Avatar == LogicUnit)
                Info_Name.Text = LogicUnit.Player.Name;
            else Info_Name.Text = "\n" + Locale.UnitName[LogicUnit.Class.ID];

            string lifeCaption = Locale.Main[19];
            string lifeValue = string.Format("\n{0}/{1}", LogicUnit.Stats.Health, LogicUnit.Stats.HealthMax);
            if (LogicUnit.Stats.ManaMax > 0)
            {
                lifeCaption += "\n\n" + Locale.Main[20];
                lifeValue += string.Format("\n\n{0}/{1}", LogicUnit.Stats.Mana, LogicUnit.Stats.ManaMax);
            }

            Info_LifeCaption.Text = lifeCaption;
            Info_Life.Text = lifeValue;

            Info_BRMSCaption.Text = string.Format("{0}\n{1}\n{2}\n{3}", Locale.Main[15], Locale.Main[16], Locale.Main[17], Locale.Main[18]);
            Info_BRMS.Text = string.Format("{0}\n{1}\n{2}\n{3}", LogicUnit.Stats.Body, LogicUnit.Stats.Reaction, LogicUnit.Stats.Mind, LogicUnit.Stats.Spirit);

            Info_DamageCaption.Text = string.Format("{0}\n{1}", Locale.Main[23], Locale.Main[25]);
            Info_Damage.Text = string.Format("{0}-{1}\n{2}", LogicUnit.Stats.DamageMin, LogicUnit.Stats.DamageMax, LogicUnit.Stats.ToHit);

            Info_DefenseCaption.Text = string.Format("{0}\n{1}", Locale.Main[24], Locale.Main[26]);
            Info_Defense.Text = string.Format("{0}\n{1}", LogicUnit.Stats.Absorbtion, LogicUnit.Stats.Defence);

            Info_ResistCaptionMain.Text = Locale.Main[28];
            Info_ResistCaption.Text = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", Locale.Main[41], Locale.Main[42], Locale.Main[43], Locale.Main[44], Locale.Main[45]);
            Info_Resist.Text = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", LogicUnit.Stats.ProtectionFire,
                                                                        LogicUnit.Stats.ProtectionWater,
                                                                        LogicUnit.Stats.ProtectionAir,
                                                                        LogicUnit.Stats.ProtectionEarth,
                                                                        LogicUnit.Stats.ProtectionAstral);

            Info_SkillCaptionMain.Text = Locale.Main[28];
            Info_SkillCaption.Text = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", Locale.Main[30], Locale.Main[31], Locale.Main[32], Locale.Main[33], Locale.Main[34]);
            Info_Skill.Text = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", LogicUnit.Stats.ProtectionBlade,
                                                                       LogicUnit.Stats.ProtectionAxe,
                                                                       LogicUnit.Stats.ProtectionBludgeon,
                                                                       LogicUnit.Stats.ProtectionPike,
                                                                       LogicUnit.Stats.ProtectionShooting);

            Info_ScanSpeedCaption.Text = string.Format("{0}\n{1}", Locale.Main[21], Locale.Main[22]);
            Info_ScanSpeed.Text = string.Format("{0:F1}\n{1}", LogicUnit.Stats.ScanRange, LogicUnit.Stats.Speed);
        }
        else
        {
            if (InfoObject != null)
                InfoObject.SetActive(false);
        }
    }
}