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

    public override void Start()
    {
        name = string.Format("Obstacle (ID={0}, Class={1})", LogicObstacle.ID, LogicObstacle.Class.DescText);
        // let's give ourselves a sprite renderer first.
        SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
        sr.material = new Material(Shader.Find("Custom/MainShaderPaletted"));
        transform.localScale = new Vector3(100, 100, 1);
    }

    private bool spriteSet = false;

    public override void Update()
    {
        if (LogicObstacle.DoUpdateView)
        {
            SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();

            LogicObstacle.Class.File.UpdateSprite();
            Images.AllodsSprite sprites = LogicObstacle.Class.File.File;

            int actualFrame = LogicObstacle.Class.Frames[LogicObstacle.CurrentFrame].Frame + LogicObstacle.Class.Index;
            Vector2 xP = MapView.Instance.LerpCoords(LogicObject.X + 0.5f, LogicObject.Y + 0.5f);
            transform.localPosition = new Vector3(xP.x - sprites.Sprites[actualFrame].rect.width * LogicObstacle.Class.CenterX,
                                                    xP.y - sprites.Sprites[actualFrame].rect.height * LogicObstacle.Class.CenterY,
                                                    MakeZFromY(xP.y)); // order sprites by y coordinate basically
            //Debug.Log(string.Format("{0} {1} {2}", xP.x, sprites.Sprites[0].rect.width, LogicObstacle.Class.CenterX));
            sr.sprite = sprites.Sprites[actualFrame];

            if (!spriteSet)
            {
                sr.material.SetTexture("_Palette", sprites.OwnPalette); // no palette swap for this one
                spriteSet = true;
            }

            LogicObstacle.DoUpdateView = false;
        }
    }
}