using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class GameConsole : MonoBehaviour, IUiEventProcessor, IUiEventProcessorBackground
{
    private static GameConsole _Instance = null;
    public static GameConsole Instance
    {
        get
        {
            if (_Instance == null) _Instance = GameObject.FindObjectOfType<GameConsole>();
            return _Instance;
        }
    }

    // console contents
    bool ConsoleActive = false;
    int ConsoleHeight;
    GameObject BgObject;
    MeshRenderer BgRenderer;

    AllodsTextRenderer TextRendererA;
    GameObject TextObject;
    MeshRenderer TextRenderer;

    // editor field
    TextField EditField;

    // command handler
    GameConsoleCommands CommandHandler;

    // command history
    int CommandHistoryPosition = 0;
    List<string> CommandHistory = new List<string>();

    public void OnDestroy()
    {
        UiManager.Instance.Unsubscribe(this);
    }

    public void Start()
    {
        UiManager.Instance.Subscribe(this);
        CommandHandler = new GameConsoleCommands(this);
        CommandHistory.Add("");

        transform.localScale = new Vector3(1, 1, 1);

        ConsoleHeight = Screen.height / 3;

        BgObject = Utils.CreatePrimitive(PrimitiveType.Quad);
        BgObject.name = "ConsoleBackground";
        BgObject.transform.parent = transform;
        BgRenderer = BgObject.GetComponent<MeshRenderer>();
        BgObject.transform.localPosition = new Vector3(Screen.width / 2, ConsoleHeight / 2, 0f);
        BgObject.transform.localScale = new Vector3(Screen.width, ConsoleHeight);
        BgRenderer.material = new Material(MainCamera.MainShader);
        BgRenderer.material.color = new Color(0, 0, 0, 0.6f);
        BgRenderer.enabled = false;
        transform.position = new Vector3(0, 0, MainCamera.InterfaceZ - 0.99f);

        // prepare text. this renderer will wrap lines based on screen width.
        TextRendererA = new AllodsTextRenderer(Fonts.Font2, Font.Align.Left, Screen.width - 4, 0, true);
        TextObject = TextRendererA.GetNewGameObject(0.01f, transform, 100);
        TextObject.transform.localPosition = new Vector3(2, 2, -0.001f);
        TextRenderer = TextObject.GetComponent<MeshRenderer>();
        TextRenderer.material.color = new Color(0.8f, 0.8f, 0.8f);

        EditField = Utils.CreateObjectWithScript<TextField>();
        EditField.transform.parent = transform;
        EditField.transform.localPosition = new Vector3(2, ConsoleHeight - 13, -0.001f);
        EditField.transform.localScale = new Vector3(1, 1, 0.001f);
        EditField.Font = Fonts.Font2;
        EditField.Prefix = "> ";
        EditField.Width = Screen.width - 4;
        EditField.Height = Fonts.Font2.LineHeight;
        EditField.OnReturn = () =>
        {
            string cmd = EditField.Value;
            if (cmd.Trim().Length > 0)
            {
                WriteLine("> " + cmd);
                EditField.Value = "";
                ExecuteCommand(cmd);
                CommandHistory[CommandHistory.Count - 1] = cmd;
                CommandHistory.Add("");
                CommandHistoryPosition = CommandHistory.Count - 1;
            }
        };

        WriteLine("Welcome to UnityAllods!");
    }

    public bool ProcessEvent(Event e)
    {
        if (e.type == EventType.KeyDown &&
            e.keyCode == KeyCode.BackQuote)
        {
            ConsoleActive = !ConsoleActive;
            if (ConsoleActive)
                EditField.Value = "";
            return true;
        }

        if (!ConsoleActive)
            return false;

        // handle input events here
        if (e.type == EventType.KeyDown)
        {
            switch(e.keyCode)
            {
                case KeyCode.UpArrow:
                    if (CommandHistoryPosition > 0)
                    {
                        if (CommandHistoryPosition == CommandHistory.Count - 1)
                            CommandHistory[CommandHistory.Count - 1] = EditField.Value;
                        CommandHistoryPosition--;
                        EditField.Value = CommandHistory[CommandHistoryPosition];
                    }
                    break;
                case KeyCode.DownArrow:
                    if (CommandHistoryPosition < CommandHistory.Count - 1)
                    {
                        CommandHistoryPosition++;
                        EditField.Value = CommandHistory[CommandHistoryPosition];
                    }
                    break;
            }
        }

        return true;
    }

    public void Update()
    {
        Utils.SetRendererEnabledWithChildren(gameObject, ConsoleActive);
        // we do this in every frame because WriteLine needs to be multithreaded.
        TextRendererA.Text = string.Join("\n", ConsoleLines.ToArray());
        TextObject.transform.localPosition = new Vector3(2, ConsoleHeight - 14 - TextRendererA.Height, -0.001f);
    }

    /// outside functions here
    List<string> ConsoleLines = new List<string>();
    public void WriteLine(string line, params object[] args)
    {
        lock (ConsoleLines)
        {
            ConsoleLines.AddRange(string.Format(line, args).Split(new char[] { '\n' }));
            if (ConsoleLines.Count > 100) // remove first N lines if range is too large
                ConsoleLines.RemoveRange(0, ConsoleLines.Count - 100);
        }
    }

    public static string[] SplitArguments(string commandLine)
    {
        var parmChars = commandLine.ToCharArray();
        var inSingleQuote = false;
        var inDoubleQuote = false;
        for (var index = 0; index < parmChars.Length; index++)
        {
            if (parmChars[index] == '"' && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
                parmChars[index] = '\n';
            }
            if (parmChars[index] == '\'' && !inDoubleQuote)
            {
                inSingleQuote = !inSingleQuote;
                parmChars[index] = '\n';
            }
            if (!inSingleQuote && !inDoubleQuote && parmChars[index] == ' ')
                parmChars[index] = '\n';
        }
        return (new string(parmChars)).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
    }

    public void ExecuteCommand(string cmd)
    {
        if (cmd.Trim().Length <= 0)
            return;

        string[] args = SplitArguments(cmd);
        args[0] = args[0].ToLower();

        bool cmdFound = false;
        // now, go through ClientConsoleCommands
        System.Reflection.MethodInfo[] cmds = CommandHandler.GetType().GetMethods();
        for (int i = 0; i < cmds.Length; i++)
        {
            if (cmds[i].Name.ToLower() == args[0] &&
                cmds[i].IsPublic)
            {
                try
                {
                    cmds[i].Invoke(CommandHandler, args.Skip(1).ToArray());
                    cmdFound = true;
                }
                catch (System.Reflection.TargetParameterCountException)
                {
                    if (args.Length - 1 < cmds[i].GetParameters().Length)
                        WriteLine("{0}: too few arguments.", args[0]);
                    else WriteLine("{0}: too many arguments.", args[0]);
                    cmdFound = true;
                }
                catch (ArgumentException) // not a command, commands accept strings
                {

                }
                break;
            }
        }

        if (!cmdFound)
        {
            WriteLine("{0}: command not found.", args[0]);
        }
    }
}
