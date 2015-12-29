using UnityEngine;
using System.Collections;
using System.IO;
using System;

public class MainCamera : MonoBehaviour {
    public const float OverlayZ = -83;
    public const float InterfaceZ = -84;
    public const float MouseZ = -95;

    private static MainCamera _Instance;
    public static MainCamera Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = GameObject.FindObjectOfType<MainCamera>();
            return _Instance;
        }
    }

    public Shader _MainShader;
    public Shader _MainShaderPaletted;
    public Shader _TerrainShader;
    public Shader _TerrainFOWShader;

    public static Shader MainShader { get { return Instance._MainShader; } }
    public static Shader MainShaderPaletted { get { return Instance._MainShaderPaletted; } }
    public static Shader TerrainShader { get { return Instance._TerrainShader; } }
    public static Shader TerrainFOWShader { get { return Instance._TerrainFOWShader; } }

    // Use this for initialization
    private AllodsTextRenderer m_fpsr = null;
    private GameObject m_fpso = null;

    public Images.AllodsSprite arrow;
    public Texture2D arrowTex;

    void Start ()
    {
        arrow = Images.Load256("graphics/projectiles/archer/arrow.256", false);
        arrowTex = arrow.Atlas;

        // set w/h to screen res
        Camera camera = GetComponent<Camera>();
        //camera.transform.position = new Vector3(-0.5f, 0.5f);
        camera.orthographicSize = Screen.height / 2;
        camera.transform.Translate((float)Screen.width / 2 / 100, (float)Screen.height / 2 / 100, 0, Space.World);
        camera.projectionMatrix *= Matrix4x4.Scale(new Vector3(100, -100, 1));
        Debug.Log(string.Format("{0}x{1}", Screen.width, Screen.height));

        m_fpsr = new AllodsTextRenderer(Fonts.Font1, Font.Align.Right, Screen.width-176);
        m_fpso = m_fpsr.GetNewGameObject(0.01f, SceneRoot.Instance.transform);
        m_fpso.transform.position = new Vector3(0, 0, OverlayZ+0.99f);
    }

    // Update is called once per frame
    float fps_timer = 0;
    int fps_frameCounter = 0;
    float fps_timeCounter = 0.0f;
    float fps_lastFramerate = 0.0f;
    float fps_refreshTime = 0.5f;
    bool fps_enabled = true;
    float gc_timer = 0;

    void Update ()
    {
        gc_timer += Time.unscaledDeltaTime;
        if (gc_timer >= 5)
        {
            Resources.UnloadUnusedAssets();
            gc_timer = 0;
        }

        if (fps_timeCounter < fps_refreshTime)
        {
            fps_timeCounter += Time.unscaledDeltaTime;
            fps_frameCounter++;
        }
        else
        {
            //This code will break if you set your m_refreshTime to 0, which makes no sense.
            fps_lastFramerate = (float)fps_frameCounter / fps_timeCounter;
            fps_frameCounter = 0;
            fps_timeCounter = 0.0f;
        }

        fps_timer += Time.unscaledDeltaTime;
        //if (fps_timer >= 1) // display FPS string
        {
            if (fps_enabled)
            {
                string fpstr = string.Format("FPS: {0}\nMeshDebug: {1}", (int)fps_lastFramerate, m_fpsr.Height);
                if (MapLogic.Instance.IsLoaded)
                {
                    fpstr += string.Format("\nMouseCell: {0},{1}\nScroll: {2},{3}", MapView.Instance.MouseCellX, MapView.Instance.MouseCellY,
                                                                                    MapView.Instance.ScrollX, MapView.Instance.ScrollY);
                    MapLogicObject ho = MapView.Instance.HoveredObject;
                    if (ho != null)
                    {
                        if (ho.GetObjectType() == MapLogicObjectType.Structure)
                            fpstr += string.Format("\nHoveredObject: {0}", ((MapLogicStructure)ho).Class.DescText);
                        else if (ho.GetObjectType() == MapLogicObjectType.Monster)
                            fpstr += string.Format("\nHoveredObject: {0}", ((MapLogicUnit)ho).Template.Name);
                    }
                    else fpstr += "\nHoveredObject: <none>";
                }
                m_fpsr.Text = fpstr;
                //fps_timer = 0;
            }
        }
    }

    public string TakeScreenshot()
    {
        if (!Directory.Exists("screenshots"))
        {
            try
            {
                DirectoryInfo info = Directory.CreateDirectory("screenshots");
            }
            catch (IOException)
            {
                return null;
            }
        }

        string filenameBase = string.Format("screenshots/Screenshot_{0:dd-MM-yyyy_HH-mm-ss}", DateTime.Now);
        int filenameAdd = 0;
        while (File.Exists(string.Format("{0}_{1}.png", filenameBase, filenameAdd)))
            filenameAdd++;
        if (filenameAdd > 0) filenameBase += "_" + filenameAdd.ToString();
        filenameBase += ".png";
        StartCoroutine(TakeScreenshotImpl(filenameBase));
        return filenameBase;
    }

    // since we can't screenshot midframe,
    // process scheduled screenshot here.
    IEnumerator TakeScreenshotImpl(string screenshotFileName)
    {
        Texture2D outtex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        yield return new WaitForEndOfFrame();

        outtex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        outtex.Apply();
        byte[] outpng = outtex.EncodeToPNG();

        try
        {
            FileStream fs = File.OpenWrite(screenshotFileName);
            fs.Write(outpng, 0, outpng.Length);
            fs.Close();
            GameConsole.Instance.WriteLine("Saved screenshot as \"{0}\".", screenshotFileName);
        }
        catch (IOException)
        {
            GameConsole.Instance.WriteLine("Failed to save screenshot as \"{0}\"!", screenshotFileName);
        }
    }

    void OnPreRender()
    {
        MouseCursor.OnPreRenderCursor();
    }
}
