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
            Debug.LogFormat("Added Player {0}", player.ID);
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
        public global::UnitStats CurrentStats;
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
        [ProtoMember(18)]
        public int DeathFrame;
        [ProtoMember(19)]
        public int DeathTime;
        [ProtoMember(20)]
        public bool IsAlive;
        [ProtoMember(21)]
        public bool IsDying;
        [ProtoMember(22)]
        public bool IsHuman;
        [ProtoMember(23)]
        public bool IsHero;
        [ProtoMember(24)]
        public List<NetItem> ItemsBody;
        [ProtoMember(25)]
        public List<NetItem> ItemsPack;
        [ProtoMember(26)]
        public uint SpellBook;
        [ProtoMember(27)]
        public global::UnitFlags Flags;

        public bool Process()
        {
            //Debug.LogFormat("added unit {0}", Tag);
            if (!MapLogic.Instance.IsLoaded)
                return false;
            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            bool newUnit = false;
            if (unit == null)
            {
                if (IsHuman)
                    unit = new MapHuman(ServerID, IsHero);
                else unit = new MapUnit(ServerID);
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
            unit.IsAlive = IsAlive;
            unit.IsDying = IsDying;
            unit.SetPosition(X, Y, true);
            if (IsAvatar)
            {
                unit.Player.Avatar = unit;
                if (player == MapLogic.Instance.ConsolePlayer)
                    MapView.Instance.CenterOnObject(unit);
            }
            unit.Actions.RemoveRange(1, unit.Actions.Count - 1); // clear states.
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
            unit.DeathFrame = DeathFrame;
            unit.DeathTime = DeathTime;

            unit.ItemsBody.Clear();
            if (ItemsBody != null)
            {
                for (int i = 0; i < ItemsBody.Count; i++)
                    unit.ItemsBody.PutItem(unit.ItemsBody.Count, new Item(ItemsBody[i]));
            }
            unit.UpdateItems();

            unit.ItemsPack.Clear();
            if (ItemsPack != null)
            {
                for (int i = 0; i < ItemsPack.Count; i++)
                    unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item(ItemsPack[i]));
            }

            if (newUnit)
                MapLogic.Instance.Objects.Add(unit);
            else
            {
                unit.DoUpdateView = true; // update view if unit already present on map (otherwise its automatically done)
                unit.DoUpdateInfo = true;
            }

            // add spells
            unit.SpellBook.Clear();
            for (int i = 0; i < 32; i++)
            {
                uint sp = 1u << i;
                if ((SpellBook & sp) != 0)
                {
                    Spell cspell = new Spell(i, unit);
                    unit.SpellBook.Add(cspell);
                }
            }

            unit.Update(); // set isalive/isdying
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
            [ProtoMember(5)]
            public DeathAction Death;
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
                if (aus.Attack.TargetUnit != null)
                    aus.TargetUnitTag = aus.Attack.TargetUnit.Tag;
                else aus.TargetUnitTag = -1;
            }
            else aus.Attack = null;
            if (state.GetType() == typeof(DeathAction))
                aus.Death = (DeathAction)state;
            else aus.Death = null;
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
                        States[i].Attack.TargetUnit = targetUnit;
                        States[i].Attack.Spell = unit.GetSpell((Spell.Spells)States[i].Attack.SpellID);
                        if (States[i].Attack.Spell == null && States[i].Attack.TargetUnit == null)
                            return false; // bad packet
                        unit.Actions.Insert(pPos, States[i].Attack);
                    }

                    if (States[i].Death != null)
                    {
                        States[i].Death.Unit = unit;
                        unit.Actions.Insert(pPos, States[i].Death);
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
                return true;
            }

            unit.AllowIdle = true;
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
                Debug.LogFormat("Attempted to damage nonexistent unit {0}", Tag);
                return true;
            }

            // visible = whether to display flying hp
            unit.Stats.TrySetHealth(unit.Stats.Health - Damage);
            unit.DoUpdateInfo = true;
            unit.DoUpdateView = true;

            if (Visible)
            {
                // todo: display flying hp
                if (unit.GetVisibityInCamera())
                    MapView.Instance.SpawnDamageNumbers(unit, Damage, false);
            }

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.UnitPack)]
    public struct UnitPack : IClientCommand
    {
        [ProtoMember(1)]
        public int Tag;
        [ProtoMember(2)]
        public NetItem[] Body;
        [ProtoMember(3)]
        public NetItem[] Pack;
        [ProtoMember(4)]
        public long Money;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            if (unit == null)
            {
                Debug.LogFormat("Attempted to set pack for nonexistent unit {0}", Tag);
                return true;
            }

            unit.ItemsBody.Clear();
            if (Body != null)
            {
                for (int i = 0; i < Body.Length; i++)
                    unit.ItemsBody.PutItem(unit.ItemsBody.Count, new Item(Body[i]));
            }
            unit.UpdateItems();

            unit.ItemsPack.Clear();
            if (Pack != null)
            {
                for (int i = 0; i < Pack.Length; i++)
                    unit.ItemsPack.PutItem(unit.ItemsPack.Count, new Item(Pack[i]));
            }

            unit.ItemsPack.Money = Money;
            unit.DoUpdateInfo = true;

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.UnitStats)]
    public struct UnitStats : IClientCommand
    {
        [ProtoMember(1)]
        public int Tag;
        [ProtoMember(2)]
        public global::UnitStats Stats;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            if (unit == null)
            {
                Debug.LogFormat("Attempted to set stats for nonexistent unit {0}", Tag);
                return true;
            }

            unit.Stats = Stats;
            unit.DoUpdateView = true;

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.UnitStatsShort)]
    public struct UnitStatsShort : IClientCommand
    {
        [ProtoMember(1)]
        public int Tag;
        [ProtoMember(2)]
        public int Health;
        [ProtoMember(3)]
        public int Mana;
        [ProtoMember(4)]
        public int HealthMax;
        [ProtoMember(5)]
        public int ManaMax;
        // todo: effect flags (like invisibility)

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            if (unit == null)
            {
                Debug.LogFormat("Attempted to set stats for nonexistent unit {0}", Tag);
                return true;
            }

            unit.Stats.Health = Health;
            unit.Stats.HealthMax = HealthMax;
            unit.Stats.Mana = Mana;
            unit.Stats.ManaMax = ManaMax;
            unit.DoUpdateView = true;
            unit.DoUpdateInfo = true;

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.UnitItemPickup)]
    public struct UnitItemPickup : IClientCommand
    {
        [ProtoMember(1)]
        public int Tag;
        [ProtoMember(2)]
        public int ItemID;
        [ProtoMember(3)]
        public long ItemCount;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            if (unit == null)
            {
                Debug.LogFormat("Attempted to set pick up with nonexistent unit {0}", Tag);
                return true;
            }

            // print message if this unit is console
            if (unit.Player != MapLogic.Instance.ConsolePlayer)
                return true;

            string msg;
            if (ItemID >= 0)
            {
                // itemcount is resultant count
                ItemClass cls = ItemClassLoader.GetItemClassById((ushort)ItemID);
                msg = string.Format("{0} {1}", Locale.Main[85], (cls != null ? cls.VisualName : "(null)")); // you picked up: ...
                if (ItemCount > 1) // (now got NNN)
                    msg += string.Format(" ({0} {1} {2})", Locale.Main[86], ItemCount, Locale.Main[87]);
            }
            else
            {
                msg = string.Format("{0} {1} {2}", Locale.Main[88], ItemCount, Locale.Main[89]); // you picked up NNN gold
            }

            MapViewChat.Instance.AddChatMessage(Player.AllColorsPickup, msg);
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.SackAt)]
    public struct SackAt : IClientCommand
    {
        [ProtoMember(1)]
        public int X;
        [ProtoMember(2)]
        public int Y;
        [ProtoMember(3)]
        public long Price;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            ItemPack pack = new ItemPack();
            pack.Money = Price;
            MapLogic.Instance.PutSackAt(X, Y, pack, true);
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.NoSackAt)]
    public struct NoSackAt : IClientCommand
    {
        [ProtoMember(1)]
        public int X;
        [ProtoMember(2)]
        public int Y;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            MapLogic.Instance.RemoveSackAt(X, Y);
            return true;
        }
    }

    /*    public static void SpawnProjectileHoming(int id, IPlayerPawn source, float x, float y, float z, MapUnit target, float speed, MapProjectileCallback cb = null)
    {
        MapProjectile proj = new MapProjectile(id, source, new MapProjectileLogicHoming(target, speed), cb);
        proj.SetPosition(x, y, z);
        MapLogic.Instance.Objects.Add(proj);
        // todo notify clients
    }

    public static void SpawnProjectileDirectional(int id, IPlayerPawn source, float x, float y, float z, float tgx, float tgy, float tgz, float speed, MapProjectileCallback cb = null)
    {
        MapProjectile proj = new MapProjectile(id, source, new MapProjectileLogicDirectional(tgx, tgy, tgz, speed), cb);
        proj.SetPosition(x, y, z);
        MapLogic.Instance.Objects.Add(proj);
        // todo notify clients
    }

    public static void SpawnProjectileSimple(int id, IPlayerPawn source, float x, float y, float z)
    {
        MapProjectile proj = new MapProjectile(id, source, null, null); // this is usually SFX like stuff. projectile plays animation based on typeid and stops.
        proj.SetPosition(x, y, z);
        MapLogic.Instance.Objects.Add(proj);
        // todo notify clients
    }*/
    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.AddProjectileHoming)]
    public struct AddProjectileHoming : IClientCommand
    {
        [ProtoMember(1)]
        public float X;
        [ProtoMember(2)]
        public float Y;
        [ProtoMember(3)]
        public float Z;
        [ProtoMember(4)]
        public MapObjectType SourceType;
        [ProtoMember(5)]
        public int SourceTag;
        [ProtoMember(6)]
        public int TypeID;
        [ProtoMember(7)]
        public int TargetTag;
        [ProtoMember(8)]
        public float Speed;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            IPlayerPawn source = null;
            if (SourceTag > 0)
            {
                switch (SourceType)
                {
                    case MapObjectType.Human:
                    case MapObjectType.Monster:
                        source = MapLogic.Instance.GetUnitByTag(SourceTag);
                        break;

                    case MapObjectType.Structure:
                        source = MapLogic.Instance.GetStructureByTag(SourceTag);
                        break;
                }
            }

            MapUnit target = MapLogic.Instance.GetUnitByTag(TargetTag);
            if (target == null)
                return false;

            Server.SpawnProjectileHoming(TypeID, source, X, Y, Z, target, Speed, (MapProjectile fproj) =>
            {
                fproj.Dispose();
            });

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.AddProjectileDirectional)]
    public struct AddProjectileDirectional : IClientCommand
    {
        [ProtoMember(1)]
        public float X;
        [ProtoMember(2)]
        public float Y;
        [ProtoMember(3)]
        public float Z;
        [ProtoMember(4)]
        public MapObjectType SourceType;
        [ProtoMember(5)]
        public int SourceTag;
        [ProtoMember(6)]
        public int TypeID;
        [ProtoMember(7)]
        public float TargetX;
        [ProtoMember(8)]
        public float TargetY;
        [ProtoMember(9)]
        public float TargetZ;
        [ProtoMember(10)]
        public float Speed;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            IPlayerPawn source = null;
            if (SourceTag > 0)
            {
                switch (SourceType)
                {
                    case MapObjectType.Human:
                    case MapObjectType.Monster:
                        source = MapLogic.Instance.GetUnitByTag(SourceTag);
                        break;

                    case MapObjectType.Structure:
                        source = MapLogic.Instance.GetStructureByTag(SourceTag);
                        break;
                }
            }

            Server.SpawnProjectileDirectional(TypeID, source, X, Y, Z, TargetX, TargetY, TargetZ, Speed, (MapProjectile fproj) =>
            {
                fproj.Dispose();
            });

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.AddProjectileSimple)]
    public struct AddProjectileSimple : IClientCommand
    {
        [ProtoMember(1)]
        public float X;
        [ProtoMember(2)]
        public float Y;
        [ProtoMember(3)]
        public float Z;
        [ProtoMember(4)]
        public MapObjectType SourceType;
        [ProtoMember(5)]
        public int SourceTag;
        [ProtoMember(6)]
        public int TypeID;
        [ProtoMember(7)]
        public float AnimSpeed;
        [ProtoMember(8)]
        public float Scale;
        [ProtoMember(9)]
        public int Start;
        [ProtoMember(10)]
        public int End;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            IPlayerPawn source = null;
            if (SourceTag > 0)
            {
                switch (SourceType)
                {
                    case MapObjectType.Human:
                    case MapObjectType.Monster:
                        source = MapLogic.Instance.GetUnitByTag(SourceTag);
                        break;

                    case MapObjectType.Structure:
                        source = MapLogic.Instance.GetStructureByTag(SourceTag);
                        break;
                }
            }

            Server.SpawnProjectileSimple(TypeID, source, X, Y, Z, AnimSpeed, Scale, Start, End);
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.AddProjectileLightning)]
    public struct AddProjectileLightning: IClientCommand
    {
        [ProtoMember(1)]
        public float X;
        [ProtoMember(2)]
        public float Y;
        [ProtoMember(3)]
        public float Z;
        [ProtoMember(4)]
        public MapObjectType SourceType;
        [ProtoMember(5)]
        public int SourceTag;
        [ProtoMember(6)]
        public int TargetTag;
        [ProtoMember(7)]
        public int Color;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            IPlayerPawn source = null;
            if (SourceTag > 0)
            {
                switch (SourceType)
                {
                    case MapObjectType.Human:
                    case MapObjectType.Monster:
                        source = MapLogic.Instance.GetUnitByTag(SourceTag);
                        break;

                    case MapObjectType.Structure:
                        source = MapLogic.Instance.GetStructureByTag(SourceTag);
                        break;
                }
            }

            MapUnit target = MapLogic.Instance.GetUnitByTag(TargetTag);
            if (target == null)
                return false;

            Server.SpawnProjectileLightning(source, X, Y, Z, target, Color);

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.AddProjectileEOT)]
    public struct AddProjectileEOT : IClientCommand
    {
        [ProtoMember(1)]
        public float X;
        [ProtoMember(2)]
        public float Y;
        [ProtoMember(3)]
        public float Z;
        [ProtoMember(4)]
        public MapObjectType SourceType;
        [ProtoMember(5)]
        public int SourceTag;
        [ProtoMember(6)]
        public int TypeID;
        [ProtoMember(7)]
        public int Duration;
        [ProtoMember(8)]
        public int StartFrames;
        [ProtoMember(9)]
        public int EndFrames;
        [ProtoMember(10)]
        public int ZOffset;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            IPlayerPawn source = null;
            if (SourceTag > 0)
            {
                switch (SourceType)
                {
                    case MapObjectType.Human:
                    case MapObjectType.Monster:
                        source = MapLogic.Instance.GetUnitByTag(SourceTag);
                        break;

                    case MapObjectType.Structure:
                        source = MapLogic.Instance.GetStructureByTag(SourceTag);
                        break;
                }
            }

            Server.SpawnProjectileEOT(TypeID, source, X, Y, Z, Duration, Duration, StartFrames, EndFrames, ZOffset);
            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.StaticObjectDead)]
    public struct StaticObjectDead : IClientCommand
    {
        [ProtoMember(1)]
        public int X;
        [ProtoMember(2)]
        public int Y;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            if (X < 0 || X >= MapLogic.Instance.Width ||
                Y < 0 || Y >= MapLogic.Instance.Height)
                    return false;

            MapNode node = MapLogic.Instance.Nodes[X, Y];
            for (int i = 0; i < node.Objects.Count; i++)
            {
                // check object type.
                if (node.Objects[i].GetObjectType() != MapObjectType.Obstacle)
                    continue;

                MapObstacle ob = (MapObstacle)node.Objects[i];
                ob.SetDead(false);
            }

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.UnitSpells)]
    public struct UnitSpells : IClientCommand
    {
        [ProtoMember(1)]
        public int Tag;
        [ProtoMember(2)]
        public uint SpellBook;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            if (unit == null)
            {
                Debug.LogFormat("Attempted to set spells for nonexistent unit {0}", Tag);
                return true;
            }

            // add spells
            unit.SpellBook.Clear();
            for (int i = 0; i < 32; i++)
            {
                uint sp = 1u << i;
                if ((SpellBook & sp) != 0)
                {
                    Spell cspell = new Spell(i, unit);
                    unit.SpellBook.Add(cspell);
                }
            }

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.UnitFlags)]
    public struct UnitFlags : IClientCommand
    {
        [ProtoMember(1)]
        public int Tag;
        [ProtoMember(2)]
        public global::UnitFlags Flags;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            if (unit == null)
            {
                Debug.LogFormat("Attempted to set flags for nonexistent unit {0}", Tag);
                return true;
            }

            unit.Flags = Flags;

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.UnitPosition)]
    public struct UnitPosition : IClientCommand
    {
        [ProtoMember(1)]
        public int Tag;
        [ProtoMember(2)]
        public int X;
        [ProtoMember(3)]
        public int Y;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            if (unit == null)
            {
                Debug.LogFormat("Attempted to set position for nonexistent unit {0}", Tag);
                return true;
            }

            unit.SetPosition(X, Y, true);

            return true;
        }
    }

    [ProtoContract]
    [NetworkPacketId(ClientIdentifiers.HumanLevelUp)]
    public struct HumanLevelUp : IClientCommand
    {
        [ProtoMember(1)]
        public int Tag;
        [ProtoMember(2)]
        public MapHuman.ExperienceSkill Skill;
        [ProtoMember(3)]
        public int ExpAfter;

        public bool Process()
        {
            if (!MapLogic.Instance.IsLoaded)
                return false;

            MapUnit unit = MapLogic.Instance.GetUnitByTag(Tag);
            if (unit == null || !(unit is MapHuman))
            {
                Debug.LogFormat("Attempted to set experience for nonexistent human {0}", Tag);
                return true;
            }

            MapHuman human = (MapHuman)unit;
            human.SetSkillExperience(Skill, ExpAfter);

            return true;
        }
    }
}
