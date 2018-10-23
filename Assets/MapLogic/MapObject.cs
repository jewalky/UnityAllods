using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public enum MapObjectType
{
    Object,
    Obstacle,
    Structure,
    Monster,
    Human,
    Effect,
    Sack
}

public interface IDynlight
{
    int GetLightValue();
}

public interface IPlayerPawn
{
    Player GetPlayer();
}

[Flags]
public enum DamageFlags
{
    TerrainDamage = 0x0001,
    Fire = 0x0002,
    Air = 0x0004,
    Water = 0x0008,
    Earth = 0x0010,
    Astral = 0x0020,
    Blade = 0x0040,
    Axe = 0x0080,
    Bludgeon = 0x0100,
    Pike = 0x0200,
    Shooting = 0x0400,
    Raw = 0x0800,

    PhysicalDamage = Blade|Axe|Bludgeon|Pike|Shooting|Raw,
    MagicDamage = Fire|Air|Water|Earth|Astral
}

public interface IVulnerable
{
    int TakeDamage(DamageFlags flags, MapUnit source, int count);
}

public class MapObject : IDisposable
{
    public int X = 0;
    public int Y = 0;
    public int Width = 0;
    public int Height = 0;
    public GameObject GameObject = null;
    public MonoBehaviour GameScript = null;
    public readonly int ID = MapLogic.Instance.TopObjectID;
    public bool DoUpdateView = false;
    public bool DoUpdateInfo = false;
    public bool IsLinked { get; private set; }

    public virtual MapObjectType GetObjectType() { return MapObjectType.Object; }
    protected virtual Type GetGameObjectType() { return typeof(MapViewObject); }

    public ulong NetPlayerVisibility = 0;
    public bool IsVisibleForNetPlayer(Player player)
    {
        if (player == null)
            return false;
        int netId = player.ID - 16;
        ulong mask = (1ul << netId);
        return (NetPlayerVisibility & mask) != 0;
    }

    public void SetVisibleForNetPlayer(Player player, bool visible)
    {
        if (player == null)
            return;
        int netId = player.ID - 16;
        ulong mask = (1ul << netId);
        if (visible) NetPlayerVisibility |= mask;
        else NetPlayerVisibility &= ~mask;
    }

    public MapObject()
    {
        GameManager.Instance.CallDelegateOnNextFrame(() =>
        {
            GameObject = MapView.Instance.CreateObject(GetGameObjectType(), this);
            GameScript = (MonoBehaviour)GameObject.GetComponent(GetGameObjectType());
            return false;
        });
    }

    public void DisposeNoUnlink()
    {
        if (NetworkManager.IsServer)
            Server.NotifyDelObject(this);
        if (GameObject != null)
        {
            GameManager.Instance.CallDelegateOnNextFrame(() =>
            {
                GameObject.Destroy(GameObject);
                GameObject = null;
                return false;
            });
        }
    }

    public virtual void Dispose()
    {
        UnlinkFromWorld();
        DisposeNoUnlink();
        MapLogic.Instance.Objects.Remove(this);
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
        IsLinked = false;

        if (x < 0 || y < 0)
        {
            x = X;
            y = Y;
        }

        MapNode[,] nodes = MapLogic.Instance.Nodes;
        int mw = MapLogic.Instance.Width;
        int mh = MapLogic.Instance.Height;
        for (int ly = y - 1; ly < y + Height + 1; ly++)
        {
            for (int lx = x - 1; lx < x + Width + 1; lx++)
            {
                if (lx < 0 || lx >= mw || ly < 0 || ly >= mh)
                    continue;
                MapNode node = nodes[lx, ly];
                if (node.Objects.Contains(this))
                {
                    node.Objects.Remove(this); // if any, obviously.
                    node.Flags &= ~GetNodeLinkFlags(lx - X, ly - Y);
                }
            }
        }
    }

    public virtual void LinkToWorld(int x = -1, int y = -1)
    {
        IsLinked = true;

        if (x < 0 || y < 0)
        {
            x = X;
            y = Y;
        }

        MapNode[,] nodes = MapLogic.Instance.Nodes;
        int mw = MapLogic.Instance.Width;
        int mh = MapLogic.Instance.Height;
        for (int ly = y; ly < y + Height; ly++)
        {
            for (int lx = x; lx < x + Width; lx++)
            {
                if (lx < 0 || lx >= mw || ly < 0 || ly >= mh)
                    continue;
                MapNode node = nodes[lx, ly];
                if (!node.Objects.Contains(this))
                    node.Objects.Add(this); // if any, obviously.
                node.Flags |= GetNodeLinkFlags(lx-X, ly-Y);
            }
        }
    }

    public bool GetVisibityInCamera()
    {
        return !(X > MapView.Instance.VisibleRect.xMax || X + Width < MapView.Instance.VisibleRect.xMin ||
                 Y > MapView.Instance.VisibleRect.yMax || Y + Height < MapView.Instance.VisibleRect.yMin);
    }

    public virtual int GetVisibilityInFOW()
    {
        int extRadius = (GetObjectType() == MapObjectType.Obstacle) ? 2 : 0;
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

        Player ownPlayer = (this is IPlayerPawn) ? ((IPlayerPawn)this).GetPlayer() : null;
        foreach (Player player in MapLogic.Instance.Players)
        {
            if (player.NetClient == null)
                continue;

            bool wasVisibleForPlayer = IsVisibleForNetPlayer(player);
            bool isVisibleForPlayer = false;
            if (player == ownPlayer)
            {
                isVisibleForPlayer = true;
            }
            else
            {
                // check if invisible
                // [ZZ] todo: check if this is actually ok in real usage
                /*if (this is MapUnit &&
                    (((MapUnit)this).Flags & UnitFlags.Invisible) != 0 &&
                    ownPlayer != null &&
                    (ownPlayer.Diplomacy[player.ID] & DiplomacyFlags.Vision) == 0)
                {
                    isVisibleForPlayer = false;
                }
                else*/
                {
                    foreach (MapObject playerMobj in player.Objects)
                    {
                        if (Math.Abs(playerMobj.X - X) > 30 ||
                            Math.Abs(playerMobj.Y - Y) > 30) continue; // basic coordinate check in 60x60 square with player unit in the center
                        isVisibleForPlayer = true;
                        break;
                    }
                }
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

    public static int FaceVector(float dx, float dy)
    {
        float deltaY = dy;
        float deltaX = dx;
        int sang = (int)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI) - 90;
        while (sang > 360)
            sang -= 360;
        while (sang < 0)
            sang += 360;
        return sang;
    }
}
