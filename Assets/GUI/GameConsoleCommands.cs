using UnityEngine;

public class GameConsoleCommands
{
    public void quit()
    {
        Application.Quit();
    }

    public void exit()
    {
        quit();
    }

    public void map(string filename)
    {
        if (NetworkManager.Instance.State == NetworkState.Client)
            NetworkManager.Instance.Disconnect();
        MapLogic.Instance.Unload();

        string actualFilename = null;

        // check 4 locations
        if (ResourceManager.FileExists(filename))
            actualFilename = filename;
        else if (ResourceManager.FileExists(filename + ".alm"))
            actualFilename = filename + ".alm";
        else if (ResourceManager.FileExists("maps/" + filename))
            actualFilename = "maps/" + filename;
        else if (ResourceManager.FileExists("maps/" + filename + ".alm"))
            actualFilename = "maps/" + filename + ".alm";

        if (actualFilename == null)
        {
            GameConsole.Instance.WriteLine("Error: map not found for \"{0}\"", filename);
            return;
        }

        GameConsole.Instance.WriteLine("Switching to map \"{0}\" (found as \"{1}\")", filename, actualFilename);
        MapView.Instance.InitFromFile(actualFilename);
    }

    public void screenshot()
    {
        MainCamera.Instance.TakeScreenshot();
    }

    public void connect(string host = "localhost", string port = "8000")
    {
        NetworkManager.Instance.InitClient(host, ushort.Parse(port));
    }

    public void test() {
        connect("hat.allods2.eu", "8049");
    }

    public void host(string port = "8000")
    {
        NetworkManager.Instance.InitServer(ushort.Parse(port));
        GameConsole.Instance.WriteLine("Hosting server on port \"{0}\"", port);
    }

    public void srv()
    {
        host("8049");
    }

    public void disconnect()
    {
        NetworkManager.Instance.Disconnect();
    }

    public void grid()
    {
        MapView.Instance.GridEnabled = !MapView.Instance.GridEnabled;
    }

    public void showmap()
    {
        if (NetworkManager.IsClient)
            return;
        
        // for easier pathfinding testing
        for (int y = 0; y < MapLogic.Instance.Height; y++)
        {
            for (int x = 0; x < MapLogic.Instance.Width; x++)
            {
                MapLogic.Instance.Nodes[x, y].Flags |= MapNodeFlags.Discovered;
            }
        }
    }

    public void spawn(string unitName)
    {
        if (NetworkManager.IsClient || MapLogic.Instance.ConsolePlayer == null)
            return;

        MapUnit consoleMainChar = MapLogic.Instance.ConsolePlayer.Avatar;
        if (consoleMainChar == null)
            return;

        MapUnit unit = new MapHuman(unitName);
        if (unit.Class == null)
            unit = new MapUnit(unitName);

        if (unit.Class == null)
        {
            GameConsole.Instance.WriteLine("Failed to spawn summoned unit {0}", unitName);
            return;
        }

        unit.Player = MapLogic.Instance.ConsolePlayer;
        unit.Tag = MapLogic.Instance.GetFreeUnitTag();

        if (!unit.RandomizePosition(consoleMainChar.X, consoleMainChar.Y, 2, false))
        {
            // invalid position, don't add unit
            unit.Dispose();
            GameConsole.Instance.WriteLine("No space for summoned unit {0}", unitName);
            return;
        }

        MapLogic.Instance.AddObject(unit, true);
    }

    public void showgroup(string gid = "-1")
    {
        int groupId = -1;
        int.TryParse(gid, out groupId);

        if (NetworkManager.IsClient)
            return;

        MapLogic.Instance.DebugShowUnit = -1;
        MapLogic.Instance.DebugShowGroup = groupId;
    }

    public void showunit(string uid = "-1")
    {
        int unitId = -1;
        int.TryParse(uid, out unitId);

        if (NetworkManager.IsClient)
            return;

        MapLogic.Instance.DebugShowGroup = -1;
        MapLogic.Instance.DebugShowUnit = unitId;
    }

    public void rcon(params string[] args)
    {
        //string toSend = GameConsole.JoinArguments(args);
        //
    }
}
