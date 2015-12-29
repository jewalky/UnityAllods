using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewChat : MonoBehaviour, IUiEventProcessor, IUiEventProcessorBackground
{
    private static MapViewChat _Instance;
    public static MapViewChat Instance
    {
        get
        {
            if (_Instance == null) _Instance = FindObjectOfType<MapViewChat>();
            return _Instance;
        }
    }

    public bool AlternateColors = false;

    private TextField ChatField;
    private bool ChatFieldEnabled = false;

    private class ChatMessage
    {
        public Color MsgColor = new Color(1, 1, 1, 1); // can have alpha :D
        public string Text = "";
        public float TimeLeft = 0;

        public GameObject Object;
        public AllodsTextRenderer Renderer;

        public void Update()
        {
            if (Object == null || Renderer == null)
            {
                Renderer = new AllodsTextRenderer(Fonts.Font1, Font.Align.Left, Screen.width - 176 - 12, 0, true);
                Renderer.Text = Text;
                Renderer.Material.color = MsgColor;
                Object = Renderer.GetNewGameObject(0.01f, Instance.transform, 100, 1);
            }
        }
    }

    private List<ChatMessage> Messages = new List<ChatMessage>();

    private void UpdateChatMessages()
    {
        for (int i = 0; i < Messages.Count; i++)
        {
            ChatMessage msg = Messages[i];
            msg.TimeLeft -= Time.unscaledDeltaTime;
            if (msg.TimeLeft <= 0)
            {
                msg.Renderer.DestroyImmediate();
                Messages.Remove(msg);
                i--;
                continue;
            }
        }

        int yOffset = 6;
        foreach (ChatMessage msg in Messages)
        {
            msg.Update();
            msg.Object.transform.localPosition = new Vector3(6, yOffset, 0);
            yOffset += msg.Renderer.Height;
        }
    }

    public void AddChatMessage(int color, string text)
    {
        if (AlternateColors)
        {
            switch (color)
            {
                case Player.AllColorsSystem:
                    color = 3;
                    break;
                default:
                    color = Player.AllColorsChat;
                    break;
            }
        }

        // check if too many messages
        int maxMessageCount = Screen.height / 2 / 16;
        if (maxMessageCount <= 0) maxMessageCount = 1;
        if (Messages.Count >= maxMessageCount)
        {
            for (int i = 0; i < Messages.Count - maxMessageCount; i++)
                Messages[i].TimeLeft = 0; // force remove excessive chat
        }

        ChatMessage msg = new ChatMessage();
        msg.MsgColor = Player.AllColors[color];
        msg.Text = text;
        float timeLeftTop = (Messages.Count != 0) ? Messages.Last().TimeLeft : 0;
        msg.TimeLeft = timeLeftTop + 5f; // 5 seconds + top message timeleft, so they disappear in order.
        Messages.Add(msg);
    }

    public void OnDestroy()
    {
        UiManager.Instance.Unsubscribe(this);
    }

    public void Start()
    {
        UiManager.Instance.Subscribe(this);
        transform.localScale = new Vector3(1, 1, 1);
        transform.localPosition = new Vector3(0, 0, MainCamera.InterfaceZ + 0.99f); // on this layer all map UI is drawn

        ChatField = Utils.CreateObjectWithScript<TextField>();
        ChatField.transform.parent = transform;
        ChatField.transform.localPosition = new Vector3(2, Screen.height - Fonts.Font1.LineHeight - 2, -0.001f);
        ChatField.transform.localScale = new Vector3(1, 1, 0.001f);
        ChatField.Font = Fonts.Font1;
        ChatField.Width = Screen.width - 176 - 4;
        ChatField.Height = Fonts.Font1.LineHeight;
        ChatField.OnReturn = () =>
        {
            string text = ChatField.Value;
            ChatField.Value = "";
            ChatField.Visible = false; // hide chat after successful message
            if (text.Trim().Length > 0)
            {
                // handle chat.
                SendChatMessage(text);
            }
        };

        Hide();
    }

    public void Show()
    {
        Utils.SetRendererEnabledWithChildren(gameObject, true);
        ChatFieldEnabled = false;
        ChatField.Value = "";
        ChatField.Visible = false;
    }

    public void Hide()
    {
        Utils.SetRendererEnabledWithChildren(gameObject, false);
        ChatFieldEnabled = false;
        ChatField.Value = "";

        foreach (ChatMessage msg in Messages)
        {
            if (msg.Renderer != null)
                msg.Renderer.DestroyImmediate();
        }

        Messages.Clear();
    }

    public void Update()
    {
        UpdateChatMessages();
    }

    public bool ProcessEvent(Event e)
    {
        if (!MapLogic.Instance.IsLoaded)
            return false;

        if (e.type == EventType.KeyDown)
        {
            switch (e.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    ChatFieldEnabled = true;
                    ChatField.Visible = true;
                    return true;
                case KeyCode.Backspace: // remove all messages
                    foreach (ChatMessage msg in Messages)
                        msg.TimeLeft = 0;
                    return true;
                case KeyCode.Escape:
                    if (ChatFieldEnabled)
                    {
                        ChatFieldEnabled = false;
                        ChatField.Value = "";
                        ChatField.Visible = false;
                        return true;
                    }
                    break;
            }
        }

        if (ChatField.Visible)
            return true; // do not allow events to leak down into the map

        return false;
    }

    public void SendChatMessage(string text)
    {
        if (NetworkManager.IsServer)
            Server.SendChatMessage(text);
        else Client.SendChatMessage(text);
    }
}