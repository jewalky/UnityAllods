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

    protected float MakeZFromY(float y)
    {
        return -y;
    }

    public virtual void Start()
    {
        
    }

    public virtual void Update()
    {

    }
}
