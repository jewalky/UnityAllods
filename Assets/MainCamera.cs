using UnityEngine;
using System.Collections;

public class MainCamera : MonoBehaviour {
    public static readonly float OverlayZ = -83;
    public static readonly float InterfaceZ = -84;
    public static readonly float MouseZ = -85;

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

    public static Shader MainShader { get { return Instance._MainShader; } }
    public static Shader MainShaderPaletted { get { return Instance._MainShaderPaletted; } }
    public static Shader TerrainShader { get { return Instance._TerrainShader; } }

    // Use this for initialization
    private AllodsTextRenderer m_fpsr = null;
    private GameObject m_fpso = null;

    void Start ()
    {
        // set w/h to screen res
        Camera camera = GetComponent<Camera>();
        //camera.transform.position = new Vector3(-0.5f, 0.5f);
        camera.orthographicSize = Screen.height / 2;
        camera.transform.Translate((float)Screen.width / 2 / 100, (float)Screen.height / 2 / 100, 0, Space.World);
        camera.projectionMatrix *= Matrix4x4.Scale(new Vector3(100, -100, 1));
        Debug.Log(string.Format("{0}x{1}", Screen.width, Screen.height));

        m_fpsr = new AllodsTextRenderer(Fonts.Font1);
        m_fpso = new GameObject();
        m_fpso.AddComponent<MeshFilter>().mesh = m_fpsr.Mesh;
        MeshRenderer mr = m_fpso.AddComponent<MeshRenderer>();
        mr.material = m_fpsr.Material;
        m_fpso.name = "FPSString";
        m_fpso.transform.position = new Vector3(0, 0, OverlayZ);
        m_fpso.transform.localScale = new Vector3(1, 1, 1);
        GameObject shadow = GameObject.Instantiate(m_fpso);
        shadow.name = "FPSString_Shadow";
        shadow.transform.parent = m_fpso.transform;
        shadow.transform.localPosition = new Vector3(0.01f, 0.01f, 0.01f);
        shadow.GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0, 1);
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
        if (gc_timer >= 1)
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
                string fpstr = string.Format("FPS: {0}", (int)fps_lastFramerate);
                if (MapLogic.Instance.IsLoaded)
                    fpstr += string.Format("\nMouseCell: {0},{1}\nScroll: {2},{3}", MapView.Instance.MouseCellX, MapView.Instance.MouseCellY,
                                                                                    MapView.Instance.ScrollX, MapView.Instance.ScrollY);
                m_fpsr.Text = fpstr;
                //fps_timer = 0;
            }
        }
    }
}
