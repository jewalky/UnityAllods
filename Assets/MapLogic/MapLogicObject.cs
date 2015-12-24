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
    public readonly int ID = MapLogic.Instance.TopObjectID;
    public bool DoUpdateView = false;

    public virtual MapLogicObjectType GetObjectType() { return MapLogicObjectType.Object; }
    protected virtual Type GetGameObjectType() { return typeof(MapViewObject); }

    public MapLogicObject()
    {
        MapLogic.Instance.Objects.Add(this);
        GameObject = MapView.Instance.CreateObject(GetGameObjectType(), this);
    }

    public void Dispose()
    {
        UnlinkFromWorld();
        if (GameObject != null)
        {
            GameObject.Destroy(GameObject);
            GameObject = null;
        }
    }

    public virtual void Update()
    {
        // this is the global logic update
    }

    public virtual MapNodeFlags GetNodeLinkFlags(int x, int y)
    {
        return 0;
    }

    public virtual void UnlinkFromWorld(int x = -1, int y = -1)
    {
        if (x < 0 || y < 0)
        {
            x = X;
            y = Y;
        }

        MapNode[,] nodes = MapLogic.Instance.Nodes;
        int mw = MapLogic.Instance.Width;
        int mh = MapLogic.Instance.Height;
        for (int ly = Y; ly < Y + Height; ly++)
        {
            for (int lx = X; lx < X + Width; lx++)
            {
                if (lx < 0 || lx >= mw || ly < 0 || ly >= mh)
                    continue;
                nodes[lx, ly].Objects.Remove(this); // if any, obviously.
                nodes[lx, ly].Flags &= ~GetNodeLinkFlags(lx-X, ly-Y);
            }
        }
    }

    public virtual void LinkToWorld(int x = -1, int y = -1)
    {
        if (x < 0 || y < 0)
        {
            x = X;
            y = Y;
        }

        MapNode[,] nodes = MapLogic.Instance.Nodes;
        int mw = MapLogic.Instance.Width;
        int mh = MapLogic.Instance.Height;
        for (int ly = Y; ly < Y + Height; ly++)
        {
            for (int lx = X; lx < X + Width; lx++)
            {
                if (lx < 0 || lx >= mw || ly < 0 || ly >= mh)
                    continue;
                nodes[lx, ly].Objects.Add(this); // if any, obviously.
                nodes[lx, ly].Flags |= GetNodeLinkFlags(lx-X, ly-Y);
            }
        }
    }

    public bool GetVisibityInCamera()
    {
        return !(X > MapView.Instance.VisibleRect.xMax || X + Width < MapView.Instance.VisibleRect.xMin ||
                 Y > MapView.Instance.VisibleRect.yMax || Y + Height < MapView.Instance.VisibleRect.yMin);
    }

    public int GetVisibility()
    {
        if (!GetVisibityInCamera())
            return 0;

        int mw = MapLogic.Instance.Width;
        int mh = MapLogic.Instance.Height;
        int TopFlag = 0;
        for (int ly = Y - 2; ly <= Y + Height + 2; ly++)
        {
            for (int lx = X - 2; lx <= X + Width + 2; lx++)
            {
                if (lx < 0 || lx >= mw || ly < 0 || ly >= mh)
                    continue;
                MapNodeFlags flags = MapLogic.Instance.Nodes[lx, ly].Flags;
                if ((flags & MapNodeFlags.Visible) != 0)
                    return 2;
                else if ((flags & MapNodeFlags.Discovered) != 0)
                    TopFlag = 1;
            }
        }

        return TopFlag;
    }
}
