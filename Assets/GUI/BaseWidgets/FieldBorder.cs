using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class FieldBorder : Widget
{
    private MeshRenderer Renderer;
    private MeshFilter Filter;

    public void Start()
    {
        Renderer = gameObject.AddComponent<MeshRenderer>();
        Renderer.material = new Material(MainCamera.MainShader);
        Filter = gameObject.AddComponent<MeshFilter>();
        UpdateMesh();
    }

    private int LastWidth = -1;
    private int LastHeight = -1;
    public void UpdateMesh()
    {
        if (Filter.mesh != null && LastWidth == Width && LastHeight == Height)
            return;

        LastWidth = Width;
        LastHeight = Height;

        Mesh mesh = Filter.mesh;
        if (mesh == null) mesh = new Mesh();
        mesh.Clear();

        Vector3[] qv = new Vector3[4 * 2];
        Color32[] qc = new Color32[4 * 2];
        int pp = 0, ppc = pp;

        // top line
        qv[pp++] = new Vector3(0, 0);
        qc[ppc++] = new Color32(0, 0, 0, 255);
        qv[pp++] = new Vector3(Width - 1, 0);
        qc[ppc++] = new Color32(0, 0, 0, 255);

        // left line
        qv[pp++] = new Vector3(0, 0);
        qc[ppc++] = new Color32(0, 0, 0, 255);
        qv[pp++] = new Vector3(0, Height - 2);
        qc[ppc++] = new Color32(0, 0, 0, 255);

        // right line
        qv[pp++] = new Vector3(Width - 1, 0);
        qc[ppc++] = new Color32(90, 113, 99, 255);
        qv[pp++] = new Vector3(Width - 1, Height - 2);
        qc[ppc++] = new Color32(90, 113, 99, 255);

        // bottom line
        qv[pp++] = new Vector3(0, Height - 1);
        qc[ppc++] = new Color32(90, 113, 99, 255);
        qv[pp++] = new Vector3(Width - 1, Height - 1);
        qc[ppc++] = new Color32(90, 113, 99, 255);

        mesh.vertices = qv;
        mesh.colors32 = qc;
        int[] qt = new int[4 * 2];
        for (int i = 0; i < qt.Length; i++)
            qt[i] = i;
        mesh.SetIndices(qt, MeshTopology.Lines, 0);
        Filter.mesh = mesh;
    }

    public void Update()
    {
        UpdateMesh();
    }
}