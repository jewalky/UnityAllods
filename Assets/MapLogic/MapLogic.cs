﻿using System.Collections.Generic;
using UnityEngine;
using System;

[Flags]
public enum MapNodeFlags
{
    BlockedGround = 0x0001,
    BlockedAir = 0x0002,
    Discovered = 0x0004, // the cell was visible
    Visible = 0x0008, // the cell is now visible (always Visible+Discovered means open cell, only Discovered means fog of war, neither means cell is black)
    Unblocked = 0x0010 // walkable water / rocks
}

public class MapNode
{
    public short Height = 0;
    public byte Light = 255;
    public int DynLight = 0;
    public ushort Tile = 0;
    public MapNodeFlags Flags = 0;
    public List<MapLogicObject> Objects = new List<MapLogicObject>();
}

class MapLogic
{
    private static MapLogic _Instance = null;
    public static MapLogic Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = new MapLogic();
            return _Instance;
        }
    }

    private MapLogic()
    {
        Objects = new List<MapLogicObject>();
        Players = new List<MapLogicPlayer>();
    }

    private AllodsMap MapStructure = null;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public MapNode[,] Nodes { get; private set; }
    public List<MapLogicObject> Objects { get; private set; }
    private int _TopObjectID = 0;
    public List<MapLogicPlayer> Players { get; private set; }
    public static readonly int MaxPlayers = 1024;

    public bool IsLoaded
    {
        get
        {
            return (MapStructure != null);
        }
    }

    public int TopObjectID
    {
        get
        {
            return _TopObjectID++;
        }
    }

    public string Title
    {
        get
        {
            if (MapStructure != null)
                return MapStructure.Data.Name;
            return "";
        }
    }

    private TerrainLighting MapLighting = null;
    public bool MapLightingNeedsUpdate { get; private set; }
    private Texture2D MapLightingTex = null;
    private Color MapLightingColor = new Color(1, 1, 1, 1);
    private bool MapLightingUpdated = false;

    public void CalculateLighting(float SolarAngle)
    {
        MapLighting.Calculate(MapStructure.Heights, (float)SolarAngle);
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                Nodes[x, y].Light = MapLighting.Result[y * Width + x];
        MapLightingNeedsUpdate = true;
    }

    public void CalculateDynLighting()
    {
        Rect vRec = MapView.Instance.VisibleRect;
        for (int y = (int)vRec.yMin; y < vRec.yMax; y++)
        {
            for (int x = (int)vRec.xMin; x < vRec.xMax; x++)
            {
                Nodes[x, y].DynLight = 0;
                foreach(MapLogicObject mobj in Nodes[x, y].Objects)
                {
                    if (mobj is IMapLogicDynlight)
                        Nodes[x, y].DynLight += ((IMapLogicDynlight)mobj).GetLightValue();
                }
            }
        }
        MapLightingNeedsUpdate = true;
    }

    public Texture2D CheckLightingTexture()
    {
        if (MapLightingUpdated || MapLightingTex == null)
            return GetLightingTexture();
        return null;
    }

    public Texture2D GetLightingTexture()
    {
        if (GameManager.Instance.IsHeadless)
            return null;

        if (MapLightingTex == null)
        {
            MapLightingTex = new Texture2D(256, 256, TextureFormat.Alpha8, false);
            MapLightingTex.filterMode = FilterMode.Bilinear;
            MapLightingNeedsUpdate = true;
        }

        if (MapLightingNeedsUpdate)
        {
            Color[] colors = new Color[256 * 256];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float lvw = (float)(Math.Min(255, Nodes[x, y].Light + Nodes[x, y].DynLight)) / 255; // combine dynlights here. Light is base terrain light.
                    colors[y * 256 + x] = new Color(1, 1, 1, lvw);
                }
            }
            MapLightingTex.SetPixels(colors);
            MapLightingTex.Apply(false);
            MapLightingNeedsUpdate = false;
            MapLightingUpdated = true;
        }

        return MapLightingTex;
    }

    public bool MapFOWNeedsUpdate { get; private set; }
    private Texture2D MapFOWTex = null;
    private bool MapFOWUpdated = false;

    public Texture2D CheckFOWTexture()
    {
        if (MapFOWUpdated || MapFOWTex == null)
            return GetFOWTexture();
        return null;
    }

    public Texture2D GetFOWTexture()
    {
        if (GameManager.Instance.IsHeadless)
            return null;

        if (MapFOWTex == null)
        {
            MapFOWTex = new Texture2D(256, 256, TextureFormat.Alpha8, false);
            MapFOWTex.filterMode = FilterMode.Bilinear;
            MapFOWNeedsUpdate = true;
        }

        if (MapFOWNeedsUpdate)
        {
            Color[] colors = new Color[256 * 256];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    MapNodeFlags flags = Nodes[x, y].Flags;
                    float alpha = 1;
                    if ((flags & MapNodeFlags.Discovered) != 0) alpha -= 0.5f;
                    if ((flags & MapNodeFlags.Visible) != 0) alpha -= 0.5f;
                    colors[y * 256 + x] = new Color(1, 1, 1, alpha);
                }
            }
            MapFOWTex.SetPixels(colors);
            MapFOWTex.Apply(false);
            MapFOWNeedsUpdate = false;
            MapFOWUpdated = true;
        }

        return MapFOWTex;
    }

    private int _LevelTime = 0;
    public int LevelTime
    {
        get
        {
            return _LevelTime;
        }
    }

    private int _Speed = 1;
    public int Speed
    {
        set
        {
            if (value < 0) value = 0;
            if (value > 8) value = 8;
            _Speed = value;
            float scale = 5 * (_Speed+1);
            Time.timeScale = scale;
        }

        get
        {
            return _Speed;
        }
    }

    public void Update()
    {
        _LevelTime++;
        foreach (MapLogicObject mo in Objects)
            mo.Update();
    }

    public void Unload()
    {
        foreach (MapLogicObject mo in Objects)
            mo.Dispose();
        Objects.Clear();
        Players.Clear();
    }

    private void InitGeneric()
    {
        ObstacleClassLoader.InitClasses();
        StructureClassLoader.InitClasses();
        TemplateLoader.LoadTemplates();
        MapLightingNeedsUpdate = true;
        MapFOWNeedsUpdate = true;
    }

    public void InitFromFile(string filename)
    {
        Unload();
        InitGeneric();

        MapStructure = AllodsMap.LoadFrom(filename);
        if (MapStructure == null)
        {
            //Core.Abort("Couldn't load \"{0}\"", filename);
            GameConsole.Instance.WriteLine("Couldn't load \"{0}\"", filename);
            Unload();
            return;
        }

        Width = (int)MapStructure.Data.Width;
        Height = (int)MapStructure.Data.Height;

        Nodes = new MapNode[Width, Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Nodes[x, y] = new MapNode();
                Nodes[x, y].Tile = (ushort)(MapStructure.Tiles[y * Width + x] & 0x3FF);
                Nodes[x, y].Height = MapStructure.Heights[y * Width + x];
                //Nodes[i].Flags = (ushort)(MapStructure.Tiles[y * Width + x] & 0xFC00);
                //Nodes[i].Flags = MapNodeFlags.Discovered;
                Nodes[x, y].Flags = 0;
                Nodes[x, y].Light = 255;
            }
        }

        MapLighting = new TerrainLighting(Width, Height);
        CalculateLighting(180);

        // load players
        foreach (AllodsMap.AlmPlayer almplayer in MapStructure.Players)
        {
            MapLogicPlayer player = new MapLogicPlayer(almplayer);
            Players.Add(player);
            //Debug.Log(string.Format("player ID={2} {0} (flags {1})", player.Name, player.Flags, player.ID));
        }

        Speed = 5;
        _TopObjectID = 0;

        // load obstacles
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int typeId = MapStructure.Objects[y * Width + x];
                if (typeId <= 0) continue;
                typeId -= 1;
                MapLogicObstacle mob = new MapLogicObstacle(typeId);
                mob.X = x;
                mob.Y = y;
                mob.LinkToWorld();
            }
        }

        // load structures
        foreach (AllodsMap.AlmStructure almstruc in MapStructure.Structures)
        {
            MapLogicStructure struc;
            if (almstruc.IsBridge)
            {
                struc = new MapLogicStructure(37); // typeId 37 is "horisontal wooden bridge"
                struc.X = (int)almstruc.X;
                struc.Y = (int)almstruc.Y;
                struc.Health = 0;
                struc.Tag = almstruc.ID;
                struc.Player = GetPlayerByID(almstruc.Player-1);
                struc.IsBridge = true;
                struc.Width = almstruc.Width;
                struc.Health = almstruc.Height;
            }
            else
            {
                struc = new MapLogicStructure(almstruc.TypeID);
                struc.X = (int)almstruc.X;
                struc.Y = (int)almstruc.Y;
                struc.Health = almstruc.Health;
                struc.Tag = almstruc.ID;
                struc.Player = GetPlayerByID(almstruc.Player-1);
            }

            struc.LinkToWorld();
        }
    }

    public int GetFreePlayerID(bool ai)
    {
        int startingFrom = 0;
        if (!ai) startingFrom = 16;
        // tos 
        for (; startingFrom < MaxPlayers; startingFrom++)
        {
            bool used = false;
            foreach (MapLogicPlayer player in Players)
            {
                if (player.ID == startingFrom)
                {
                    used = true;
                    break;
                }
            }

            if (!used) return startingFrom;
        }

        return -1;
    }

    public MapLogicPlayer GetPlayerByID(int id)
    {
        foreach (MapLogicPlayer player in Players)
        {
            if (player.ID == id)
                return player;
        }

        return null;
    }

    public MapLogicPlayer GetPlayerByName(string name)
    {
        foreach (MapLogicPlayer player in Players)
        {
            if (player.Name == name)
                return player;
        }

        return null;
    }

    public void SetTestingVisibility(int x, int y, float range)
    {
        int ri = (int)range;
        // first, delete existing visibility
        for (int ly = 0; ly < Height; ly++)
        {
            for (int lx = 0; lx < Width; lx++)
            {
                Nodes[lx, ly].Flags &= ~MapNodeFlags.Visible;
            }
        }

        for (int ly = y - ri; ly <= y + ri; ly++)
        {
            for (int lx = x - ri; lx <= x + ri; lx++)
            {
                if (lx < 0 || lx >= Width ||
                    ly < 0 || ly >= Height) continue;

                Nodes[lx, ly].Flags |= MapNodeFlags.Visible | MapNodeFlags.Discovered;
            }
        }

        MapFOWNeedsUpdate = true;
    }
}
