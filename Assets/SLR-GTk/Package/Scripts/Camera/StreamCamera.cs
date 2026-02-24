using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Camera {
    
    public class StreamCamera: MonoBehaviour, ICamera {
        private static readonly int _SwapBR = Shader.PropertyToID("_SwapBR");
        private static readonly int RotationAngle = Shader.PropertyToID("_RotationAngle");

        [NonSerialized] private Dictionary<string, Action<Texture2D>> callbacks = new();
        // public bool enableISO = true;
        public CameraSelector cameraSelector = CameraSelector.FIRST_FRONT_CAMERA;
        [NonSerialized] private WebCamTexture webCamTexture;
        [NonSerialized] private WebCamDevice? currentDevice;
        
        private Material _webcamControlShader = null;

        public bool polling = true;

        private void Awake() {
            _webcamControlShader = new Material(Shader.Find("Nana/WebcamControlShader"));
        }

        public void UpdateProps() {
            if (WebCamTexture.devices.Length <= 0) throw new Exception("Camera not connected");
                camera:
            foreach (var device in WebCamTexture.devices) {
                switch (cameraSelector) {
                    case CameraSelector.FIRST_FRONT_CAMERA:
                        if (device.isFrontFacing) {
                            currentDevice = device;
                            goto ISO;
                        }

                        break;
                    case CameraSelector.FIRST_BACK_CAMERA:
                        if (!device.isFrontFacing) {
                            currentDevice = device;
                            goto ISO;
                        }

                        break;
            }

            // TODO: use NatCam? Can disable ISO from there + better interface for frontcam etc.
            }

            currentDevice = WebCamTexture.devices[0];
            ISO:
            
            // TODO - best resolution
            webCamTexture = new WebCamTexture(currentDevice.Value.name, 1280, 720, 30);
            
            
            if (webCamTexture.graphicsFormat == GraphicsFormat.R8G8B8A8_UNorm);
            else if (webCamTexture.graphicsFormat == GraphicsFormat.B8G8R8A8_UNorm) {
                // This happens on iOS and OSX device webcams!
                _webcamControlShader.SetInt(_SwapBR, 0);
            }
            // else {
            //     throw new Exception("Unsupported graphics format from webcam: " + webCamTexture.graphicsFormat);
            // }
        }
        
        public void Poll() {
            UpdateProps();
            polling = true;
            webCamTexture.Play();
        }

        public void Pause() {
            polling = false;
            webCamTexture.Pause();
        }

        private void Update() {
            if (polling) {
                if(webCamTexture == null || !webCamTexture.isPlaying) {
                    Poll();
                }

                if (webCamTexture.didUpdateThisFrame && webCamTexture.width > 0 && webCamTexture.height > 0) {
                    foreach (var callback in callbacks) {
                        // Why create a new texture for each callback? because memory management - if one callback frees the
                        // texture I don't want it to affect another. 
                        // Ideally a reference counter should do the trick and allow for much efficient operation - can 
                        // look into that with a custom class to manage the resource instead of passing around Texture2D
                        // TODO: Reference Counter
                        _webcamControlShader.SetFloat(RotationAngle, webCamTexture.videoRotationAngle);
                        _webcamControlShader.SetInt("_HorizontalFlip", webCamTexture.videoVerticallyMirrored ? 1 : 0);
                        // Debug.Log($"Webcam rotation: {webCamTexture.videoRotationAngle}");
                        // Debug.Log($"Webcam resolution: {webCamTexture.width}x{webCamTexture.height}");
                        // TODO: Figure out a way to rotate the entire texture including the dimensions rather than just the UVs
                        var dest = new Texture2D(
                            webCamTexture.videoRotationAngle % 180 == 0 ? webCamTexture.width : webCamTexture.height, // webCamTexture.width, 
                            webCamTexture.videoRotationAngle % 180 == 0 ? webCamTexture.height : webCamTexture.width, // webCamTexture.height, 
                            TextureFormat.RGBA32,
                            false);
                        

                        RenderTexture tempRT = RenderTexture.GetTemporary(
                            webCamTexture.videoRotationAngle % 180 == 0 ? webCamTexture.width : webCamTexture.height, // webCamTexture.width, 
                            webCamTexture.videoRotationAngle % 180 == 0 ? webCamTexture.height : webCamTexture.width, // webCamTexture.height, 
                            0, GraphicsFormatUtility.GetGraphicsFormat(TextureFormat.RGBA32, true));
                        // _webcamControlShader.SetTexture("_MainTex", webCamTexture);
                        Graphics.Blit(webCamTexture, tempRT, _webcamControlShader);

                        ////////////////////////////////////////////////
                        ///// This does not seem to be working - seems like textures are left in GPU and all GPU
                        ///// Pipelines ( the preview afterwards) will handle it okay, but the pixels are not CPU
                        ///// readable and Mediapipe doesnt really play with that.
                        ////////////////////////////////////////////////
                        ////////////////===OLD COMMENT==////////////////
                        // The tempRT is required on Android and iOS since the webcam texture is not on the GPU then and the
                        // GL pipelines they have don't allow for a one line copy texture.
                        // Theoretically in the editor - i was able to get away with just CopyTexture but on the mobile
                        // devices it crashes since the webcamtexture is not on the GPU which CopyTexture requires.
                        ////////////////======CODE======////////////////
                        // Graphics.CopyTexture(tempRT, dest);
                        ////////////////////////////////////////////////

                        RenderTexture.active = tempRT;
                        dest.ReadPixels(new Rect(
                            0, 0, 
                            webCamTexture.videoRotationAngle % 180 == 0 ? webCamTexture.width : webCamTexture.height, // webCamTexture.width, 
                            webCamTexture.videoRotationAngle % 180 == 0 ? webCamTexture.height : webCamTexture.width // webCamTexture.height
                            ), 0, 0, false);
                        dest.Apply();
                        RenderTexture.active = null;

                        RenderTexture.ReleaseTemporary(tempRT);
                        callback.Value(dest);
                    }
                }
            }
            else {
                if (webCamTexture  && webCamTexture.isPlaying)
                    Pause();
            }
        }

        public void AddCallback(string name, Action<Texture2D> callback) {
            if (callbacks.ContainsKey(name)) callbacks.Remove(name);
            callbacks.Add(name, callback);
        }

        public void RemoveCallback(string name) {
            callbacks.Remove(name);
        }
    }
}