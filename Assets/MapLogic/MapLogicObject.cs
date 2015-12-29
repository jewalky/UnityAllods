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

public interface IMapLogicDynlight
{
    int GetLightValue();
}

public interface IMapLogicPlayerPawn
{
    MapLogicPlayer GetPlayer();
}

public class MapLogicObject : IDisposable
{
    public int X = 0;
    public int Y = 0;
    public int Width = 0;
    public int Height = 0;
    public GameObject GameObject = null;
    public MonoBehaviour GameScript = null;
    public readonly int ID = MapLogic.Instance.TopObjectID;
    public bool DoUpdateView = false;

    public virtual MapLogicObjectType GetObjectType() { return MapLogicObjectType.Object; }
    protected virtual Type GetGameObjectType() { return typeof(MapViewObject); }

    public ulong NetPlayerVisibility = 0;
    public bool IsVisibleForNetPlayer(MapLogicPlayer player)
    {
        int netId = player.ID - 16;
        ulong mask = NetPlayerVisibility & (1ul << netId);
        return (NetPlayerVisibility & mask) != 0;
    }

    public void SetVisibleForNetPlayer(MapLogicPlayer player, bool visible)
    {
        int netId = player.ID - 16;
        ulong mask = NetPlayerVisibility & (1ul << netId);
        if (visible) NetPlayerVisibility |= mask;
        else NetPlayerVisibility &= ~mask;
    }

    public MapLogicObject()
    {
        GameObject = MapView.Instance.CreateObject(GetGameObjectType(), this);
        GameScript = (MonoBehaviour)GameObject.GetComponent(GetGameObjectType());
    }

    public virtual void Dispose()
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

    public int GetVisibilityInFOW()
    {
        int extRadius = (GetObjectType() == MapLogicObjectType.Obstacle) ? 2 : 0;
        int mw = MapLogic.Instance.Width;
        int mh = MapLogic.Instance.Height;
        int TopFlag = 0;
        for (int ly = Y - extRadius; ly <= Y + Height + extRadius; ly++)
        {
            for (int lx = X - extRadius; lx <= X + Width + extRadius; lx++)
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

    public int GetVisibility()
    {
        if (!GetVisibityInCamera())
            return 0;
        return GetVisibilityInFOW();
    }

    public void UpdateNetVisibility()
    {
        // perform simple multiplayer vision check
        // this vision check is only used to determine whether multiplayer clients receive info about this building.
        // clients "see" everything that server allows them to see. local player just sees everything. so this only makes sense for servers.
        if (!NetworkManager.IsServer)
            return;

        ulong oldPlayerVisibility = NetPlayerVisibility;
        NetPlayerVisibility = 0;
        foreach (MapLogicPlayer player in MapLogic.Instance.Players)
        {
            if ((player.Flags & MapLogicPlayerFlags.NetClient) == 0)
                continue;

            bool wasVisibleForPlayer = IsVisibleForNetPlayer(player);
            bool isVisibleForPlayer = false;
            foreach (MapLogicObject playerMobj in player.Objects)
            {
                if (Math.Abs(playerMobj.X - X) > 30 ||
                    Math.Abs(playerMobj.Y - Y) > 30) continue; // basic coordinate check in 60x60 square with player unit in the center
                isVisibleForPlayer = true;
                break;
            }

            if (isVisibleForPlayer && !wasVisibleForPlayer)
            {
                SetVisibleForNetPlayer(player, true);
                Server.ObjectBecameVisible(player, this);
            }
            else if (!isVisibleForPlayer && wasVisibleForPlayer)
            {
                SetVisibleForNetPlayer(player, false);
                Server.ObjectBecameInvisible(player, this);
            }
        }
    }
}
