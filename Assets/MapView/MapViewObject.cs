using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public interface IMapViewSelfie
{
    bool ProcessEventPic(Event e);
    bool ProcessEventInfo(Event e);
    void DisplayPic(bool on, Transform parent);
    void DisplayInfo(bool on, Transform parent); // object displays it's info text at coordinates
}

public interface IMapViewSelectable
{
    bool IsSelected(int x, int y); // mouse coords
}

public class MapViewObject : MonoBehaviour
{
    protected MapLogicObject LogicObject = null;
    public void SetLogicObject(MapLogicObject lo)
    {
        if (LogicObject == null)
            LogicObject = lo;
    }

    protected float MakeZFromY(float y)
    {
        return -y;
    }
}
