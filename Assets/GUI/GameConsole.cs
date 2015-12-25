using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class GameConsole : MonoBehaviour, IUiEventProcessor
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

    AllodsTextRenderer EditRendererA;
    GameObject EditObject;
    MeshRenderer EditRenderer;

    // Mesh for cursor, mesh for selection, mesh for cat...owait.
    GameObject SelectionObject;
    Mesh SelectionMesh;

    int Selection1;
    int Selection2;
    bool EditCursor;
    float EditCursorTimer;

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

        // prepare editing field presentation
        EditRendererA = new AllodsTextRenderer(Fonts.Font2);
        EditObject = EditRendererA.GetNewGameObject(0.01f, transform, 100);
        EditObject.transform.localPosition = new Vector3(2, ConsoleHeight - 13, -0.001f);
        EditRenderer = EditObject.GetComponent<MeshRenderer>();
        EditRenderer.material.color = new Color(1, 1, 1);
        EditRendererA.Text = "> ";

        SelectionObject = Utils.CreateObject();
        SelectionObject.transform.parent = transform;
        SelectionObject.transform.localScale = new Vector3(1, 1, 1);
        SelectionObject.transform.localPosition = new Vector3(2, ConsoleHeight - 13, -0.0005f);
        SelectionMesh = new Mesh();
        MeshFilter selectionFilter = SelectionObject.AddComponent<MeshFilter>();
        MeshRenderer selectionRenderer = SelectionObject.AddComponent<MeshRenderer>();
        selectionFilter.mesh = SelectionMesh;
        selectionRenderer.material = new Material(MainCamera.MainShader);

        Selection1 = Selection2 = 0;

        WriteLine("Welcome to UnityAllods!");
    }

    public bool ProcessEvent(Event e)
    {
        if (e.type == EventType.KeyDown &&
            e.keyCode == KeyCode.BackQuote)
        {
            ConsoleActive = !ConsoleActive;
            if (ConsoleActive)
            {
                EditRendererA.Text = "> ";
                Selection1 = Selection2 = 0;
            }
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
                            CommandHistory[CommandHistory.Count - 1] = EditRendererA.Text.Substring(2);
                        CommandHistoryPosition--;
                        EditRendererA.Text = "> " + CommandHistory[CommandHistoryPosition];
                        Selection2 = Selection1 = EditRendererA.Text.Length - 2;
                    }
                    EditCursor = true;
                    break;
                case KeyCode.DownArrow:
                    if (CommandHistoryPosition < CommandHistory.Count - 1)
                    {
                        CommandHistoryPosition++;
                        EditRendererA.Text = "> " + CommandHistory[CommandHistoryPosition];
                        Selection2 = Selection1 = EditRendererA.Text.Length - 2;
                    }
                    EditCursor = true;
                    break;
                case KeyCode.Backspace:
                case KeyCode.Delete:
                    int s1 = Mathf.Min(Selection1, Selection2);
                    int s2 = Mathf.Max(Selection1, Selection2);
                    if (s1 != s2)
                    {
                        EditRendererA.Text = EditRendererA.Text.Remove(s1 + 2, s2 - s1);
                        Selection2 = Selection1 = s1;
                    }
                    else
                    {
                        if (e.keyCode == KeyCode.Backspace &&
                            s1 > 0)
                        {
                            EditRendererA.Text = EditRendererA.Text.Remove(Selection2 + 2 - 1, 1);
                            Selection1 = --Selection2;
                        }
                        else if (e.keyCode == KeyCode.Delete &&
                            s1 < EditRendererA.Text.Length - 2)
                        {
                            EditRendererA.Text = EditRendererA.Text.Remove(Selection2 + 2, 1);
                        }
                    }
                    EditCursor = true;
                    break;
                case KeyCode.RightArrow:
                    if (Selection2 < EditRendererA.Text.Length - 2)
                        Selection2++;
                    if (!e.shift) Selection1 = Selection2;
                    EditCursor = true;
                    break;
                case KeyCode.LeftArrow:
                    if (Selection2 > 0)
                        Selection2--;
                    if (!e.shift) Selection1 = Selection2;
                    EditCursor = true;
                    break;
                case KeyCode.Home:
                case KeyCode.PageUp:
                    Selection2 = 0;
                    if (!e.shift) Selection1 = Selection2;
                    EditCursor = true;
                    break;
                case KeyCode.End:
                case KeyCode.PageDown:
                    Selection2 = EditRendererA.Text.Length - 2;
                    if (!e.shift) Selection1 = Selection2;
                    EditCursor = true;
                    break;
                default:
                    if (e.character >= 0x20 && e.character != '`' && e.character != '~')
                    {
                        // type!
                        string possibleText = EditRendererA.Text.Insert(Selection2 + 2, ""+e.character);
                        if (EditRendererA.Font.Width(possibleText) <= Screen.width)
                            EditRendererA.Text = possibleText; // don't allow inserting characters if we don't have space
                        Selection2 = ++Selection1;
                        EditCursor = true;
                    }
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    string cmd = EditRendererA.Text.Substring(2);
                    if (cmd.Trim().Length > 0)
                    {
                        WriteLine(EditRendererA.Text);
                        EditRendererA.Text = "> ";
                        ExecuteCommand(cmd);
                        CommandHistory[CommandHistory.Count - 1] = cmd;
                        CommandHistory.Add("");
                        CommandHistoryPosition = CommandHistory.Count - 1;
                    }
                    Selection1 = Selection2 = 0;
                    break;
            }
        }

        return true;
    }

    private void UpdateMesh()
    {
        int s1 = Mathf.Min(Selection1, Selection2);
        int s2 = Mathf.Max(Selection1, Selection2);
        float s1pos = EditRendererA.Font.Width(EditRendererA.Text.Substring(0, s1 + 2));
        float s2pos = EditRendererA.Font.Width(EditRendererA.Text.Substring(0, s2 + 2));
        float cursorPos = EditRendererA.Font.Width(EditRendererA.Text.Substring(0, Selection2 + 2));

        // selectionMesh 
        SelectionMesh.Clear();
        Vector3[] qv = new Vector3[8];
        Color[] qc = new Color[8];
        int[] qt = new int[8];
        for (int i = 0; i < 8; i++)
            qt[i] = i;
        int pp = 0;
        qv[pp++] = new Vector3(s1pos, -1, 0);
        qv[pp++] = new Vector3(s2pos, -1, 0);
        qv[pp++] = new Vector3(s2pos, 11, 0);
        qv[pp++] = new Vector3(s1pos, 11, 0);
        qv[pp++] = new Vector3(cursorPos, 0, 0);
        qv[pp++] = new Vector3(cursorPos + 1, 0, 0);
        qv[pp++] = new Vector3(cursorPos + 1, 10, 0);
        qv[pp++] = new Vector3(cursorPos, 10, 0);

        for (int i = 0; i < 8; i++)
        {
            if (i < 4) qc[i] = new Color(0, 0, 0, (s1 != s2) ? 1 : 0); // only show this part if selection rect exists!
            else qc[i] = new Color(1, 1, 1, EditCursor ? 1 : 0);
        }

        SelectionMesh.vertices = qv;
        SelectionMesh.colors = qc;
        SelectionMesh.SetIndices(qt, MeshTopology.Quads, 0);
    }

    public void Update()
    {
        if (ConsoleActive) UpdateMesh();
        Utils.SetRendererEnabledWithChildren(gameObject, ConsoleActive);

        EditCursorTimer += Time.unscaledDeltaTime;
        if (EditCursorTimer >= 0.25)
        {
            EditCursorTimer = 0;
            EditCursor = !EditCursor;
        }
    }

    /// outside functions here
    List<string> ConsoleLines = new List<string>();
    public void WriteLine(string line, params object[] args)
    {
        ConsoleLines.AddRange(string.Format(line, args).Split(new char[] { '\n' }));
        if (ConsoleLines.Count > 100) // remove first N lines if range is too large
            ConsoleLines.RemoveRange(0, ConsoleLines.Count - 100);
        TextRendererA.Text = string.Join("\n", ConsoleLines.ToArray());
        TextObject.transform.localPosition = new Vector3(2, ConsoleHeight - 14 - TextRendererA.Height, -0.001f);
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
