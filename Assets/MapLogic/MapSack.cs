using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class MapSack : MapObject
{
    public override MapObjectType GetObjectType() { return MapObjectType.Sack; }
    protected override Type GetGameObjectType() { return typeof(MapViewSack); }

    public ItemPack Pack;
    public int Tag = 0;

    public MapSack()
    {
        Pack = new ItemPack();
        InitSack();
    }

    public MapSack(ItemPack otherpack)
    {
        Pack = new ItemPack(otherpack, false, null);
        InitSack();
    }

    private void InitSack()
    {
        Width = 1;
        Height = 1;
        RenderViewVersion++;
    }

    public override void Update()
    {
        UpdateNetVisibility();
    }
}