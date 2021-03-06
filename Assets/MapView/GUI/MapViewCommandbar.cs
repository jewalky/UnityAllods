﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapViewCommandbar : MonoBehaviour, IUiEventProcessor
{
    private MapObject HoveredObject = null;
    private MapObject SelectedObject = null;

    private static Texture2D CommandBarL = null;
    private static Texture2D CommandBarR = null;
    private static Texture2D CommandBarPressed = null;
    private static Texture2D CommandBarEmpty = null;

    private GameObject CommandBarLObject;
    private GameObject CommandBarEmptyObject;

    private GameObject[] CommandBarUpObjects = new GameObject[8];
    private GameObject[] CommandBarDnObjects = new GameObject[8];

    [Flags]
    public enum Commands
    {
        Attack = 0x0001,
        Move = 0x0002,
        Stop = 0x0004,
        Defend = 0x0008,
        Cast = 0x0010,
        MoveAttack = 0x0020,
        HoldPosition = 0x0040,
        Retreat = 0x0080,

        // invisible commands
        Pickup = 0x0100, // this command won't be displayed on the commandbar, instead looking as if there was no command.

        //All = Attack|Move|Stop|Defend|Cast|MoveAttack|HoldPosition|Retreat
        All = Attack|Move|MoveAttack // todo: add more commands when done
    }

    public Commands EnabledCommands = (Commands)0xFF;
    public Commands CurrentCommandActual { get; private set; }
    public Commands CurrentCommand { get; private set; }

    public void Awake()
    {
        UiManager.Instance.Subscribe(this);
    }

    public void Start()
    {
        transform.localScale = new Vector3(1, 1, 0.01f);
        transform.localPosition = new Vector3(MainCamera.Width - 176, 158, MainCamera.InterfaceZ + 0.99f); // on this layer all map UI is drawn

        if (CommandBarL == null) CommandBarL = Images.LoadImage("graphics/interface/commandbarl.bmp", 0, Images.ImageType.AllodsBMP);
        if (CommandBarR == null) CommandBarR = Images.LoadImage("graphics/interface/commandbarr.bmp", Images.ImageType.AllodsBMP);
        if (CommandBarPressed == null) CommandBarPressed = Images.LoadImage("graphics/interface/commanddnr.bmp", Images.ImageType.AllodsBMP);
        if (CommandBarEmpty == null) CommandBarEmpty = Images.LoadImage("graphics/interface/commandempr.bmp", Images.ImageType.AllodsBMP);

        Utils.MakeTexturedQuad(out CommandBarLObject, CommandBarL);
        Utils.MakeTexturedQuad(out CommandBarEmptyObject, CommandBarEmpty);
        CommandBarLObject.transform.parent = transform;
        CommandBarLObject.transform.localPosition = new Vector3(0, 0, 0);
        CommandBarLObject.transform.localScale = new Vector3(1, 1, 1);
        CommandBarEmptyObject.transform.parent = transform;
        CommandBarEmptyObject.transform.localPosition = new Vector3(CommandBarL.width, 0, 0);
        CommandBarEmptyObject.transform.localScale = new Vector3(1, 1, 1);

        for (int i = 0; i < 8; i++)
        {
            int bX = i % 4;
            int bY = i / 4;
            Rect buttonRectRaw = new Rect(8 + 34 * bX, 7 + 34 * bY, 34, 34);
            Rect buttonRectTex = Utils.DivRect(buttonRectRaw, new Vector2(CommandBarPressed.width, CommandBarPressed.height));

            Utils.MakeTexturedQuad(out CommandBarUpObjects[i], CommandBarR, buttonRectTex);
            CommandBarUpObjects[i].transform.parent = CommandBarEmptyObject.transform;
            CommandBarUpObjects[i].transform.localScale = new Vector3(1, 1, 1);
            CommandBarUpObjects[i].transform.localPosition = new Vector3(buttonRectRaw.x, buttonRectRaw.y, -0.02f);
            CommandBarUpObjects[i].SetActive(false);

            Utils.MakeTexturedQuad(out CommandBarDnObjects[i], CommandBarPressed, buttonRectTex);
            CommandBarDnObjects[i].transform.parent = CommandBarEmptyObject.transform;
            CommandBarDnObjects[i].transform.localScale = new Vector3(1, 1, 1);
            CommandBarDnObjects[i].transform.localPosition = new Vector3(buttonRectRaw.x, buttonRectRaw.y, -0.02f);
            CommandBarDnObjects[i].SetActive(false);
        }
    }

    public void OnDestroy()
    {
        UiManager.Instance.Unsubscribe(this);
    }

    public bool ProcessEvent(Event e)
    {
        if (e.rawType == EventType.MouseDown ||
            e.rawType == EventType.MouseUp ||
            e.rawType == EventType.MouseMove)
        {
            Vector2 mPos = Utils.GetMousePosition();
            if (!new Rect(transform.position.x, transform.position.y, CommandBarL.width + CommandBarR.width, CommandBarR.height).Contains(mPos))
                return false;

            mPos.x -= transform.position.x + 8 + CommandBarL.width;
            mPos.y -= transform.position.y + 7;

            if (e.rawType == EventType.MouseDown &&
                e.button == 0)
            {
                int bX = (int)mPos.x / 34;
                int bY = (int)mPos.y / 34;
                if (bX >= 0 && bX < 4 &&
                    bY >= 0 && bY < 2)
                {
                    Commands icmd = (Commands)(1 << (bY * 4 + bX));
                    if ((EnabledCommands & icmd) != 0)
                    {
                        bool wasCast = (CurrentCommandActual == Commands.Cast);
                        CurrentCommandActual = icmd;
                        if (CurrentCommandActual == Commands.Cast)
                            MapView.Instance.SpellbookVisible = true;
                        else if (wasCast)
                            MapView.Instance.SpellbookVisible = false;
                    }
                }
            }

            if (e.rawType == EventType.MouseMove &&
                e.commandName == "tooltip")
            {
                int bX = (int)mPos.x / 34;
                int bY = (int)mPos.y / 34;
                if (bX >= 0 && bX < 4 &&
                    bY >= 0 && bY < 2)
                {
                    Commands icmd = (Commands)(1 << (bY * 4 + bX));
                    if ((EnabledCommands & icmd) != 0)
                        UiManager.Instance.SetTooltip(Locale.Main[bY * 4 + bX]);
                }
            }

            MouseCursor.SetCursor(MouseCursor.CurDefault);

            return true;
        }

        return false;
    }

    public bool ProcessCustomEvent(CustomEvent ce)
    {
        if (ce is MapViewSelectionChanged sel)
        {
            SelectedObject = sel.NewSelection.Count > 0 ? sel.NewSelection[0] : null;
            InitDefault(SelectedObject);
        }
        else if (ce is MapViewHoverChanged h)
        {
            HoveredObject = h.NewHover;
        }
        return false;
    }

    public void Update()
    {
        CurrentCommandActual &= EnabledCommands;
        CurrentCommand = CurrentCommandActual;

        Spell.Spells viewCast = MapView.Instance.GetCastSpell();
        if (viewCast != Spell.Spells.NoneSpell)
        {
            CurrentCommand = Commands.Cast;
        }
        else if (CurrentCommandActual == Commands.Move)
        {
            // check if ctrl is pressed.
            // if alt is pressed, it's always Move overriding any other command.
            bool bAlt = (Input.GetKey(KeyCode.RightAlt) ||
                        Input.GetKey(KeyCode.LeftAlt));
            bool bCtrl = (Input.GetKey(KeyCode.RightControl) ||
                          Input.GetKey(KeyCode.LeftControl));

            if (bAlt && !bCtrl)
            {
                CurrentCommand = Commands.Move;
            }
            else if (bCtrl && !bAlt)
            {
                CurrentCommand = (HoveredObject != null &&
                                  (HoveredObject.GetObjectType() == MapObjectType.Monster ||
                                   HoveredObject.GetObjectType() == MapObjectType.Human)) ? Commands.Attack : Commands.MoveAttack;
            }
            else
            {
                // get own player
                Player ownPlayer = MapLogic.Instance.ConsolePlayer;
                Player hisPlayer = (HoveredObject != null &&
                                    (HoveredObject.GetObjectType() == MapObjectType.Monster ||
                                     HoveredObject.GetObjectType() == MapObjectType.Human)) ? ((IPlayerPawn)HoveredObject).GetPlayer() : null;
                if (ownPlayer != null && hisPlayer != null)
                {
                    if ((ownPlayer.Diplomacy[hisPlayer.ID] & DiplomacyFlags.Enemy) != 0)
                        CurrentCommand = Commands.Attack;
                }

                // check pickup
                // if selected unit == hovered unit
                // AND selected unit belongs to us
                // AND we are hovering a pack
                if (CurrentCommand == Commands.Move)
                {
                    MapSack sack = MapLogic.Instance.GetSackAt(MapView.Instance.MouseCellX, MapView.Instance.MouseCellY);
                    if (sack != null &&
                        (HoveredObject == SelectedObject || HoveredObject == null) &&
                        SelectedObject != null &&
                        SelectedObject is IPlayerPawn &&
                        ((IPlayerPawn)SelectedObject).GetPlayer() == ownPlayer)
                    {
                        CurrentCommand = Commands.Pickup;
                    }
                }
            }
        }

        // update visual button states
        for (int i = 0; i < 8; i++)
        {
            Commands icmd = (Commands)(1 << i);
            if ((EnabledCommands & icmd) != 0)
            {
                CommandBarDnObjects[i].SetActive(CurrentCommand == icmd);
                CommandBarUpObjects[i].SetActive(CurrentCommand != icmd);
            }
            else
            {
                CommandBarDnObjects[i].SetActive(false);
                CommandBarUpObjects[i].SetActive(false);
            }
        }
    }

    public void InitDefault(MapObject pp)
    {
        EnabledCommands = 0;
        if (pp == null)
            return;

        if ((pp is IPlayerPawn && ((IPlayerPawn)pp).GetPlayer() == MapLogic.Instance.ConsolePlayer) &&
            (pp.GetObjectType() == MapObjectType.Monster ||
             pp.GetObjectType() == MapObjectType.Human))
        {
            EnabledCommands = (Commands.All & ~Commands.Cast);
            // enable cast if object has spells or scrolls
            bool haveSpells = false;
            MapUnit mu = (MapUnit)pp;
            for (int i = 0; i < 32; i++)
            {
                if (mu.GetSpell((Spell.Spells)i) != null)
                {
                    haveSpells = true;
                    break;
                }
            }
            if (haveSpells)
                EnabledCommands |= Commands.Cast;
            CurrentCommandActual = Commands.Move;
        }
    }
}