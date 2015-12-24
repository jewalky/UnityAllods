using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AllodsUI
{
    public class GameConsoleCommands
    {
        private GameConsole GameConsole;
        public GameConsoleCommands(GameConsole con)
        {
            GameConsole = con;
        }

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
                GameConsole.WriteLine("Error: map not found for \"{0}\"", filename);
                return;
            }

            GameConsole.WriteLine("Switching to map \"{0}\" (found as \"{1}\")", filename, actualFilename);
            MapView.Instance.InitFromFile(actualFilename);
        }
    }
}