using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewProjectile : MapViewObject, IObjectManualUpdate
{
    private MapProjectile LogicProjectile
    {
        get
        {
            return (MapProjectile)LogicObject;
        }
    }

    private MeshRenderer Renderer;
    private MeshFilter Filter;
    private Mesh ProjectileMesh;

    private GameObject ShadowObject;
    private MeshRenderer ShadowRenderer;
    private MeshFilter ShadowFilter;
    private Mesh ShadowMesh;

    private Mesh UpdateMesh(Images.AllodsSprite sprite, int frame, Mesh mesh, float shadowOffs, bool first, bool flip)
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

        Vector2[] quv = new Vector2[4];
        if (LogicProjectile.Class.ID == 7)
        {
            qv[pp++] = new Vector3(0, 0, 0);
            qv[pp++] = new Vector3(32, 0, 0);
            qv[pp++] = new Vector3(32, 32, 0);
            qv[pp++] = new Vector3(0, 32, 0);

            quv[0] = new Vector2(0, 0);
            quv[1] = new Vector2(1, 0);
            quv[2] = new Vector2(1, 1);
            quv[3] = new Vector2(0, 1);
        }
        else
        {
            qv[pp++] = new Vector3(shadowOffsReal, 0, 0);
            qv[pp++] = new Vector3(shadowOffsReal + sW, 0, 0);
            qv[pp++] = new Vector3(shadowOffsXLeft + sW, sH, 0);
            qv[pp++] = new Vector3(shadowOffsXLeft, sH, 0);

            if (!flip)
            {
                quv[0] = new Vector2(tMinX, tMinY);
                quv[1] = new Vector2(tMaxX, tMinY);
                quv[2] = new Vector2(tMaxX, tMaxY);
                quv[3] = new Vector2(tMinX, tMaxY);
            }
            else
            {
                quv[0] = new Vector2(tMaxX, tMinY);
                quv[1] = new Vector2(tMinX, tMinY);
                quv[2] = new Vector2(tMinX, tMaxY);
                quv[3] = new Vector2(tMaxX, tMaxY);
            }
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
        if (LogicProjectile.Class != null)
            name = string.Format("Projectile (ID={0}, ClassID={1})", LogicProjectile.ID, LogicProjectile.Class.ID);
        else name = string.Format("Projectile (ID={0}, ClassID=<NULL>)", LogicProjectile.ID);

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

    public bool HasShadow
    {
        get
        {
            return false;
        }
    }

    private Texture2D _BasePalette = null;
    private bool spriteSet = false;
    private bool oldVisibility = false;
    private int viewVersion = 0;
    public void OnUpdate()
    {
        if (Renderer == null)
            return;

        if (LogicProjectile.Class == null)
        {
            oldVisibility = false;
            Renderer.enabled = false;
            ShadowRenderer.enabled = false;
            return;
        }

        if (LogicProjectile.GetVisibility() < 2)
        {
            oldVisibility = false;
            Renderer.enabled = false;
            ShadowRenderer.enabled = false;
            return;
        }
        else if (!oldVisibility)
        {
            Renderer.enabled = true;
            ShadowRenderer.enabled = HasShadow;
            oldVisibility = true;
            return;
        }

        if (LogicProjectile.RenderViewVersion != viewVersion)
        {
            Renderer.enabled = true;
            ShadowRenderer.enabled = HasShadow;

            /*if (_ProjectileSprite == null)
                _ProjectileSprite = Images.Load256("graphics/backpack/sprites.256");*/
            LogicProjectile.Class.UpdateSprite();

            Images.AllodsSprite sprites = LogicProjectile.Class.File;

            if (sprites == null || !spriteSet)
            {
                if (LogicProjectile.Class.ID == 7) // bat_sonic attack, apparently hardcoded
                {
                    Renderer.material = new Material(MainCamera.BatShader);
                }
                else
                {
                    Renderer.material = new Material(MainCamera.MainShaderPaletted);
                    if (LogicProjectile.Class.HasPalette)
                        Renderer.material.SetTexture("_Palette", sprites.OwnPalette); // no palette swap for this one
                    else
                    {
                        if (_BasePalette == null) _BasePalette = Images.LoadPalette("graphics/projectiles/projectiles.pal");
                        Renderer.material.SetTexture("_Palette", _BasePalette);
                    }
                    ShadowRenderer.material = Renderer.material;
                    ShadowRenderer.material.color = new Color(0, 0, 0, 0.5f);
                }
                spriteSet = true;
            }

            // This is a hack for prismatic spray support. Maybe something else in the future
            Renderer.material.color = new Color(LogicProjectile.Color.r,
                                                LogicProjectile.Color.g,
                                                LogicProjectile.Color.b,
                                                LogicProjectile.Color.a * LogicProjectile.Alpha);

            int actualFrame = 0;
            // now, projectile frame is RotationPhases * angle + CurrentFrame
            // thus, max count of frames is Count / RotationPhases
            int actualRotationPhases;
            if (LogicProjectile.Class.RotationPhases > 0)
                actualRotationPhases = LogicProjectile.Class.RotationPhases;
            else actualRotationPhases = 16;
            int actualAngle = 0;


            int lPA = (int)(Mathf.Round((float)LogicProjectile.Angle / 45) * 45) % 360;
            // calculate nearest angle

            bool doFlip = false;
            if (actualRotationPhases < 1)
            {
                actualAngle = 0;
            }
            else if (LogicProjectile.Class.Flip)
            {
                actualRotationPhases = actualRotationPhases / 2;
                if (LogicProjectile.Angle < 180)
                {
                    actualAngle = lPA * actualRotationPhases / 180;
                }
                else
                {
                    actualAngle = (180 - (lPA - 180)) * actualRotationPhases / 180;
                    doFlip = true;
                }
            }
            else
            {
                actualAngle = lPA * actualRotationPhases / 360;
            }

            actualAngle %= 16;

            actualFrame = LogicProjectile.Class.Phases * actualAngle + LogicProjectile.CurrentFrame;
            //Debug.LogFormat("actualFrame = {0}, actualAngle = {1}", actualFrame, actualAngle);

            Vector2 xP;
            if (LogicProjectile.ZAbsolute)
            {
                xP = new Vector2(LogicProjectile.ProjectileX * 32, LogicProjectile.ProjectileY * 32);
                xP.y -= LogicProjectile.ProjectileZ * 32;
            }
            else if (LogicProjectile.Class.ID != (int)AllodsProjectile.Lightning)
            {
                xP = MapView.Instance.MapToScreenCoords(LogicProjectile.ProjectileX, LogicProjectile.ProjectileY, 1, 1);
                xP.y -= LogicProjectile.ProjectileZ * 32;
            }
            else
            {
                xP = MapView.Instance.MapToScreenCoords(LogicProjectile.ProjectileX, LogicProjectile.ProjectileY - LogicProjectile.ProjectileZ, 1, 1);
            }

            int projectileWidth = LogicProjectile.Class.Width == 0 ? (int)(sprites.AtlasRects[actualFrame].width * sprites.Atlas.width) : LogicProjectile.Class.Width;
            int projectileHeight = LogicProjectile.Class.Height == 0 ? (int)(sprites.AtlasRects[actualFrame].height * sprites.Atlas.height) : LogicProjectile.Class.Height;

            // always centered
            transform.localPosition = new Vector3(xP.x - projectileWidth * 0.5f * LogicProjectile.Scale,
                                                    xP.y - projectileHeight * 0.5f * LogicProjectile.Scale,
                                                    MakeZFromY(xP.y) - LogicProjectile.ZOffset - LogicProjectile.ProjectileZ * 32); // order sprites by y coordinate basically

            transform.localScale = new Vector3(LogicProjectile.Scale, LogicProjectile.Scale, LogicProjectile.Scale);
            ShadowObject.transform.localScale = new Vector3(LogicProjectile.Scale, LogicProjectile.Scale, LogicProjectile.Scale);

            ProjectileMesh = UpdateMesh(sprites, actualFrame, Filter.mesh, 0, (ProjectileMesh == null), doFlip);
            ShadowMesh = UpdateMesh(sprites, actualFrame, ShadowFilter.mesh, 0.3f, (ShadowMesh == null), doFlip); // 0.3 of sprite height

            viewVersion = LogicProjectile.RenderViewVersion;
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