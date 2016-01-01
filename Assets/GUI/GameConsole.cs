using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
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

    public bool ConsoleEnabled = true;
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
    GameConsoleCommands CommandHandler = new GameConsoleCommands();

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
        transform.position = new Vector3(0, 0, MainCamera.MouseZ + 0.01f);

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
                        EditField.CursorPosition = EditField.Value.Length;
                    }
                    break;
                case KeyCode.DownArrow:
                    if (CommandHistoryPosition < CommandHistory.Count - 1)
                    {
                        CommandHistoryPosition++;
                        EditField.Value = CommandHistory[CommandHistoryPosition];
                        EditField.CursorPosition = EditField.Value.Length;
                    }
                    break;
            }
        }
        else if (e.rawType == EventType.MouseMove)
        {
            if (ConsoleActive)
                MouseCursor.SetCursor(MouseCursor.CurDefault);
        }

        return true;
    }

    public void Update()
    {
        if (!ConsoleEnabled)
            ConsoleActive = false;
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

    public static string JoinArguments(string[] args)
    {
        string os = "";
        foreach (string arg in args)
        {
            bool enclose = arg.Contains(' ');
            if (enclose) os += '"';
            foreach (char ch in arg)
            {
                if (ch == '\\')
                    os += "\\\\";
                else if (ch == '"')
                    os += "\\\"";
                else if (ch == '\'')
                    os += "\\'";
                else os += ch;
            }
            if (enclose) os += '"';
            os += ' ';
        }

        return os;
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

    public bool ExecuteCommand(string cmd)
    {
        if (cmd.Trim().Length <= 0)
            return false;

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
            // now, go through Config class
            string varVal = (args.Length > 1) ? args[1] : null;
            PropertyInfo[] configFields = typeof(Config).GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            foreach (PropertyInfo field in configFields)
            {
                if (field.Name.ToLower() == args[0])
                {
                    // check type. only int, float, bool and string values are supported.
                    if (varVal == null)
                    {
                        try
                        {
                            string oV;
                            if (field.PropertyType == typeof(int) ||
                                field.PropertyType == typeof(float) ||
                                field.PropertyType == typeof(bool) ||
                                field.PropertyType == typeof(string)) oV = field.GetValue(null, null).ToString();
                            else continue;
                            WriteLine("{0} is \"{1}\"", args[0], oV);
                            cmdFound = true;
                            break;
                        }
                        catch (Exception e)
                        {
                            cmdFound = false;
                            break;
                        }
                    }
                    else
                    {
                        try
                        {
                            if (field.PropertyType == typeof(int))
                            {
                                try
                                {
                                    field.SetValue(null, int.Parse(varVal), null);
                                    WriteLine("{0} is now \"{1}\"", args[0], field.GetValue(null, null).ToString());
                                }
                                catch (Exception)
                                {
                                    WriteLine("{0}: should be an integer.", args[0]);
                                }
                            }
                            else if (field.PropertyType == typeof(float))
                            {
                                try
                                {
                                    field.SetValue(null, float.Parse(varVal), null);
                                    WriteLine("{0} is now \"{1}\"", args[0], field.GetValue(null, null).ToString());
                                }
                                catch (Exception)
                                {
                                    WriteLine("{0}: should be a float.", args[0]);
                                }
                            }
                            else if (field.PropertyType == typeof(bool))
                            {
                                try
                                {
                                    field.SetValue(null, (int.Parse(varVal) != 0), null);
                                    WriteLine("{0} is now \"{1}\"", args[0], field.GetValue(null, null).ToString());
                                }
                                catch (Exception)
                                {
                                    try
                                    {
                                        field.SetValue(null, bool.Parse(varVal), null);
                                    }
                                    catch (Exception)
                                    {
                                        WriteLine("{0}: should be a boolean.", args[0]);
                                    }
                                }
                            }
                            else if (field.PropertyType == typeof(string))
                            {
                                field.SetValue(null, varVal, null);
                                WriteLine("{0} is now \"{1}\"", args[0], field.GetValue(null, null).ToString());
                            }
                            else continue;
                            cmdFound = true;
                            break;
                        }
                        catch (Exception)
                        {
                            WriteLine("{0} is read-only.", args[0]);
                            cmdFound = true;
                            break;
                        }
                    }
                }
            }
        }

        if (!cmdFound)
        {
            WriteLine("{0}: command not found.", args[0]);
            return false;
        }

        return true;
    }
}
