using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// here reside functions and packet structures for the client. on input, a function is picked, based on packet type id.
/// </summary>

public class ClientCommands
{
    [Serializable()]
    public class TestCommand
    {
        public string TestString { get; set; }
    }

    public static bool OnTestCommand(TestCommand cmd)
    {
        GameConsole.Instance.WriteLine(cmd.TestString);
        ServerCommands.TestCommandReply cmdr = new ServerCommands.TestCommandReply();
        cmdr.TestString = cmd.TestString + " - yea okay";
        Client.SendCommand(cmdr);
        return true;
    }
}