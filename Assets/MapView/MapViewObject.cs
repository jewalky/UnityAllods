using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewObject : MonoBehaviour
{
    protected MapLogicObject LogicObject = null;
    public void SetLogicObject(MapLogicObject lo)
    {
        if (LogicObject == null)
            LogicObject = lo;
    }

    public bool IsVisible
    {
        get
        {
            return (MapView.Instance.VisibleRect.Contains(new Vector2(LogicObject.X, LogicObject.Y)));
        }
    }

    protected float MakeZFromY(float y)
    {
        return -((float)y / 16384 + 0.1f);
    }

    public virtual void Start()
    {
        
    }

    public virtual void Update()
    {

    }
}
