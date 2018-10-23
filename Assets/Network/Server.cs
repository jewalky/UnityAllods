using System.Collections.Generic;
using System.Linq;

public interface IServerCommand
{
    bool Process(ServerClient client);
}

public class Server
{
    public static void ClientConnected(ServerClient cl)
    {
        GameConsole.Instance.WriteLine("Client {0}:{1} has connected.", cl.IPAddress, cl.IPPort);
    }

    public static void ClientDisconnected(ServerClient cl)
    {
        GameConsole.Instance.WriteLine("Client {0}:{1} has disconnected.", cl.IPAddress, cl.IPPort);
        // right now, we don't have any special handling for offline (yet present) players. that will only be implemented when we'll have a master server (hat).
        // so when a player disconnects, we immediately kick him out without giving a chance to reconnect flawlessly.
        if (MapLogic.Instance.IsLoaded)
        {
            Player player = MapLogic.Instance.GetNetPlayer(cl);
            if (player != null)
                MapLogic.Instance.DelNetPlayer(player, false, false);
        }
    }

    public static void DisconnectPlayer(Player player)
    {
        if (player.NetClient == null)
            return;
        player.NetClient.Disconnect();
    }

    public static void KickPlayer(Player player)
    {
        if (player.NetClient == null)
            return;
        DisconnectPlayer(player);
        MapLogic.Instance.DelNetPlayer(player, false, true);
    }

    public static void NotifyPlayerJoined(Player player)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;
            if (client == player.NetClient) // don't send own info, this is done separately.
                continue;

            ClientCommands.AddPlayer plCmd;
            plCmd.ID = player.ID;
            plCmd.Color = player.Color;
            plCmd.Name = player.Name;
            plCmd.Money = player.Money;
            plCmd.Diplomacy = player.Diplomacy;
            plCmd.Silent = false;
            plCmd.ConsolePlayer = false;
            client.SendCommand(plCmd);
        }
    }

    public static void NotifyPlayerLeft(Player player, bool kicked)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;
            if (client == player.NetClient) // don't send own info, this is done separately.
                continue;

            ClientCommands.DelPlayer plCmd;
            plCmd.ID = player.ID;
            plCmd.Kick = kicked;
            plCmd.Silent = false;
            client.SendCommand(plCmd);
        }
    }

    public static void NotifyChatMessage(Player player, string message)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            ClientCommands.ChatMessage chatCmd;
            chatCmd.PlayerID = (player != null) ? player.ID : -1;
            chatCmd.Text = message;
            client.SendCommand(chatCmd);
        }
    }

    // clients will see anything sent with this function as a "<server>: blablabla" in their chat.
    public static void SendChatMessage(string text)
    {
        // local chat presentation.
        int color = Player.AllColorsSystem;
        string actualText = "<server>: " + text;
        MapViewChat.Instance.AddChatMessage(color, actualText);

        NotifyChatMessage(null, text);
    }

    public static void NotifySpeedChanged(int newSpeed)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            ClientCommands.SpeedChanged speedCmd;
            speedCmd.NewSpeed = newSpeed;
            client.SendCommand(speedCmd);
        }
    }

    public static void ObjectBecameVisible(Player player, MapObject mobj)
    {
        if (player.NetClient == null)
            return;

        //Debug.LogFormat("visible = {0}->{1}", player.Name, mobj.GetObjectType().ToString());
        if (mobj.GetObjectType() == MapObjectType.Monster ||
            mobj.GetObjectType() == MapObjectType.Human)
        {
            MapUnit unit = (MapUnit)mobj;
            ClientCommands.AddUnit unitCmd;
            unitCmd.Tag = unit.Tag;
            unitCmd.X = unit.X;
            unitCmd.Y = unit.Y;
            unitCmd.Angle = unit.Angle;
            unitCmd.Player = unit.Player.ID;
            unitCmd.ServerID = unit.ServerID;
            unitCmd.CurrentStats = unit.Stats;
            unitCmd.IsAvatar = (unit == unit.Player.Avatar);
            unitCmd.VState = unit.VState;
            unitCmd.IdleFrame = unit.IdleFrame;
            unitCmd.IdleTime = unit.IdleTime;
            unitCmd.MoveFrame = unit.MoveFrame;
            unitCmd.MoveTime = unit.MoveTime;
            unitCmd.FracX = unit.FracX;
            unitCmd.FracY = unit.FracY;
            unitCmd.AttackFrame = unit.AttackFrame;
            unitCmd.AttackTime = unit.AttackTime;
            unitCmd.DeathFrame = unit.DeathFrame;
            unitCmd.DeathTime = unit.DeathTime;
            unitCmd.IsAlive = unit.IsAlive;
            unitCmd.IsDying = unit.IsDying;
            unitCmd.IsHuman = (unit.GetObjectType() == MapObjectType.Human);

            if (unitCmd.IsHuman)
            {
                MapHuman human = (MapHuman)unit;
                unitCmd.IsHero = human.IsHero;
            }
            else
            {
                unitCmd.IsHero = false;
            }

            unitCmd.ItemsBody = new List<NetItem>();
            for (int i = 0; i < unit.ItemsBody.Count; i++)
                unitCmd.ItemsBody.Add(new NetItem(unit.ItemsBody[i]));

            unitCmd.ItemsPack = new List<NetItem>();
            if (unit.Player == player)
                for (int i = 0; i < unit.ItemsPack.Count; i++)
                    unitCmd.ItemsPack.Add(new NetItem(unit.ItemsPack[i]));

            uint sps = 0;
            foreach (Spell cspell in unit.SpellBook)
                sps |= 1u << (int)cspell.SpellID;
            unitCmd.SpellBook = sps;

            unitCmd.Flags = unit.Flags;

            player.NetClient.SendCommand(unitCmd);
            // also notify of current unit state
            NotifyAddUnitActionsSingle(player.NetClient, unit, unit.Actions.Skip(1).ToArray());
            //Debug.LogFormat("sending player {0} unit {1}", player.Name, unitCmd.Tag);            
        }
        else if (mobj.GetObjectType() == MapObjectType.Sack)
        {
            MapSack sack = (MapSack)mobj;
            NotifySack(sack.X, sack.Y, sack.Pack.Price);
        }
        else if (mobj.GetObjectType() == MapObjectType.Obstacle)
        {
            MapObstacle obstacle = (MapObstacle)mobj;
            if (obstacle.IsDead)
                Server.NotifyStaticObjectDead(obstacle.X, obstacle.Y);
        }
    }

    public static void ObjectBecameInvisible(Player player, MapObject mobj)
    {
        //Debug.LogFormat("invisible = {0}->{1}", player.Name, mobj.GetObjectType().ToString());
    }

    public static void NotifyDelObject(MapObject mobj)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            //todo object deleting
            if (mobj.GetObjectType() == MapObjectType.Monster ||
                mobj.GetObjectType() == MapObjectType.Human)
            {
                MapUnit unit = (MapUnit)mobj;
                ClientCommands.DelUnit dunitCmd;
                dunitCmd.Tag = unit.Tag;
                client.SendCommand(dunitCmd);
            }
        }
    }

    public static void NotifyAddUnitActionsSingle(ServerClient client, MapUnit unit, IUnitAction[] states)
    {
        ClientCommands.AddUnitActions statesCmd;
        statesCmd.Tag = unit.Tag;
        statesCmd.Position = unit.Actions.Count - states.Length;
        // for some reason we can't send array of these objects even if properly configured.
        // client will always receive NULL.
        statesCmd.State1 = null;
        statesCmd.State2 = null;
        if (states.Length > 0)
            statesCmd.State1 = ClientCommands.AddUnitActions.GetAddUnitAction(states[0]);
        if (states.Length > 1)
            statesCmd.State2 = ClientCommands.AddUnitActions.GetAddUnitAction(states[1]);
        client.SendCommand(statesCmd);
    }

    public static void NotifyAddUnitActions(MapUnit unit, IUnitAction[] states)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            if (unit.IsVisibleForNetPlayer(p))
            {
                NotifyAddUnitActionsSingle(client, unit, states);
            }
        }
    }

    public static void NotifyIdleUnit(MapUnit unit, int x, int y, int angle)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            if (unit.IsVisibleForNetPlayer(p))
            {
                ClientCommands.IdleUnit idleCmd;
                idleCmd.Tag = unit.Tag;
                client.SendCommand(idleCmd);
            }
        }
    }

    public static void NotifyDamageUnit(MapUnit unit, int damage, bool visible)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            if (unit.IsVisibleForNetPlayer(p))
            {
                ClientCommands.DamageUnit dmgCmd;
                dmgCmd.Tag = unit.Tag;
                dmgCmd.Damage = damage;
                dmgCmd.Visible = visible;
                client.SendCommand(dmgCmd);
            }
        }
    }

    public static void NotifyRespawn(MapUnit unit)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            if (unit.IsVisibleForNetPlayer(p))
            {
                ObjectBecameVisible(p, unit);
            }
        }
    }

    public static void NotifyUnitPack(MapUnit unit)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            if (unit.IsVisibleForNetPlayer(p))
            {
                ClientCommands.UnitPack packCmd;
                packCmd.Tag = unit.Tag;
                packCmd.Body = new List<NetItem>();
                for (int i = 0; i < unit.ItemsBody.Count; i++)
                    packCmd.Body.Add(new NetItem(unit.ItemsBody[i]));
                packCmd.Pack = new List<NetItem>();
                packCmd.Money = 0;
                if (unit.Player == p)
                {
                    for (int i = 0; i < unit.ItemsPack.Count; i++)
                        packCmd.Pack.Add(new NetItem(unit.ItemsPack[i]));
                    packCmd.Money = unit.ItemsPack.Money;
                }
                client.SendCommand(packCmd);
            }
        }
    }

    public static void NotifyUnitStats(MapUnit unit)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            if (unit.IsVisibleForNetPlayer(p))
            {
                ClientCommands.UnitStats statsCmd;
                statsCmd.Tag = unit.Tag;
                statsCmd.Stats = unit.Stats;
                client.SendCommand(statsCmd);
            }
        }
    }

    public static void NotifyItemPickup(MapUnit unit, int itemid, long count)
    {
        // if item is >=0, print language string for this item
        // if item is <0, print "gold"
        // send message to unit's owner
        ClientCommands.UnitItemPickup pkpCmd;
        pkpCmd.Tag = unit.Tag;
        pkpCmd.ItemID = itemid;
        pkpCmd.ItemCount = count;

        if (NetworkManager.IsServer)
        {
            ServerClient client = unit.Player.NetClient;
            if (client == null)
                return;

            client.SendCommand(pkpCmd);
        }
        else if (!NetworkManager.IsClient)
        {
            pkpCmd.Process(); // local play. process as-is.
        }
    }

    public static void NotifySack(int x, int y, long price)
    {
        MapSack sack = MapLogic.Instance.GetSackAt(x, y);
        if (sack == null)
        {
            NotifyNoSack(x, y);
            return;
        }

        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            if (sack.IsVisibleForNetPlayer(p))
            {
                ClientCommands.SackAt sckCmd;
                sckCmd.X = x;
                sckCmd.Y = y;
                sckCmd.Price = price;
                client.SendCommand(sckCmd);
            }
        }
    }

    public static void NotifyNoSack(int x, int y)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            ClientCommands.NoSackAt nosckCmd;
            nosckCmd.X = x;
            nosckCmd.Y = y;
            client.SendCommand(nosckCmd);
        }
    }

    public static void SpawnProjectileHoming(AllodsProjectile id, IPlayerPawn source, float x, float y, float z, MapUnit target, float speed, MapProjectileCallback cb = null)
    {
        SpawnProjectileHoming((int)id, source, x, y, z, target, speed, cb);
    }

    public static void SpawnProjectileDirectional(AllodsProjectile id, IPlayerPawn source, float x, float y, float z, float tgx, float tgy, float tgz, float speed, MapProjectileCallback cb = null)
    {
        SpawnProjectileDirectional((int)id, source, x, y, z, tgx, tgy, tgz, speed, cb);
    }

    public static void SpawnProjectileSimple(AllodsProjectile id, IPlayerPawn source, float x, float y, float z, float animspeed = 0.5f, float scale = 1f)
    {
        SpawnProjectileSimple((int)id, source, x, y, z, animspeed, scale);
    }

    public static void SpawnProjectileEOT(AllodsProjectile id, IPlayerPawn source, float x, float y, float z, int duration, int frequency, int startframes = 0, int endframes = 0, int zoffs = -128, MapProjectileCallback cb = null)
    {
        SpawnProjectileEOT((int)id, source, x, y, z, duration, frequency, startframes, endframes, zoffs, cb);
    }

    public static void SpawnProjectileHoming(int id, IPlayerPawn source, float x, float y, float z, MapUnit target, float speed, MapProjectileCallback cb = null)
    {
        MapProjectile proj = new MapProjectile(id, source, new MapProjectileLogicHoming(target, speed), cb);
        proj.SetPosition(x, y, z);
        MapLogic.Instance.Objects.Add(proj);

        if (NetworkManager.IsServer)
        {
            foreach (ServerClient client in ServerManager.Clients)
            {
                if (client.State != ClientState.Playing)
                    continue;

                Player p = MapLogic.Instance.GetNetPlayer(client);
                MapObject sourceObj = (MapObject)source;
                if (sourceObj != null)
                {
                    if (!sourceObj.IsVisibleForNetPlayer(p))
                        ObjectBecameVisible(p, sourceObj); // force keyframe update for source unit
                }

                ClientCommands.AddProjectileHoming app;
                app.X = x;
                app.Y = y;
                app.Z = z;
                app.SourceType = (sourceObj != null) ? sourceObj.GetObjectType() : MapObjectType.Object;

                if (app.SourceType == MapObjectType.Human ||
                    app.SourceType == MapObjectType.Monster) app.SourceTag = ((MapUnit)sourceObj).Tag;
                else if (app.SourceType == MapObjectType.Structure) app.SourceTag = ((MapStructure)sourceObj).Tag;
                else app.SourceTag = -1;

                app.Speed = speed;
                app.TargetTag = target.Tag;
                app.TypeID = id;

                client.SendCommand(app);
            }
        }
    }

    public static void SpawnProjectileDirectional(int id, IPlayerPawn source, float x, float y, float z, float tgx, float tgy, float tgz, float speed, MapProjectileCallback cb = null)
    {
        MapProjectile proj = new MapProjectile(id, source, new MapProjectileLogicDirectional(tgx, tgy, tgz, speed), cb);
        proj.SetPosition(x, y, z);
        MapLogic.Instance.Objects.Add(proj);

        if (NetworkManager.IsServer)
        {
            foreach (ServerClient client in ServerManager.Clients)
            {
                if (client.State != ClientState.Playing)
                    continue;

                Player p = MapLogic.Instance.GetNetPlayer(client);
                MapObject sourceObj = (MapObject)source;
                if (sourceObj != null)
                {
                    if (!sourceObj.IsVisibleForNetPlayer(p))
                        ObjectBecameVisible(p, sourceObj); // force keyframe update for source unit
                }

                ClientCommands.AddProjectileDirectional app;
                app.X = x;
                app.Y = y;
                app.Z = z;
                app.TargetX = tgx;
                app.TargetY = tgy;
                app.TargetZ = tgz;
                app.SourceType = (sourceObj != null) ? sourceObj.GetObjectType() : MapObjectType.Object;

                if (app.SourceType == MapObjectType.Human ||
                    app.SourceType == MapObjectType.Monster) app.SourceTag = ((MapUnit)sourceObj).Tag;
                else if (app.SourceType == MapObjectType.Structure) app.SourceTag = ((MapStructure)sourceObj).Tag;
                else app.SourceTag = -1;

                app.Speed = speed;
                app.TypeID = id;

                client.SendCommand(app);
            }
        }
    }

    public static void SpawnProjectileSimple(int id, IPlayerPawn source, float x, float y, float z, float animspeed = 0.5f, float scale = 1f)
    {
        MapProjectile proj = new MapProjectile(id, source, new MapProjectileLogicSimple(animspeed, scale), null); // this is usually SFX like stuff. projectile plays animation based on typeid and stops.
        proj.SetPosition(x, y, z);
        MapLogic.Instance.Objects.Add(proj);

        if (NetworkManager.IsServer)
        {
            foreach (ServerClient client in ServerManager.Clients)
            {
                if (client.State != ClientState.Playing)
                    continue;

                Player p = MapLogic.Instance.GetNetPlayer(client);
                MapObject sourceObj = (MapObject)source;
                if (sourceObj != null)
                {
                    if (!sourceObj.IsVisibleForNetPlayer(p))
                        ObjectBecameVisible(p, sourceObj); // force keyframe update for source unit
                }

                ClientCommands.AddProjectileSimple app;
                app.X = x;
                app.Y = y;
                app.Z = z;
                app.SourceType = (sourceObj != null) ? sourceObj.GetObjectType() : MapObjectType.Object;

                if (app.SourceType == MapObjectType.Human ||
                    app.SourceType == MapObjectType.Monster) app.SourceTag = ((MapUnit)sourceObj).Tag;
                else if (app.SourceType == MapObjectType.Structure) app.SourceTag = ((MapStructure)sourceObj).Tag;
                else app.SourceTag = -1;

                app.TypeID = id;
                app.AnimSpeed = animspeed;
                app.Scale = scale;

                client.SendCommand(app);
            }
        }
    }

    public static void SpawnProjectileLightning(IPlayerPawn source, float x, float y, float z, MapUnit target, int color, MapProjectileCallback cb = null)
    {
        MapProjectile proj = new MapProjectile(AllodsProjectile.Lightning, source, new MapProjectileLogicLightning(target, color), cb);
        proj.SetPosition(x, y, z);
        MapLogic.Instance.Objects.Add(proj);

        if (NetworkManager.IsServer)
        {
            foreach (ServerClient client in ServerManager.Clients)
            {
                if (client.State != ClientState.Playing)
                    continue;

                Player p = MapLogic.Instance.GetNetPlayer(client);
                MapObject sourceObj = (MapObject)source;
                if (sourceObj != null)
                {
                    if (!sourceObj.IsVisibleForNetPlayer(p))
                        ObjectBecameVisible(p, sourceObj); // force keyframe update for source unit
                }

                ClientCommands.AddProjectileLightning app;
                app.X = x;
                app.Y = y;
                app.Z = z;
                app.SourceType = (sourceObj != null) ? sourceObj.GetObjectType() : MapObjectType.Object;

                if (app.SourceType == MapObjectType.Human ||
                    app.SourceType == MapObjectType.Monster) app.SourceTag = ((MapUnit)sourceObj).Tag;
                else if (app.SourceType == MapObjectType.Structure) app.SourceTag = ((MapStructure)sourceObj).Tag;
                else app.SourceTag = -1;

                app.TargetTag = target.Tag;
                app.Color = color;

                client.SendCommand(app);
            }
        }
    }

    public static void SpawnProjectileEOT(int id, IPlayerPawn source, float x, float y, float z, int duration, int frequency, int startframes = 0, int endframes = 0, int zoffs = -128, MapProjectileCallback cb = null)
    {
        MapProjectile proj = new MapProjectile(id, source, new MapProjectileLogicEOT(duration, frequency, startframes, endframes), cb);
        proj.ZOffset = zoffs;
        proj.SetPosition(x, y, z);
        MapLogic.Instance.Objects.Add(proj);

        if (NetworkManager.IsServer)
        {
            foreach (ServerClient client in ServerManager.Clients)
            {
                if (client.State != ClientState.Playing)
                    continue;

                Player p = MapLogic.Instance.GetNetPlayer(client);
                MapObject sourceObj = (MapObject)source;
                if (sourceObj != null)
                {
                    if (!sourceObj.IsVisibleForNetPlayer(p))
                        ObjectBecameVisible(p, sourceObj); // force keyframe update for source unit
                }

                ClientCommands.AddProjectileEOT app;
                app.X = x;
                app.Y = y;
                app.Z = z;
                app.SourceType = (sourceObj != null) ? sourceObj.GetObjectType() : MapObjectType.Object;

                if (app.SourceType == MapObjectType.Human ||
                    app.SourceType == MapObjectType.Monster) app.SourceTag = ((MapUnit)sourceObj).Tag;
                else if (app.SourceType == MapObjectType.Structure) app.SourceTag = ((MapStructure)sourceObj).Tag;
                else app.SourceTag = -1;

                app.TypeID = id;
                app.Duration = duration;
                app.StartFrames = startframes;
                app.EndFrames = endframes;
                app.ZOffset = zoffs;

                client.SendCommand(app);
            }
        }
    }

    public static void NotifyStaticObjectDead(int x, int y)
    {
        if (!NetworkManager.IsServer)
            return;

        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            ClientCommands.StaticObjectDead sod;
            sod.X = x;
            sod.Y = y;

            client.SendCommand(sod);
        }
    }

    public static void NotifyUnitSpells(MapUnit unit)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            if (unit.IsVisibleForNetPlayer(p))
            {
                ClientCommands.UnitSpells spellsCmd;
                spellsCmd.Tag = unit.Tag;
                uint sps = 0;
                foreach (Spell cspell in unit.SpellBook)
                    sps |= 1u << (int)cspell.SpellID;
                spellsCmd.SpellBook = sps;
                client.SendCommand(spellsCmd);
            }
        }
    }

    public static void NotifyUnitStatsShort(MapUnit unit)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            if (unit.IsVisibleForNetPlayer(p))
            {
                ClientCommands.UnitStatsShort statsCmd;
                statsCmd.Tag = unit.Tag;
                statsCmd.Health = unit.Stats.Health;
                statsCmd.HealthMax = unit.Stats.HealthMax;
                statsCmd.Mana = unit.Stats.Mana;
                statsCmd.ManaMax = unit.Stats.ManaMax;
                client.SendCommand(statsCmd);
            }
        }
    }

    public static void NotifyUnitFlags(MapUnit unit)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            if (unit.IsVisibleForNetPlayer(p))
            {
                ClientCommands.UnitFlags flagsCmd;
                flagsCmd.Tag = unit.Tag;
                flagsCmd.Flags = unit.Flags;
                client.SendCommand(flagsCmd);
            }
        }
    }

    public static void NotifyUnitTeleport(MapUnit unit)
    {
        foreach (ServerClient client in ServerManager.Clients)
        {
            if (client.State != ClientState.Playing)
                continue;

            Player p = MapLogic.Instance.GetNetPlayer(client);
            if (unit.IsVisibleForNetPlayer(p))
            {
                ClientCommands.UnitPosition posCmd;
                posCmd.Tag = unit.Tag;
                posCmd.X = unit.X;
                posCmd.Y = unit.Y;
                client.SendCommand(posCmd);
            }
        }
    }
}