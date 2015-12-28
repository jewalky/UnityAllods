using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public void connect(string host)
    {
        NetworkManager.Instance.InitClient(host, 8000);
    }

    public void host()
    {
        NetworkManager.Instance.InitServer(8000);
    }

    public void disconnect()
    {
        NetworkManager.Instance.Disconnect();
    }

    public void grid()
    {
        MapView.Instance.GridEnabled = !MapView.Instance.GridEnabled;
    }

    public void rcon(params string[] args)
    {
        string toSend = GameConsole.JoinArguments(args);
        //
    }
}
