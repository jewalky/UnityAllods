using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public interface IFocusableWidget
{
    void OnFocus();
    void OnBlur();
}

public class Widget : MonoBehaviour
{
    public Widget Parent
    {
        get
        {
            Transform p = transform.parent;
            if (p == null)
                return null;

            // check if parent is a widget
            Widget pwid = p.gameObject.GetComponent<Widget>();
            if (pwid == null)
                return null;

            return pwid;
        }
    }

    public int Width;
    public int Height;

    private bool _IsFocused = false;
    public bool IsFocused
    {
        get
        {
            return _IsFocused;
        }

        set
        {
            if (value && !_IsFocused)
            {
                // go up and unfocus anything except direct ancestors
                //
                Widget parent = Parent;
                if (parent != null)
                    parent.IsFocused = true;
                UnfocusSiblings();
                if (this is IFocusableWidget)
                    ((IFocusableWidget)this).OnFocus();
            }
            else if (!value && _IsFocused)
            {
                // go down and unfocus children
                UnfocusChildren(null);
                if (this is IFocusableWidget)
                    ((IFocusableWidget)this).OnBlur();
            }

            _IsFocused = value;
        }
    }

    private void UnfocusSiblings()
    {
        Transform p = transform.parent;
        if (p == null)
            return;

        for (int i = 0; i < p.childCount; i++)
        {
            Transform sibling = p.GetChild(i);

            //Debug.LogFormat("child {0}", sibling.gameObject);

            Widget wid = sibling.gameObject.GetComponent<Widget>();
            if (wid == null)
                continue;

            if (wid != this)
                wid.IsFocused = false;
        }

        //pwid.UnfocusChildren(this); // note this doesnt work for windows
    }

    private void UnfocusChildren(Widget exception)
    {
        Transform p = transform;

        for (int i = 0; i < p.childCount; i++)
        {
            Transform sibling = p.GetChild(i);

            Widget wid = sibling.gameObject.GetComponent<Widget>();
            if (wid == null)
                continue;

            if (wid != exception)
                wid.IsFocused = false;
        }
    }
}