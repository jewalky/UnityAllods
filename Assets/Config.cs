using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Config
{
    public static bool MapGridEnabled
    {
        get
        {
            return MapView.Instance.GridEnabled;
        }

        set
        {
            MapView.Instance.GridEnabled = value;
        }
    }

    public static bool ChatAlternateColors
    {
        get
        {
            return MapViewChat.Instance.AlternateColors;
        }

        set
        {
            MapViewChat.Instance.AlternateColors = value;
        }
    }
}