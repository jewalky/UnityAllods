using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class InnScreen : FullscreenView
{

    public MapStructure Inn;

    public override void OnStart()
    {
        
    }

    public override bool ProcessEvent(Event e)
    {
        if (e.type == EventType.KeyDown)
        {
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    Client.SendLeaveStructure();
                    break;
            }
        }
        else if (e.rawType == EventType.MouseMove)
        {
            MouseCursor.SetCursor(MouseCursor.CurDefault);
            return true;
        }

        return base.ProcessEvent(e);
    }

    public override bool ProcessCustomEvent(CustomEvent ce)
    {
        return base.ProcessCustomEvent(ce);
    }

}

