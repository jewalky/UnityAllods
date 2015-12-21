using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public enum MapLogicObjectType
{
    Object,
    Obstacle,
    Structure,
    Monster,
    Human,
    Effect
}

public class MapLogicObject : IDisposable
{
    public int X = 0;
    public int Y = 0;
    public int Width = 0;
    public int Height = 0;
    public GameObject GameObject = null;
    public const MapLogicObjectType TypePrivate = MapLogicObjectType.Object;
    public readonly int ID = MapLogic.Instance.TopObjectID;

    public MapLogicObject()
    {
        MapLogic.Instance.Objects.Add(this);
        GameObject = MapView.Instance.CreateObject<MapViewObject>(this);
    }

    public void Dispose()
    {
        GameObject.Destroy(GameObject);
    }

    public void Update()
    {
        // this is the global logic update
    }

    public void UnlinkFromWorld(int x = -1, int y = -1)
    {
        if (x < 0 || y < 0)
        {
            x = X;
            y = Y;
        }

        MapNode[] nodes = MapLogic.Instance.Nodes;
        int mw = MapLogic.Instance.Width;
        int mh = MapLogic.Instance.Height;
        for (int ly = Y; ly < Y + Height; ly++)
        {
            for (int lx = X; lx < X + Width; lx++)
            {
                if (lx < 0 || lx >= mw || ly < 0 || ly >= mh)
                    continue;
                nodes[ly * mw + lx].Objects.Remove(this); // if any, obviously.
            }
        }
    }

    public void LinkToWorld(int x = -1, int y = -1)
    {
        if (x < 0 || y < 0)
        {
            x = X;
            y = Y;
        }

        MapNode[] nodes = MapLogic.Instance.Nodes;
        int mw = MapLogic.Instance.Width;
        int mh = MapLogic.Instance.Height;
        for (int ly = Y; ly < Y + Height; ly++)
        {
            for (int lx = X; lx < X + Width; lx++)
            {
                if (lx < 0 || lx >= mw || ly < 0 || ly >= mh)
                    continue;
                nodes[ly * mw + lx].Objects.Add(this); // if any, obviously.
            }
        }
    }
}
