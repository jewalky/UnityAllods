using UnityEngine;

public class MessageBox : Window
{
    public override void OnAwake()
    {
        Width = 2;
        Height = 2;
    }

    public enum ButtonsType
    {
        None,
        Ok,
        YesNo,
        AbortRetryIgnore
    }

    public string Text = "<MessageBox>";
    public ButtonsType Type = ButtonsType.None;

    private TextLabel m_TextLabel;
    private PushButton m_Button1;
    private PushButton m_Button2;
    private PushButton m_Button3;

    public delegate void ButtonHandler();
    public ButtonHandler OnButton1Click;
    public ButtonHandler OnButton2Click;
    public ButtonHandler OnButton3Click;

    public override void OnStart()
    {
        m_TextLabel = Utils.CreateObjectWithScript<TextLabel>();
        m_TextLabel.transform.parent = WorkingArea.transform;
        m_TextLabel.transform.localPosition = new Vector3(0, 0, 0);
        m_TextLabel.Width = Width * 96;
        m_TextLabel.Height = Height * 64;
        m_TextLabel.Font = Fonts.Font1;
        m_TextLabel.Value = Text;

        switch (Type)
        {
            case ButtonsType.Ok:
                {
                    m_Button1 = Utils.CreateObjectWithScript<PushButton>();
                    m_Button1.transform.parent = WorkingArea.transform;
                    m_Button1.transform.localPosition = new Vector3(Width*96/2-64, Height*64-12, 0);
                    m_Button1.Width = 128;
                    m_Button1.Height = 24;
                    m_Button1.Text = Locale.Dialogs[0];
                    m_Button1.OnClick = OnLocalButton1Click;
                    m_Button1.IsFocused = true;
                    OnReturn = OnLocalButton1Click;
                    break;
                }

            case ButtonsType.YesNo:
                {
                    int buttonwd = Width * 96 / 2 - 16;
                    m_Button1 = Utils.CreateObjectWithScript<PushButton>();
                    m_Button1.transform.parent = WorkingArea.transform;
                    m_Button1.transform.localPosition = new Vector3(0, Height * 64 - 12, 0);
                    m_Button1.Width = buttonwd;
                    m_Button1.Height = 24;
                    m_Button1.Text = Locale.Main[75];
                    m_Button1.OnClick = OnLocalButton1Click;

                    m_Button2 = Utils.CreateObjectWithScript<PushButton>();
                    m_Button2.transform.parent = WorkingArea.transform;
                    m_Button2.transform.localPosition = new Vector3(buttonwd+32, Height * 64 - 12, 0);
                    m_Button2.Width = buttonwd;
                    m_Button2.Height = 24;
                    m_Button2.Text = Locale.Main[76];
                    m_Button2.OnClick = OnLocalButton2Click;
                    m_Button2.IsFocused = true;
                    OnReturn = OnLocalButton2Click;
                    break;
                }

            case ButtonsType.AbortRetryIgnore:
                {
                    m_Button1 = Utils.CreateObjectWithScript<PushButton>();
                    m_Button1.transform.parent = WorkingArea.transform;
                    m_Button1.transform.localPosition = new Vector3(-18, Height * 64 - 12, 0);
                    m_Button1.Width = 96;
                    m_Button1.Height = 24;
                    m_Button1.Text = Locale.Dialogs[2];
                    m_Button1.OnClick = OnLocalButton1Click;

                    m_Button2 = Utils.CreateObjectWithScript<PushButton>();
                    m_Button2.transform.parent = WorkingArea.transform;
                    m_Button2.transform.localPosition = new Vector3(-18 + 96 + 8, Height * 64 - 12, 0);
                    m_Button2.Width = 96;
                    m_Button2.Height = 24;
                    m_Button2.Text = Locale.Dialogs[3];
                    m_Button2.OnClick = OnLocalButton2Click;
                    m_Button2.IsFocused = true;
                    OnReturn = OnLocalButton2Click;

                    m_Button3 = Utils.CreateObjectWithScript<PushButton>();
                    m_Button3.transform.parent = WorkingArea.transform;
                    m_Button3.transform.localPosition = new Vector3(-18 + 96 + 8 + 96 + 8, Height * 64 - 12, 0);
                    m_Button3.Width = 120;
                    m_Button3.Height = 24;
                    m_Button3.Text = Locale.Dialogs[4];
                    m_Button3.OnClick = OnLocalButton3Click;
                    break;
                }
        }
    }

    private void OnLocalButton1Click()
    {
        if (OnButton1Click != null) OnButton1Click();
        Destroy(gameObject);
    }

    private void OnLocalButton2Click()
    {
        if (OnButton2Click != null) OnButton2Click();
        Destroy(gameObject);
    }

    private void OnLocalButton3Click()
    {
        if (OnButton3Click != null) OnButton3Click();
        Destroy(gameObject);
    }

    public static MessageBox Show(string text, ButtonsType type, ButtonHandler button1 = null, ButtonHandler button2 = null, ButtonHandler button3 = null)
    {
        MessageBox bx = Utils.CreateObjectWithScript<MessageBox>();
        if (type == ButtonsType.AbortRetryIgnore)
            bx.Width = 3;
        bx.Text = text;
        bx.Type = type;
        bx.OnButton1Click = button1;
        bx.OnButton2Click = button2;
        bx.OnButton3Click = button3;
        return bx;
    }
}