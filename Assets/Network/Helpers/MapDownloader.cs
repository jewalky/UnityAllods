using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapDownloader : MonoBehaviour
{
    private static MapDownloader _Instance = null;
    public static MapDownloader Instance
    {
        get
        {
            if (_Instance == null) _Instance = FindObjectOfType<MapDownloader>();
            return _Instance;
        }
    }

    public void Kill()
    {
        _Instance = null;
        Destroy(gameObject);
        Destroy(this);
    }

    public void Setup(string filename)
    {
        FileName = filename;
        Dl_FullSize = 0;
        Dl_DoneSize = 0;
    }

    public string FileName { get; private set; }

    public int Dl_DoneSize;
    public int Dl_FullSize;
    public byte[] Dl_Content;
        
    private GameObject Background;
    private AllodsTextRenderer RendererMapName;
    private GameObject ObjectMapName;
    private AllodsTextRenderer RendererMapPercent;
    private GameObject ObjectMapPercent;

    public void Start()
    {
        _Instance = this;

        transform.localScale = new Vector3(1, 1, 0.01f);

        Background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        MeshRenderer mr = Background.GetComponent<MeshRenderer>();
        mr.material = new Material(MainCamera.MainShader);
        mr.material.SetColor("_Color", new Color(0, 0, 0, 1));
        Background.transform.parent = transform;
        Background.transform.localPosition = new Vector3(Screen.width / 2, Screen.height / 2, 0.1f);
        Background.transform.localScale = new Vector3(Screen.width, Screen.height, 1);
        Background.name = "Background";

        RendererMapName = new AllodsTextRenderer(Fonts.Font1, Font.Align.Center, Screen.width, Fonts.Font1.LineHeight, false);
        RendererMapName.Text = "downloading map " + FileName + "...";
        ObjectMapName = RendererMapName.GetNewGameObject(0.01f, transform, 100);
        ObjectMapName.transform.localPosition = new Vector3(0, Screen.height / 2 - 16, -1);

        RendererMapPercent = new AllodsTextRenderer(Fonts.Font1, Font.Align.Center, Screen.width, Fonts.Font1.LineHeight, false);
        RendererMapPercent.Text = "0% complete";
        RendererMapPercent.Material.color = new Color(0.6f, 0.6f, 0.6f);
        ObjectMapPercent = RendererMapPercent.GetNewGameObject(0.01f, transform, 100);
        ObjectMapPercent.transform.localPosition = new Vector3(0, Screen.height / 2, -1);
    }

    public void Update()
    {
        float loadPc = 0;
        if (Dl_FullSize > 0)
            loadPc = (float)Dl_DoneSize * 100 / Dl_FullSize;

        RendererMapPercent.Text = string.Format("{0}% complete", (int)loadPc);
    }
}