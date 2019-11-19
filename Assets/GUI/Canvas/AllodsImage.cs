using UnityEngine;
using UnityEngine.UI;
using System;

[AddComponentMenu("Allods UI/Image")]
public class AllodsImage : MaskableGraphic
{
    [SerializeField]
    public string Filename;
    [SerializeField]
    public Color32 Colorkey;

    private Color32 _LastColorkey;
    private string _LastFilename;
    private Texture2D _Texture;
    private bool _LastFailed;

    private void CheckTexture()
    {
        if (_LastFilename != Filename || !_LastColorkey.Equals(Colorkey) || (_Texture == null && !_LastFailed))
        {
            try
            {
                if (Colorkey.a > 127)
                {
                    uint rgbaColorkey = (uint)(Colorkey.r << 16 | Colorkey.g << 8 | Colorkey.b);
                    if (Filename.ToLowerInvariant().Contains(".bmp"))
                        _Texture = Images.LoadImage(Filename, rgbaColorkey, Images.ImageType.AllodsBMP);
                    else _Texture = Images.LoadImage(Filename, rgbaColorkey, Images.ImageType.Unity);
                }
                else
                {
                    if (Filename.ToLowerInvariant().Contains(".bmp"))
                        _Texture = Images.LoadImage(Filename, Images.ImageType.AllodsBMP);
                    else _Texture = Images.LoadImage(Filename, Images.ImageType.Unity);
                }
                _LastFailed = false;
            }
            catch (Exception)
            {
                _LastFailed = true;
                _Texture = null;
            }
            _LastFilename = Filename;
            _LastColorkey = Colorkey;
        }
    }

    public override Texture mainTexture
    {
        get
        {
            CheckTexture();
            return _Texture;
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float x = rectTransform.rect.width * rectTransform.pivot.x;
        float y = rectTransform.rect.height * rectTransform.pivot.y;

        float w = rectTransform.rect.width;
        float h = rectTransform.rect.height;
        float halfW = w * rectTransform.pivot.x;
        float halfH = h * rectTransform.pivot.y;

        vh.AddVert(new Vector3(-halfW, -halfH+h, 0), new Color(1, 1, 1, 1), new Vector2(0, 0));
        vh.AddVert(new Vector3(-halfW+w, -halfH+h, 0), new Color(1, 1, 1, 1), new Vector2(1, 0));
        vh.AddVert(new Vector3(-halfW+w, -halfH, 0), new Color(1, 1, 1, 1), new Vector2(1, 1));
        vh.AddVert(new Vector3(-halfW, -halfH, 0), new Color(1, 1, 1, 1), new Vector2(0, 1));
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    [EasyButtons.Button]
    public override void SetNativeSize()
    {
        if (_Texture != null)
        {
            rectTransform.sizeDelta = new Vector2(_Texture.width, _Texture.height);
        }
    }
}