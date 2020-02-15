using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEditor;

namespace AllodsUI
{
    [AddComponentMenu("Allods UI/Image")]
    public class AllodsImage : MaskableGraphic
    {
        // set in the editor
        [SerializeField]
        private string _Filename;
        [SerializeField]
        private Color32 _Colorkey;
        [SerializeField]
        private Rect _InnerRect;

        public string Filename
        {
            get { return _Filename; }
            set
            {
                if (_Filename == value)
                    return;
                _Filename = value;
                UpdateGeometry();
            }
        }

        public Color32 Colorkey
        {
            get { return _Colorkey; }
            set
            {
                if (_Colorkey.Equals(value))
                    return;
                _Colorkey = value;
                UpdateGeometry();
            }
        }

        public Rect InnerRect
        {
            get { return _InnerRect; }
            set
            {
                if (_InnerRect == value)
                    return;
                _InnerRect = value;
                UpdateGeometry();
            }
        }

        // set ingame
        public Texture2D TextureOverride
        {
            get
            {
                return _TextureOverride;
            }

            set
            {
                if (_TextureOverride == value)
                    return;
                _TextureOverride = value;
                UpdateMaterial();
            }
        }

        private Texture2D _TextureOverride;
        private Color32 _LastColorkey;
        private string _LastFilename;
        private Texture2D _Texture;
        private bool _LastFailed;

        private void CheckTexture()
        {
            if (_TextureOverride != null)
            {
                _Texture = _TextureOverride;
                return;
            }

            if (_LastFilename != Filename || !_LastColorkey.Equals(Colorkey) || (_Texture == null && !_LastFailed))
            {
                try
                {
                    bool setRec = (_LastFilename != Filename);
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
                    if (setRec)
                    {
                        InnerRect = new Rect(0, 0, _Texture.width, _Texture.height);
                    }
                }
                catch (Exception)
                {
                    _LastFailed = true;
                    _Texture = null;
                    InnerRect = new Rect(0, 0, 0, 0);
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

            float minU = 0, minV = 0, maxU = 1, maxV = 1;
            if (_Texture != null)
            {
                minU = InnerRect.xMin / _Texture.width;
                maxU = InnerRect.xMax / _Texture.width;
                minV = InnerRect.yMin / _Texture.height;
                maxV = InnerRect.yMax / _Texture.height;
            }

            vh.AddVert(new Vector3(-halfW, -halfH, 0), new Color(1, 1, 1, 1), new Vector2(minU, minV));
            vh.AddVert(new Vector3(-halfW + w, -halfH, 0), new Color(1, 1, 1, 1), new Vector2(maxU, minV));
            vh.AddVert(new Vector3(-halfW + w, -halfH + h, 0), new Color(1, 1, 1, 1), new Vector2(maxU, maxV));
            vh.AddVert(new Vector3(-halfW, -halfH + h, 0), new Color(1, 1, 1, 1), new Vector2(minU, maxV));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        [EasyButtons.Button]
        public override void SetNativeSize()
        {
            if (_Texture != null)
            {
                rectTransform.sizeDelta = new Vector2(_Texture.width, _Texture.height);
                UpdateGeometry();
            }
        }

        [EasyButtons.Button]
        public void InnerRectToTexture()
        {
            if (_Texture != null)
            {
                InnerRect = new Rect(0, 0, _Texture.width, _Texture.height);
                UpdateGeometry();
            }
        }

        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            baseMaterial.color = this.color;
            return baseMaterial;
        }
    }
}