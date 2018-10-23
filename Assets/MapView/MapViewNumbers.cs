using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewNumbers : MapViewObject
{
    private GameObject GObject;
    private AllodsTextRenderer TextRenderer;
    private MeshRenderer Renderer;

    public float X { get; private set; }
    public float Y { get; private set; }
    public float Z { get; private set; }
    public int Number { get; private set; }
    public bool Critical { get; private set; }
    public Player Player { get; private set; }
    public float OffsX { get; private set; }
    public float OffsY { get; private set; }

    private int Count;
    
    //
    public static MapViewNumbers Create(float x, float y, float z, int damage, bool crit, int offsX, int offsY, Player p)
    {
        AllodsTextRenderer textRenderer = new AllodsTextRenderer(Fonts.Font2, Font.Align.Center);
        textRenderer.Text = crit?"CRIT ":""+(-damage).ToString();
        GameObject gObject = textRenderer.GetNewGameObject(0, null, 100);
        MapViewNumbers mvn = gObject.AddComponent<MapViewNumbers>();

        mvn.X = x;
        mvn.Y = y;
        mvn.Z = z;
        mvn.Number = damage;
        mvn.Critical = crit;
        mvn.Player = p;
        mvn.Count = 0;
        mvn.OffsX = offsX;
        mvn.OffsY = offsY;

        mvn.GObject = gObject;
        mvn.TextRenderer = textRenderer;

        mvn.GObject.name = string.Format("MapViewNumbers (damage = {0})", mvn.TextRenderer.Text);

        //
        return mvn;
    }

    public void Start()
    {
        //
        Vector2 coords = MapView.Instance.MapToScreenCoords(X, Y, 1, 1);
        transform.localPosition = new Vector3(coords.x+OffsX, coords.y+OffsY, MakeZFromY(coords.y+OffsY));
        Renderer = GObject.GetComponent<MeshRenderer>();
        Renderer.material.color = Player.AllColors[Player.Color];
    }

    public void Update()
    {
        transform.localPosition = new Vector3(transform.localPosition.x - 0.2f, transform.localPosition.y - 0.4f, transform.localPosition.z);
        Renderer.material.color = new Vector4(Renderer.material.color.r, Renderer.material.color.g, Renderer.material.color.b, Renderer.material.color.a-0.01f);

        //
        Count++;
        if (Count > 80)
        {
            DestroyImmediate(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        
    }
}