using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewObject : MonoBehaviour
{
    private MapLogicObject LogicObject = null;
    public void SetLogicObject(MapLogicObject lo)
    {
        if (LogicObject == null)
            LogicObject = lo;
    }

    void Start()
    {
        Debug.Log(string.Format("started with ID={0}", LogicObject.ID));
    }

    void Update()
    {

    }
}