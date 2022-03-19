using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public interface IMapViewSelfie
{
    bool ProcessEventPic(Event e, float mousex, float mousey);
    bool ProcessEventInfo(Event e, float mousex, float mousey);
    bool ProcessStartDrag(float mousex, float mousey);
    bool ProcessDrag(Item item, float mousex, float mousey);
    bool ProcessDrop(Item item, float mousex, float mousey);
    void ProcessEndDrag();
    void ProcessFailDrag();
    void ProcessRollbackDrag(Item item);
    Item ProcessVerifyEndDrag();
    void DisplayPic(bool on, Transform parent);
    void DisplayInfo(bool on, Transform parent); // object displays it's info text at coordinates
    MapObject GetObject();
}

public interface IMapViewSelectable
{
    bool IsSelected(int x, int y); // mouse coords
}

public interface IObjectManualUpdate
{
    void OnUpdate();
}

public class MapViewObject : MonoBehaviour
{
    protected MapObject LogicObject = null;
    public void SetLogicObject(MapObject lo)
    {
        if (LogicObject == null)
            LogicObject = lo;
    }

    protected float MakeZFromY(float y)
    {
        return -y;
    }
}
