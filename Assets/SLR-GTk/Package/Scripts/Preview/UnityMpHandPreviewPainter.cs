using System;
using Mediapipe.Tasks.Vision.HandLandmarker;
using UnityEngine;
using UnityEngine.UI;

namespace Preview {
    public enum PainterMode {
        ImageOnly = 0,
        SkeletonOnly = 1,
        ImageAndSkeleton = 2
    }
    
    public class UnityMpHandPreviewPainter: MonoBehaviour 
    {
        public PainterMode painterMode = PainterMode.ImageAndSkeleton;

        [SerializeField]
        private RawImage screen;

        [SerializeField] private bool preserveAR = false;

        private RenderTexture _rt;

        private HandLandmarkerResult? _result;
        private Texture2D _image;
        private bool _freeOnUse;
        
        public bool Visible { get; private set; }
        
        private Material _graphMaterial;
        public float pointRadius = 1f;
        public float strokeWidth = 0.005f;
        public Color pointColor = Color.red;
        public Color lineColor = Color.blue;

        private static readonly int LandmarksPresent = Shader.PropertyToID("_LandmarksPresent");
        private static readonly int PointColor = Shader.PropertyToID("_PointColor");
        private static readonly int LineColor = Shader.PropertyToID("_LineColor");
        private static readonly int Radius = Shader.PropertyToID("_Radius");
        private static readonly int Points = Shader.PropertyToID("_Points");
        private static readonly int StrokeWidth = Shader.PropertyToID("_StrokeWidth");
        private static readonly int AspectRatio = Shader.PropertyToID("_AspectRatio");
        private static readonly int DrawingMode = Shader.PropertyToID("_DrawingMode");

        private void Awake() {
            _graphMaterial = new Material(Shader.Find("Nana/HandLandmarkAnnotator"));
        }

        public void Hide() {
            Visible = false;
        }
        
        public void Show() {
            Visible = true;
        }
        
        
        public void UpdateLandmarks(HandLandmarkerResult? result) {
            _result = result;
        }
        
        public void UpdateImage(Texture2D image, bool freeOnUse = true) {
            if (_freeOnUse) CustomTextureManager.ScheduleDeletion(_image);
            _image = image;
            _freeOnUse = freeOnUse;
        }
        
        void Update() {
            if (!Visible) {
                screen.enabled = false;
            }
            else {
                if (!screen.enabled) {
                    screen.enabled = true;
                }
                if (_image) {
                    if (!_rt || _image.width != _rt.width || _image.height != _rt.height) {
                        if (_rt) CustomTextureManager.DeleteNow(_rt);
                        _rt = new RenderTexture(_image.width, _image.height, 0);
                        _rt.Create();
                        screen.texture = _rt;
                    } 
                    if (_result is { handLandmarks: not null } && _result.Value.handLandmarks.Count > 0 && _result.Value.handLandmarks[0] is {landmarks: not null} && _result.Value.handLandmarks[0].landmarks.Count > 0) {
                        var landmarks = _result.Value.handLandmarks[0].landmarks;

                        Vector4[] points = new Vector4[landmarks.Count];
                        
                        for (int i = 0; i < landmarks.Count && i < 21; i++) {
                            points[i] = new Vector2(landmarks[i].x, landmarks[i].y);
                        }

                        _graphMaterial.SetInt(LandmarksPresent, 1);
                        _graphMaterial.SetFloat(Radius, pointRadius);
                        _graphMaterial.SetVectorArray(Points, points);
                        _graphMaterial.SetFloat(StrokeWidth, strokeWidth);
                        _graphMaterial.SetColor(PointColor, pointColor);
                        _graphMaterial.SetColor(LineColor, lineColor);
                    }
                    else {
                        _graphMaterial.SetInt(LandmarksPresent, 0);
                    }
                    _graphMaterial.SetFloat(AspectRatio, !preserveAR ? 1.0f * _image.width / _image.height : 1.0f);
                    _graphMaterial.SetInt(DrawingMode, (int) painterMode);

                    Graphics.Blit(_image, _rt, _graphMaterial);
                }
            }
        }

        private void FixedUpdate() {
            CustomTextureManager.DeleteTextures();
        }

        private void OnDisable() {
            CustomTextureManager.DeleteTextures();
        }
    }
}