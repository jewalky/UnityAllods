using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIElements
{
    [MenuItem("GameObject/Allods UI/Text", false, 0)]
    public static void AddAllodsText()
    {
        GameObject go = new GameObject("AllodsText");
        go.transform.localScale = new Vector3(1, 1, 1);
        AllodsText text = go.AddComponent<AllodsText>();
        if (Selection.activeTransform != null)
        {
            GameObject pGO = Selection.activeTransform.gameObject;
            RectTransform pRect = pGO.GetComponent<RectTransform>();
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(pRect);
            go.transform.localPosition = new Vector2(0, 0);
            go.transform.localScale = new Vector3(1, 1, 1);
            Selection.SetActiveObjectWithContext(go, pGO);
        }
    }

    [MenuItem("GameObject/Allods UI/Image", false, 0)]
    public static void AddAllodsImage()
    {
        GameObject go = new GameObject("AllodsImage");
        go.transform.localScale = new Vector3(1, 1, 1);
        AllodsImage image = go.AddComponent<AllodsImage>();
        if (Selection.activeTransform != null)
        {
            GameObject pGO = Selection.activeTransform.gameObject;
            RectTransform pRect = pGO.GetComponent<RectTransform>();
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(pRect);
            go.transform.localPosition = new Vector2(0, 0);
            go.transform.localScale = new Vector3(1, 1, 1);
            Selection.SetActiveObjectWithContext(go, pGO);
        }
    }
}

