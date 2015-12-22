using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    private GameObject m_Background = null;
    private GameObject m_Loading = null;
    private GameObject m_Loading2 = null;

    void Start()
    {
        transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        m_Background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        MeshRenderer mr = m_Background.GetComponent<MeshRenderer>();
        mr.material = new Material(MainCamera.MainShader);
        mr.material.SetColor("_Color", new Color(0, 0, 0, 1));
        m_Background.transform.parent = transform;
        m_Background.transform.localPosition = new Vector3(Screen.width / 2, Screen.height / 2, 0.1f);
        m_Background.transform.localScale = new Vector3(Screen.width, Screen.height, 1);
        m_Background.name = "m_Background";
        m_Loading = Fonts.Font1.Render("Loading...", Font.Align.Center, Screen.width, 16, false);
        m_Loading.transform.parent = transform;
        m_Loading.transform.localPosition = new Vector3(0, Screen.height / 2 - 24, 0.05f);
        m_Loading.transform.localScale = new Vector3(100f, 100f, 1);
        m_Loading.name = "m_Loading";
    }

    private void SetNamePercent(string text, float percent)
    {
        string textf = text;
        if (percent >= 0)
            textf += string.Format(" ({0}% done)", (int)(percent * 100));
        if (m_Loading2 != null)
            GameObject.Destroy(m_Loading2);
        m_Loading2 = Fonts.Font1.Render(textf, Font.Align.Center, Screen.width, 16, false);
        m_Loading2.transform.parent = transform;
        m_Loading2.transform.localPosition = new Vector3(0, Screen.height / 2, 0.05f);
        m_Loading2.transform.localScale = new Vector3(100f, 100f, 1);
        m_Loading2.GetComponent<MeshRenderer>().material.SetColor("_Color", new Color(0.8f, 0.8f, 0.8f, 1));
        m_Loading2.name = "m_Loading2";
    }

    private float timeStart = 0;
    private bool loaded = false;
    void Update()
    {
        if (timeStart == 0) timeStart = Time.realtimeSinceStartup;
        float f = ObstacleClassLoader.LoadSprites(0.05f);
        SetNamePercent("Obstacle sprites...", f);
        if (f == 1 && !loaded)
        {
            Debug.Log(string.Format("loaded in {0}s", Time.realtimeSinceStartup - timeStart));
            MouseCursor.SetCursor(MouseCursor.CurDefault);
            MapView.Instance.gameObject.SetActive(true);
            MapView.Instance.InitFromFile("an_heaven_5_8.alm");
            loaded = true;
            gameObject.SetActive(false);
        }
    }
}
