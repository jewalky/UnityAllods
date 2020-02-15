using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AllodsUI
{
    public class UIElements
    {
        [MenuItem("GameObject/Allods UI/Text", false, 0)]
        public static void AddAllodsText()
        {
            GameObject go = new GameObject("AllodsText");
            go.transform.localScale = new Vector3(1, 1, 1);
            go.AddComponent<AllodsText>();
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
            go.AddComponent<AllodsImage>();
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

        [MenuItem("GameObject/Allods UI/Text Field", false, 0)]
        public static void AddAllodsTextField()
        {
            GameObject go = new GameObject("AllodsTextField");
            go.transform.localScale = new Vector3(1, 1, 1);
            go.AddComponent<AllodsTextField>();
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
}