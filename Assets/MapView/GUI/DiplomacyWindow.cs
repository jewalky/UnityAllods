using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class DiplomacyWindow : Window
{
    private List<GameObject> BorderContents = new List<GameObject>();
    private List<AllodsTextRenderer> PlayerNames = new List<AllodsTextRenderer>();

    public override void OnAwake()
    {
        Width = 5;
        Height = 7;
    }

    public override void OnStart()
    {
        // "Diplomacy" title
        AllodsTextRenderer tr_Diplomacy = new AllodsTextRenderer(Fonts.Font1, Font.Align.Center, Width * 96, 16, false);
        tr_Diplomacy.Text = Locale.Dialogs[145];
        tr_Diplomacy.Material.color = new Color32(214, 211, 214, 255); // gray interface color
        GameObject go_Diplomacy = tr_Diplomacy.GetNewGameObject(0.01f, WorkingArea.transform, 100);
        go_Diplomacy.transform.localPosition = new Vector3(0, 0); // move slightly above.

        // Diplomacy subtitle: "Player .. enemy .. ally .. vision .. ignore"
        AllodsTextRenderer tr_Columns = new AllodsTextRenderer(Fonts.Font1, Font.Align.Left, Width * 96, 16, false);
        tr_Columns.Text = Locale.Dialogs[79];
        tr_Columns.Material.color = new Color32(189, 158, 74, 255);
        GameObject go_Columns = tr_Columns.GetNewGameObject(0.01f, WorkingArea.transform, 100);
        go_Columns.transform.localPosition = new Vector3(-8, 28);

        for (int i = 0; i < 16; i++)
        {
            int y = i * 25 + 28 + 18;

            FieldBorder border = Utils.CreateObjectWithScript<FieldBorder>();
            border.transform.parent = WorkingArea.transform;
            border.transform.localPosition = new Vector3(-8, y);
            border.Width = Width * 96 + 16;
            border.Height = 24;

            GameObject contents = Utils.CreateObject();
            contents.transform.parent = border.transform;
            contents.transform.localPosition = new Vector3(0, 0, 0);
            BorderContents.Add(contents);

            AllodsTextRenderer tr_PlayerName = new AllodsTextRenderer(Fonts.Font1, Font.Align.Left, border.Width, 16, false);
            GameObject go_PlayerName = tr_PlayerName.GetNewGameObject(0.01f, contents.transform, 100);
            go_PlayerName.transform.localPosition = new Vector3(4, 3);
            PlayerNames.Add(tr_PlayerName);
        }

        Update();
    }

    public void Update()
    {
        // update player names accordingly.
        //
        List<Player> players = new List<Player>();
        foreach (Player player in MapLogic.Instance.Players)
        {
            if ((player.Flags & PlayerFlags.AI) == 0)
                players.Add(player);
        }

        bool erase = false;
        for (int i = 0; i < PlayerNames.Count; i++)
        {
            if (erase)
            {
                BorderContents[i].SetActive(false);
                continue;
            }

            int playerId = i;
            Player player = (playerId >= 0 && playerId < players.Count) ? players[playerId] : null;
            if (player == null)
            {
                erase = true;
                i--;
                continue;
            }

            BorderContents[i].SetActive(true);
            PlayerNames[i].Text = player.Name;
        }
    }
}