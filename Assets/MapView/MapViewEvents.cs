using System;
using System.Collections.Generic;

public class MapLoadedEvent : CustomEvent
{
    public MapLoadedEvent() { IsGlobal = true; IsForced = true; }
}
public class MapUnloadedEvent : CustomEvent
{
    public MapUnloadedEvent() { IsGlobal = true; IsForced = true; }
}

public class MapViewSelectionChanged : CustomEvent
{
    public List<MapObject> NewSelection;

    public MapViewSelectionChanged(List<MapObject> selection)
    {
        NewSelection = selection;
        IsGlobal = true;
        IsForced = true;
    }
}

public class MapViewHoverChanged : CustomEvent
{
    public MapObject NewHover;

    public MapViewHoverChanged(MapObject hover)
    {
        NewHover = hover;
        IsGlobal = true;
        IsForced = true;
    }
}