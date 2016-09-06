using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class ExampleWindow : Window
{
    public override void OnAwake()
    {
        Width = 5;
        Height = 7;
    }

    private TextField m_TextField;
    private TextField m_TextField2;
    private PushButton m_Button;
    private CheckBox m_Checkbox;

    public override void OnStart()
    {
        m_TextField = Utils.CreateObjectWithScript<TextField>();
        m_TextField.transform.parent = WorkingArea.transform;
        m_TextField.transform.localPosition = new Vector3(0, 0, 0);
        m_TextField.Width = 5 * 96;
        m_TextField.Height = 24;
        m_TextField.Font = Fonts.Font1;
        m_TextField.Value = "test";
        m_TextField.BorderVisible = true;
        m_TextField.Padding = 3;

        m_TextField2 = Utils.CreateObjectWithScript<TextField>();
        m_TextField2.transform.parent = WorkingArea.transform;
        m_TextField2.transform.localPosition = new Vector3(0, 32, 0);
        m_TextField2.Width = 5 * 96;
        m_TextField2.Height = 24;
        m_TextField2.Font = Fonts.Font1;
        m_TextField2.Value = "test";
        m_TextField2.BorderVisible = true;
        m_TextField2.Padding = 3;

        m_Button = Utils.CreateObjectWithScript<PushButton>();
        m_Button.transform.parent = WorkingArea.transform;
        m_Button.transform.localPosition = new Vector3(0, 64, 0);
        m_Button.Width = 128;
        m_Button.Height = 24;
        m_Button.Text = "CLICK";
        m_Button.OnClick = OnButtonClick;

        m_Checkbox = Utils.CreateObjectWithScript<CheckBox>();
        m_Checkbox.transform.parent = WorkingArea.transform;
        m_Checkbox.transform.localPosition = new Vector3(0, 96, 0);
        m_Checkbox.Width = 128;
        m_Checkbox.Height = 24;
        m_Checkbox.Text = "check";
        //m_TextField2.BorderVisible = true;
        //m_TextField2.Padding = 3;
    }

    public void Update()
    {
        
    }

    public void OnButtonClick()
    {
        MessageBox.Show("Жопа. Жопа жопа жопа жопа жопа жопа ЖОПА ЖОПА ЖОПА ЖОПА.", MessageBox.ButtonsType.Ok);
    }
}