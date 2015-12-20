using UnityEngine;
using System.Collections;

public class MainCameraScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
        // set w/h to screen res
        Camera camera = GetComponent<Camera>();
        //camera.transform.position = new Vector3(-0.5f, 0.5f);
        camera.orthographicSize = Screen.height / 2;
        camera.transform.Translate((float)Screen.width / 2 / 100, (float)Screen.height / 2 / 100, 0, Space.World);
        camera.projectionMatrix *= Matrix4x4.Scale(new Vector3(100, -100, 1));
        Debug.Log(string.Format("{0}x{1}", Screen.width, Screen.height));
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
