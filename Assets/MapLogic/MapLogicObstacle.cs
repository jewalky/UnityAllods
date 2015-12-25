using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapLogicObstacle : MapLogicObject
{
    public override MapLogicObjectType GetObjectType() { return MapLogicObjectType.Obstacle; }
    protected override Type GetGameObjectType() { return typeof(MapViewObstacle); }

    public ObstacleClass Class = null;
    public int CurrentFrame = 0;
    public int CurrentTime = 0;

    public MapLogicObstacle(int typeId)
    {
        Class = ObstacleClassLoader.GetObstacleClassById(typeId);
        if (Class == null)
            Debug.Log(string.Format("Invalid obstacle created (typeId={0})", typeId));
        else InitObstacle();
    }

    public MapLogicObstacle(string name)
    {
        Class = ObstacleClassLoader.GetObstacleClassByName(name);
        if (Class == null)
            Debug.Log(string.Format("Invalid obstacle created (name={0})", name));
        else InitObstacle();
    }

    private void InitObstacle()
    {
        // ???
        DoUpdateView = true;
    }

    public override void Update()
    {
        if (Class == null)
            return;
        // do not animate if visibility != 2, also do not render at all if visibility == 0
        if (Class.Frames.Length > 1 && GetVisibility() == 2)
        {
            CurrentTime++;
            if (CurrentTime > Class.Frames[CurrentFrame].Time)
            {
                CurrentFrame = ++CurrentFrame % Class.Frames.Length;
                CurrentTime = 0;
                DoUpdateView = true;
            }
        }
    }

    public override MapNodeFlags GetNodeLinkFlags(int x, int y)
    {
        return MapNodeFlags.BlockedGround;
    }
}
