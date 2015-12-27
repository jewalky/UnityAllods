using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ServerCommands
{
    [Serializable()]
    public struct ChatMessage : IServerCommand
    {
        public string Text;

        public bool Process(ServerClient client)
        {
            if (client.State != ClientState.Playing)
                return false;

            MapLogicPlayer player = MapLogic.Instance.GetNetPlayer(client);
            if (player == null)
                return false; // huehue, same as "order error" in a2server.exe, except we just boot them

            // local chat presentation. on a server.
            int color = player.Color;
            string text = player.Name + ": " + Text;
            MapViewChat.Instance.AddChatMessage(color, text);

            Server.NotifyChatMessage(player, Text);
            return true;
        }
    }
}