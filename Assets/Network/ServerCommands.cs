using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ServerCommands
{
    [Serializable()]
    public class TestCommandReply
    {
        public string TestString;
    }

    public static void OnTestCommandReply(TestCommandReply cmd)
    {
        GameConsole.Instance.WriteLine(cmd.TestString);
    }
}