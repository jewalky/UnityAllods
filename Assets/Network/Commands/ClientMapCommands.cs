using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ProtoBuf;

namespace ClientCommands
{
    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.AddPlayer)]
    public struct AddPlayer : IClientCommand
    {
        [ProtoMember(1)]
        public int ID;
        [ProtoMember(2)]
        public string Name;
        [ProtoMember(3)]
        public int Color;
        [ProtoMember(4)]
        public long Money;
        [ProtoMember(5)]
        public Dictionary<int, DiplomacyFlags> Diplomacy;
        [ProtoMember(6)]
        public bool Silent; // whether to display the "player has connected" or not.
        [ProtoMember(7)]
        public bool ConsolePlayer; // this is true when the server says us that we have control over this one.

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;
            if (MapLogic.Instance.GetPlayerByID(ID) != null)
                return false; // player already exists: this should NOT happen

            Player player = new Player((ServerClient)null);
            player.ID = ID;
            player.Name = Name;
            player.Color = Color;
            player.Money = Money;
            foreach (var pair in Diplomacy)
                player.Diplomacy[pair.Key] = pair.Value;
            if (ConsolePlayer)
            {
                GameConsole.Instance.WriteLine("We are controlling player {0}.", player.Name);
                MapLogic.Instance.ConsolePlayer = player;
            }
            MapLogic.Instance.AddNetPlayer(player, Silent);
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.DelPlayer)]
    public struct DelPlayer : IClientCommand
    {
        [ProtoMember(1)]
        public int ID;
        [ProtoMember(2)]
        public bool Kick; // whether the "player was kicked" message will be displayed (if Silent is false)
        [ProtoMember(3)]
        public bool Silent;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;
            Player player = MapLogic.Instance.GetPlayerByID(ID);
            if (player == MapLogic.Instance.ConsolePlayer)
                MapLogic.Instance.ConsolePlayer = null;
            if (player != null)
                MapLogic.Instance.DelNetPlayer(player, Silent, Kick);
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.ChatMessage)]
    public struct ChatMessage : IClientCommand
    {
        [ProtoMember(1)]
        public int PlayerID;
        [ProtoMember(2)]
        public string Text;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;
            Player player = (PlayerID > 0) ? MapLogic.Instance.GetPlayerByID(PlayerID) : null;
            int color = (player != null) ? player.Color : Player.AllColorsSystem;
            string text = (player != null) ? player.Name + ": " + Text : "<server>: " + Text;
            MapViewChat.Instance.AddChatMessage(color, text);
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.SpeedChanged)]
    public struct SpeedChanged : IClientCommand
    {
        [ProtoMember(1)]
        public int NewSpeed;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;
            if (NewSpeed != MapLogic.Instance.Speed)
            {
                MapLogic.Instance.Speed = NewSpeed;
                MapViewChat.Instance.AddChatMessage(Player.AllColorsSystem, Locale.Main[108 + NewSpeed]);
            }
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.AddUnit)]
    public struct AddUnit : IClientCommand
    {
        [ProtoMember(1)]
        public int Tag;
        [ProtoMember(2)]
        public int X;
        [ProtoMember(3)]
        public int Y;
        [ProtoMember(4)]
        public int Angle;
        [ProtoMember(5)]
        public int Player;
        [ProtoMember(6)]
        public int ServerID; // this also contains templates
        [ProtoMember(7)]
        public UnitStats CurrentStats;
        [ProtoMember(8)]
        public bool IsAvatar;
        [ProtoMember(9)]
        public UnitVisualState VState;
        [ProtoMember(10)]
        public int IdleFrame;
        [ProtoMember(11)]
        public int IdleTime;
        [ProtoMember(12)]
        public int MoveFrame;
        [ProtoMember(13)]
        public int MoveTime;
        [ProtoMember(14)]
        public float FracX;
        [ProtoMember(15)]
        public float FracY;
        [ProtoMember(16)]
        public int AttackFrame;
        [ProtoMember(17)]
        public int AttackTime;

        public bool Process()
        {
            //Debug.LogFormat("added unit {0}", Tag);
            if (!MapLogic.Instance.IsLoaded)
                return false;
            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            bool newUnit = false;
            if (unit == null)
            {
                unit = new MapUnit(ServerID);
                if (unit.Class == null)
                    return false; // invalid unit created
                unit.Tag = Tag;
                newUnit = true;
            }
            Player player = MapLogic.Instance.GetPlayerByID(Player);
            if (player == null)
            {
                Debug.LogFormat("Unable to resolve player {0} for unit {1}", Player, Tag);
                return false;
            }
            unit.Player = player;
            if (IsAvatar)
                unit.Player.Avatar = unit;
            unit.Actions.RemoveRange(1, unit.Actions.Count - 1); // clear states.
            unit.SetPosition(X, Y);
            unit.Angle = Angle;
            unit.Stats = CurrentStats;
            unit.VState = VState;
            unit.IdleFrame = IdleFrame;
            unit.IdleTime = IdleTime;
            unit.MoveFrame = MoveFrame;
            unit.MoveTime = MoveTime;
            unit.FracX = FracX;
            unit.FracY = FracY;
            unit.AttackFrame = AttackFrame;
            unit.AttackTime = AttackTime;
            if (newUnit)
                MapLogic.Instance.Objects.Add(unit);
            else unit.DoUpdateView = true; // update view if unit already present on map (otherwise its automatically done)
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.DelUnit)]
    public struct DelUnit : IClientCommand
    {
        [ProtoMember(1)]
        public int Tag;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            if (unit == null)
            {
                Debug.LogFormat("Attempted to delete nonexistent unit {0}", Tag);
            }
            else
            {
                unit.Dispose();
                MapLogic.Instance.Objects.Remove(unit);
            }

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.AddUnitActions)]
    public struct AddUnitActions : IClientCommand
    {
        [ProtoContract]
        public class AddUnitAction
        {
            [ProtoMember(1)]
            public RotateAction Rotate;
            [ProtoMember(2)]
            public MoveAction Move;
            [ProtoMember(3)]
            public AttackAction Attack;
            [ProtoMember(4)]
            public int TargetUnitTag;
        }

        public static AddUnitAction GetAddUnitAction(IUnitAction state)
        {
            AddUnitAction aus = new AddUnitAction();
            aus.TargetUnitTag = -1;
            if (state.GetType() == typeof(RotateAction))
                aus.Rotate = (RotateAction)state;
            else aus.Rotate = null;
            if (state.GetType() == typeof(MoveAction))
                aus.Move = (MoveAction)state;
            else aus.Move = null;
            if (state.GetType() == typeof(AttackAction))
            {
                aus.Attack = (AttackAction)state;
                aus.TargetUnitTag = aus.Attack.TargetUnit.Tag;
            }
            else aus.Attack = null;
            return aus;
        }

        [ProtoMember(1)]
        public int Tag;
        [ProtoMember(2)]
        public int Position;
        [ProtoMember(3)]
        public AddUnitAction State1;
        [ProtoMember(4)]
        public AddUnitAction State2;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;
            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            if (unit == null)
            {
                Debug.LogFormat("Attempted to add state for nonexistent unit {0}", Tag);
            }
            else
            {
                // we can't expect ideal sync here.
                // for this reason we don't just "add" state.
                // we put it exactly where it was on server's side at the moment.
                int pPos = Math.Min(Position, unit.Actions.Count);
                // reverse iteration
                AddUnitAction[] States = new AddUnitAction[2] { State1, State2 };
                for (int i = States.Length - 1; i >= 0; i--)
                {
                    if (States[i] == null)
                        continue;

                    if (States[i].Rotate != null)
                    {
                        States[i].Rotate.Unit = unit;
                        unit.Actions.Insert(pPos, States[i].Rotate);
                    }

                    if (States[i].Move != null)
                    {
                        States[i].Move.Unit = unit;
                        unit.Actions.Insert(pPos, States[i].Move);
                    }

                    if (States[i].Attack != null)
                    {
                        States[i].Attack.Unit = unit;
                        MapUnit targetUnit = MapLogic.Instance.GetUnitByTag(States[i].TargetUnitTag);
                        if (targetUnit == null)
                            return false; // bad packet
                        States[i].Attack.TargetUnit = targetUnit;
                        unit.Actions.Insert(pPos, States[i].Attack);
                    }
                }
            }

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.IdleUnit)]
    public struct IdleUnit : IClientCommand
    {
        [ProtoMember(1)]
        public int Tag;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;
            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            if (unit == null)
            {
                Debug.LogFormat("Attempted to idle nonexistent unit {0}", Tag);
            }
            else
            {
                unit.AllowIdle = true;
            }

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.DamageUnit)]
    public struct DamageUnit : IClientCommand
    {
        [ProtoMember(1)]
        public int Tag;
        [ProtoMember(2)]
        public int Damage;
        [ProtoMember(3)]
        public bool Visible;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;
            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            if (unit == null)
            {
                Debug.LogFormat("Attempted to idle nonexistent unit {0}", Tag);
            }
            else
            {
                unit.AllowIdle = true;
            }

            // visible = whether to display flying hp
            unit.Stats.TrySetHealth(unit.Stats.Health - Damage);
            unit.DoUpdateInfo = true;
            unit.DoUpdateView = true;

            if (Visible)
            {
                // todo: display flying hp
            }

            return true;
        }
    }

}
