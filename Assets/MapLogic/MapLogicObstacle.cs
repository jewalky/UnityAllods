using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class MapLogicObstacle : MapLogicObject
{
    public override MapLogicObjectType GetObjectType() { return MapLogicObjectType.Obstacle; }
    protected override Type GetGameObjectType() { return typeof(MapViewObstacle); }

    public ObstacleClass Class = null;
    public int CurrentFrame = 0;
    public int CurrentTime = 0;

    public MapLogicObstacle(int typeId) : base()
    {
        Class = ObstacleClassLoader.GetObstacleClassById(typeId);
        InitObstacle();
    }

    public MapLogicObstacle(string name) : base()
    {
        Class = ObstacleClassLoader.GetObstacleClassByName(name);
        InitObstacle();
    }

    private void InitObstacle()
    {
        // ???
        DoUpdateView = true;
    }

    public override void Update()
    {
        if (Class.Frames.Length > 1)
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
}
