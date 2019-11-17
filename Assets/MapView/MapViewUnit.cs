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
        if (LogicUnit.Stats.ManaMax > 0 || LogicUnit.SummonTimeMax > 0)
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
        if (LogicUnit.Stats.ManaMax > 0 || LogicUnit.SummonTimeMax > 0)
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

        float row1 = (float)LogicUnit.Stats.Health / LogicUnit.Stats.HealthMax;
        float row2 = 0;
        bool row2alternate = false;

        Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, x, y, 4, 4, new Rect(0, 0, 1, 1), new Color(1, 1, 1, 1));
        Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, x + w - 4, y, 4, 4, new Rect(0, 0, 1, 1), new Color(1, 1, 1, 1));
        if (LogicUnit.Stats.ManaMax > 0)
        {
            Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, x, y + 4, 4, 4, new Rect(0, 0, 1, 1), new Color(1, 1, 1, 1));
            Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, x + w - 4, y + 4, 4, 4, new Rect(0, 0, 1, 1), new Color(1, 1, 1, 1));
            row2 = (float)LogicUnit.Stats.Mana / LogicUnit.Stats.ManaMax;
        }
        else if (LogicUnit.SummonTimeMax > 0)
        {
            Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, x, y + 4, 4, 4, new Rect(0, 0, 1, 1), new Color(1, 1, 1, 1));
            Utils.PutQuadInMesh(qv, quv, qc, ref pp, ref ppt, ref ppc, x + w - 4, y + 4, 4, 4, new Rect(0, 0, 1, 1), new Color(1, 1, 1, 1));
            row2 = 1f - ((float)LogicUnit.SummonTime / LogicUnit.SummonTimeMax);
            row2alternate = true;
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

            float lcnt = (i >= 4 * 8) ? row2 : row1;
            if (lcnt < 0) lcnt = 0;
            Color clBase = new Color(0, 1, 0, 1);
            if (i >= 4 * 8) clBase = row2alternate ? new Color(1, 0, 1, 1) : new Color(0, 0, 1, 1);
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

    private Mesh UpdateMesh(Images.AllodsSpriteSeparate sprite, Images.AllodsSpriteSeparate spriteB, int frame, Mesh mesh, float shadowOffs, bool first, bool flip)
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

        //
        bool flying = LogicUnit.IsFlying;

        // main
        float shadowOffsReal = shadowOffs * sH;
        float shadowOffsXLeft = -shadowOffsReal * (1f - LogicUnit.Class.CenterY);

        // additional
        float shadowOffsRealB = shadowOffs * sHB;
        float shadowOffsXLeftB = -shadowOffsRealB * (1f - LogicUnit.Class.CenterY);

        //
        int vertexCount = (spriteB != null) ? 8 : 4;

        Vector3[] qv = new Vector3[vertexCount];
        int pp = 0;
        if (!flying || (shadowOffs == 0))
        {
            // main sprite
            qv[pp++] = new Vector3(shadowOffsReal, 0, 0);
            qv[pp++] = new Vector3(shadowOffsReal + sW, 0, 0);
            qv[pp++] = new Vector3(shadowOffsXLeft + sW, sH, 0);
            qv[pp++] = new Vector3(shadowOffsXLeft, sH, 0);
            
            // calculate offset for spriteB, if it's not the same size
            if (spriteB != null)
            {
                // additional sprite
                qv[pp++] = new Vector3(shadowOffsRealB, 0, 0);
                qv[pp++] = new Vector3(shadowOffsRealB + sWB, 0, 0);
                qv[pp++] = new Vector3(shadowOffsXLeftB + sWB, sHB, 0);
                qv[pp++] = new Vector3(shadowOffsXLeftB, sHB, 0);
            }
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

            // calculate offset for spriteB, if it's not the same size
            if (spriteB != null)
            {
                float shadowOffs1B = shadowOffs * 64;
                float shadowOffs2B = shadowOffs1B + sWB;
                // additional sprite
                qv[pp++] = new Vector3(shadowOffs1B, shadowOffs, 0);
                qv[pp++] = new Vector3(shadowOffs2B, shadowOffs, 0);
                qv[pp++] = new Vector3(shadowOffs2B, shadowOffs + sHB, 0);
                qv[pp++] = new Vector3(shadowOffs1B, shadowOffs + sHB, 0);
            }
        }

        Vector2[] quv = new Vector2[vertexCount];
        if (!flip)
        {
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
        }
        else
        {
            quv[0] = new Vector2(tMaxX, 0);
            quv[1] = new Vector2(0, 0);
            quv[2] = new Vector2(0, tMaxY);
            quv[3] = new Vector2(tMaxX, tMaxY);

            if (spriteB != null)
            {
                quv[4] = new Vector2(tMaxXB, 0);
                quv[5] = new Vector2(0, 0);
                quv[6] = new Vector2(0, tMaxYB);
                quv[7] = new Vector2(tMaxXB, tMaxYB);
            }
        }

        float cx = (float)sprite.Frames[frame].Width * LogicUnit.Class.CenterX;
        float cy = (float)sprite.Frames[frame].Height * LogicUnit.Class.CenterY;
        for (int i = 0; i < 4; i++)
            qv[i] -= new Vector3(cx, cy, 0);

        if (spriteB != null)
        {
            float cxB = (float)spriteB.Frames[frame].Width * LogicUnit.Class.CenterX;
            float cyB = (float)spriteB.Frames[frame].Height * LogicUnit.Class.CenterY;
            for (int i = 4; i < 8; i++)
                qv[i] -= new Vector3(cxB, cyB, 0);
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
                qtB[i] = i + 4;
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

    protected virtual Texture2D GetDeathPalette(UnitFile f)
    {
        return f.UpdatePalette(LogicUnit.Face);
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
        float baseLightness = hovered ? 0.75f : 0.5f;
        if (Renderer != null && !LogicUnit.DoUpdateView) Renderer.material.SetFloat("_Lightness", baseLightness);
        bool selected = (MapView.Instance.SelectedObject == LogicUnit);
        if (HpMat1 != null) HpMat1.color = new Color(1, 1, 1, selected ? 1f : 0.5f);
        if (HpMat2 != null) HpMat2.color = new Color(1, 1, 1, selected ? 1f : 0.5f);

        // check lights. we have to do it here because hovering is also implemented through lightness
        // to-do move unit lightness elsewhere
        float meanDynLight = 0f;
        int countDynLight = 0;
        for (int cY = LogicUnit.Y; cY < LogicUnit.Y + LogicUnit.Height; cY++)
        {
            for (int cX = LogicUnit.X; cX < LogicUnit.X + LogicUnit.Width; cX++)
            {
                countDynLight++;
                meanDynLight += (float)MapLogic.Instance.Nodes[cX, cY].DynLight / 255;
            }
        }

        if (countDynLight > 0)
            meanDynLight /= countDynLight;
        else meanDynLight = 0f;

        for (int i = 0; i < Renderer.materials.Length; i++)
        {
            Renderer.materials[i].SetFloat("_Lightness", baseLightness + meanDynLight);
        }

        if (LogicUnit.DoUpdateView)
        {
            Renderer.enabled = true;
            ShadowRenderer.enabled = bAlive;
            if (HpRenderer != null) HpRenderer.enabled = LogicUnit.IsAlive;
            if (PlayerNickObject != null) PlayerNickObject.SetActive(LogicUnit.IsAlive);

            UnitClass dCls = LogicUnit.Class;
            if (!LogicUnit.IsAlive || LogicUnit.IsDying)
            {
                while (dCls.Dying != null && dCls.Dying != dCls)
                    dCls = dCls.Dying;
            }

            Images.AllodsSpriteSeparate sprites = dCls.File.File;
            Images.AllodsSpriteSeparate spritesB = dCls.File.FileB;

            dCls.File.UpdateSprite();
            sprites = dCls.File.File;
            spritesB = dCls.File.FileB;

            if (!MapView.Instance.SpritesBEnabled)
                spritesB = null;

            int newMaterialCount = (spritesB != null) ? 2 : 1;

            if (!spriteSet || (Renderer.materials.Length != newMaterialCount))
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
                spriteSet = true;
            }

            // handle invisiblity flag
            if ((LogicUnit.Flags & UnitFlags.Invisible) != 0)
            {
                for (int i = 0; i < newMaterialCount; i++)
                {
                    Renderer.materials[i].color = new Color(1, 1, 1, 0.5f);
                    ShadowRenderer.materials[i].color = new Color(0, 0, 0, 0.25f);
                }
            }
            else
            {
                for (int i = 0; i < newMaterialCount; i++)
                {
                    Renderer.materials[i].color = new Color(1, 1, 1, 1);
                    ShadowRenderer.materials[i].color = new Color(0, 0, 0, 0.5f);
                }
            }

            int actualFrame = dCls.Index; // draw frame 0 of each unit

            UnitVisualState actualVState = LogicUnit.VState;
            if (!bAlive && actualVState == UnitVisualState.Idle)
            {
                if (LogicUnit.BoneFrame == 0 || dCls.BonePhases < 3)
                {
                    actualVState = UnitVisualState.Dying;
                    LogicUnit.DeathFrame = dCls.DyingPhases - 1;
                }
                else
                {
                    actualVState = UnitVisualState.Bone;
                }
            }

            for (int i = 0; i < newMaterialCount; i++)
                Renderer.materials[i].SetTexture("_Palette", GetDeathPalette(dCls.File));

            // first (idle) state is 0..8 frames. frames 1 to 7 are flipped. frames 0 and 8 aren't.
            //  135 180 225
            //  90      270
            //  45   0  315
            bool doFlip = false;
            int actualAngle16 = 0;
            int actualAngle8 = 0;
            int countFull16 = (!dCls.Flip) ? 16 : 9;
            int countFull8 = (!dCls.Flip) ? 8 : 5;

            if (dCls.Flip)
            {
                if (LogicUnit.Angle < 180)
                {
                    actualAngle16 = LogicUnit.Angle * 8 / 180;
                    actualAngle8 = LogicUnit.Angle * 4 / 180;
                }
                else
                {
                    actualAngle16 = (180 - (LogicUnit.Angle - 180)) * 8 / 180;
                    actualAngle8 = (180 - (LogicUnit.Angle - 180)) * 4 / 180;
                    doFlip = true;
                }
            }
            else
            {
                actualAngle16 = LogicUnit.Angle * 16 / 360;
                actualAngle8 = LogicUnit.Angle * 8 / 360;
            }

            if (actualVState == UnitVisualState.Rotating || (actualVState == UnitVisualState.Idle && dCls.IdlePhases <= 1))
            {
                actualFrame = actualAngle16;
            }
            else if (actualVState == UnitVisualState.Idle)
            {
                actualFrame = sprites.Frames.Length - dCls.IdlePhases * countFull8 + dCls.IdlePhases * actualAngle8;
                actualFrame += dCls.IdleFrames[LogicUnit.IdleFrame].Frame;
            }
            else if (actualVState == UnitVisualState.Moving)
            {
                int moveSize = dCls.MoveBeginPhases + dCls.MovePhases;

                actualFrame = countFull16 + moveSize * actualAngle8;
                actualFrame += dCls.MoveBeginPhases; // movebeginphases, we don't animate this yet
                actualFrame += dCls.MoveFrames[LogicUnit.MoveFrame].Frame;
            }
            else if (actualVState == UnitVisualState.Attacking)
            {
                int moveSize = dCls.MoveBeginPhases + dCls.MovePhases;
                int attackSize = dCls.AttackPhases;

                actualFrame = countFull16 + moveSize * countFull8 + attackSize * actualAngle8;
                actualFrame += dCls.AttackFrames[LogicUnit.AttackFrame].Frame;
            }
            else if (actualVState == UnitVisualState.Dying)
            {
                int moveSize = dCls.MoveBeginPhases + dCls.MovePhases;
                int attackSize = dCls.AttackPhases;
                int dyingSize = dCls.DyingPhases;

                actualFrame = countFull16 + moveSize * countFull8 + attackSize * countFull8 + dyingSize * actualAngle8;
                actualFrame += LogicUnit.DeathFrame;
            }
            else if (actualVState == UnitVisualState.Bone)
            {
                int moveSize = dCls.MoveBeginPhases + dCls.MovePhases;
                int attackSize = dCls.AttackPhases;
                int dyingSize = dCls.DyingPhases;
                int boneSize = dCls.BonePhases;

                actualFrame = countFull16 + moveSize * countFull8 + attackSize * countFull8 + dyingSize * countFull8 + boneSize * actualAngle8;
                actualFrame += LogicUnit.BoneFrame-1;
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
            UnitMesh = UpdateMesh(sprites, spritesB, actualFrame, Filter.mesh, 0, (UnitMesh == null), doFlip);
            ShadowMesh = UpdateMesh(sprites, spritesB, actualFrame, ShadowFilter.mesh, 0.3f, (ShadowMesh == null), doFlip); // 0.3 of sprite height
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

    public virtual bool ProcessEventPic(Event e, float mousex, float mousey)
    {
        return false;
    }

    public virtual bool ProcessEventInfo(Event e, float mousex, float mousey)
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

    //
    private static AllodsTextRenderer Info_ExperienceCaption; // experience caption
    private static AllodsTextRenderer Info_Experience; // experience value

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
                Info_ExperienceCaption = DisplayInfoInit(Font.Align.Left, 16, 187, 110, 8, colorCaption);
                Info_Experience = DisplayInfoInit(Font.Align.Right, 16, 187, 110, 8, colorValue);
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

            // parts of human info here.
            if (LogicUnit is MapHuman)
            {
                Info_SkillCaptionMain.Text = Locale.Main[27];
                MapHuman human = (MapHuman)LogicUnit;
                if ((human.Gender & MapHuman.GenderFlags.Mage) != 0)
                {
                    Info_SkillCaption.Text = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", Locale.Main[36], Locale.Main[37], Locale.Main[38], Locale.Main[39], Locale.Main[40]);
                    Info_Skill.Text = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", LogicUnit.Stats.SkillFire,
                                                                               LogicUnit.Stats.SkillWater,
                                                                               LogicUnit.Stats.SkillAir,
                                                                               LogicUnit.Stats.SkillEarth,
                                                                               LogicUnit.Stats.SkillAstral);
                }
                else if ((human.Gender & MapHuman.GenderFlags.Fighter) != 0)
                {
                    Info_SkillCaption.Text = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", Locale.Main[30], Locale.Main[31], Locale.Main[32], Locale.Main[33], Locale.Main[34]);
                    Info_Skill.Text = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", LogicUnit.Stats.SkillBlade,
                                                                               LogicUnit.Stats.SkillAxe,
                                                                               LogicUnit.Stats.SkillBludgeon,
                                                                               LogicUnit.Stats.SkillPike,
                                                                               LogicUnit.Stats.SkillShooting);
                }
                else
                {
                    Info_SkillCaptionMain.Text = Locale.Main[28];
                    Info_SkillCaption.Text = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", Locale.Main[30], Locale.Main[31], Locale.Main[32], Locale.Main[33], Locale.Main[34]);
                    Info_Skill.Text = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", LogicUnit.Stats.ProtectionBlade,
                                                                               LogicUnit.Stats.ProtectionAxe,
                                                                               LogicUnit.Stats.ProtectionBludgeon,
                                                                               LogicUnit.Stats.ProtectionPike,
                                                                               LogicUnit.Stats.ProtectionShooting);
                }

                // display exp
                Info_ExperienceCaption.Text = Locale.Main[46];
                Info_Experience.Text = human.GetExperience().ToString();
            }
            else
            {
                Info_SkillCaptionMain.Text = Locale.Main[28];
                Info_SkillCaption.Text = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", Locale.Main[30], Locale.Main[31], Locale.Main[32], Locale.Main[33], Locale.Main[34]);
                Info_Skill.Text = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", LogicUnit.Stats.ProtectionBlade,
                                                                           LogicUnit.Stats.ProtectionAxe,
                                                                           LogicUnit.Stats.ProtectionBludgeon,
                                                                           LogicUnit.Stats.ProtectionPike,
                                                                           LogicUnit.Stats.ProtectionShooting);
                Info_ExperienceCaption.Text = Info_Experience.Text = "";
            }

            Info_ScanSpeedCaption.Text = string.Format("{0}\n{1}", Locale.Main[21], Locale.Main[22]);
            Info_ScanSpeed.Text = string.Format("{0:F1}\n{1}", LogicUnit.Stats.ScanRange, LogicUnit.Stats.Speed);
        }
        else
        {
            if (InfoObject != null)
                InfoObject.SetActive(false);
        }
    }

    public virtual bool ProcessStartDrag(float mousex, float mousey)
    {
        return false;
    }

    public virtual bool ProcessDrag(Item item, float mousex, float mousey)
    {
        return false;
    }

    public virtual bool ProcessDrop(Item item, float mousex, float mousey)
    {
        return false;
    }

    public virtual void ProcessEndDrag()
    {

    }

    public virtual void ProcessFailDrag()
    {

    }

    public virtual Item ProcessVerifyEndDrag()
    {
        return null;
    }
}