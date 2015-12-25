using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public interface IServerCommand
{
    bool Process(ServerClient client);
}

public class ServerCommands
{

}