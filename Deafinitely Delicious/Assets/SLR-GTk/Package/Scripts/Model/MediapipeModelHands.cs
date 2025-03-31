using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mediapipe;
using Mediapipe.Tasks.Vision.HandLandmarker;
using UnityEngine;
using UnityEngine.Rendering;
using RunningMode = Mediapipe.Tasks.Vision.Core.RunningMode;

namespace Model {
    public class ImageMPResultWrapper<T> {
        public T Result { get; }
        public Texture2D Image { get; }

        public ImageMPResultWrapper(T result, Texture2D image) {
            Result = result;
            Image = image;
        }
    }

    public class MediapipeHandModelManager {
        private readonly HandLandmarker graph;

        private readonly Dictionary<string, Action<ImageMPResultWrapper<HandLandmarkerResult>>> callbacks = new();
        private readonly ConcurrentDictionary<long, Texture2D> outputInputLookup = new();
        private readonly RunningMode runningMode;

        private static class Config {
            public static readonly float HAND_DETECTION_CONFIDENCE = 0.5f;
            public static readonly float HAND_TRACKING_CONFIDENCE = 0.5f;
            public static readonly float HAND_PRESENCE_CONFIDENCE = 0.5f;
            public static int NUM_HANDS = 1;
        }

        public MediapipeHandModelManager(byte[] modelAssetBuffer, Mediapipe.Tasks.Vision.Core.RunningMode runningMode) {
            this.runningMode = runningMode;
            if (runningMode != Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM)
                graph = HandLandmarker.CreateFromOptions(new HandLandmarkerOptions(
                    new Mediapipe.Tasks.Core.BaseOptions(
                        modelAssetBuffer: modelAssetBuffer
                    ),
                    minHandDetectionConfidence: Config.HAND_DETECTION_CONFIDENCE,
                    minTrackingConfidence: Config.HAND_TRACKING_CONFIDENCE,
                    minHandPresenceConfidence: Config.HAND_PRESENCE_CONFIDENCE,
                    numHands: Config.NUM_HANDS,
                    runningMode: runningMode
                ));
            else
                graph = HandLandmarker.CreateFromOptions(new HandLandmarkerOptions(
                    new Mediapipe.Tasks.Core.BaseOptions(
                        modelAssetBuffer: modelAssetBuffer
                    ),
                    minHandDetectionConfidence: Config.HAND_DETECTION_CONFIDENCE,
                    minTrackingConfidence: Config.HAND_TRACKING_CONFIDENCE,
                    minHandPresenceConfidence: Config.HAND_PRESENCE_CONFIDENCE,
                    numHands: Config.NUM_HANDS,
                    runningMode: runningMode,
                    resultCallback: (i, _, timestampMs) => {
                        if (!outputInputLookup.ContainsKey(timestampMs)) return;
                        foreach (var cb in callbacks.Values) {
                            cb(new ImageMPResultWrapper<HandLandmarkerResult>(
                                i,
                                outputInputLookup.GetValueOrDefault(timestampMs)
                                // TODO: maybe pass in transformed image from MP graph?
                            ));
                        }

                        outputInputLookup.Remove(timestampMs, out var _);
                        foreach (var timestamp in outputInputLookup.Keys) {
                            if (timestamp < timestampMs) {
                                outputInputLookup.Remove(timestamp, out var texture);
                                CustomTextureManager.ScheduleDeletion(texture);
                            }
                        }
                    }
                ));
        }

        private int imageCounter = 0; // Counter for image filenames

        // public void Single(Texture2D image, long timestampMs) {
        //     AsyncGPUReadback.WaitAllRequests();
        //     AsyncGPUReadback.Request(image, 0, TextureFormat.RGBA32, request => {
        //         if (request.hasError)
        //         {
        //             Debug.LogError("Readback failed!");
        //         }
        //         else
        //         {
        //             // image.SetPixels32(request.GetData<Color32>().ToArray());
        //             // image.Apply();
        //             image.LoadRawTextureData(request.GetData<byte>());
        //             image.Apply();
        //             _Single(image, (int)(Time.realtimeSinceStartup * 1000));
        //         }
        //     });
        // }

        public void Single(Texture2D image, long timestamp/*, ImageProcessingOptions? imageProcessingOptions = null*/) {
            Image img = new Image(image.format.ToImageFormat(), image);
            // Image img = new Image(ImageFormat.Types.Format.Srgba, image.width, image.height, ImageFormat.Types.Format.Srgba.NumberOfChannels() * image.width, image.GetRawTextureData<byte>());
            // Image img = new Image(image.GetNativeTexturePtr(), false);
            // Color32[] pixels = new Color32[image.width * image.height];
            // img.TryReadPixelData(pixels);
            // var texture = new Texture2D(image.width, image.height);
            // texture.SetPixels32(pixels);
            // texture.Apply();
            //
            // File.WriteAllBytes(Application.dataPath + "/IMGDUMP/" + FileCounter + ".png", texture.EncodeToPNG());
            // FileCounter++;
            // Debug.Log("Image MP: " + img.Width() + "x"  + img.Height() + ", " + img.ImageFormat());
            switch (runningMode) {
                case Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM:
                    outputInputLookup[timestamp] = image;
                    graph.DetectAsync(img, timestamp
                        // , imageProcessingOptions
                        );
                    break;
                case Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE:
                    var result = graph.Detect(img
                        // , imageProcessingOptions
                        );
                    foreach (var cb in callbacks.Values) {
                        cb(new ImageMPResultWrapper<HandLandmarkerResult>(result, image));
                    }

                    break;
                case Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO:
                    var videoResult = graph.DetectForVideo(img, timestamp
                        // , imageProcessingOptions
                        );
                    foreach (var cb in callbacks.Values) {
                        cb(new ImageMPResultWrapper<HandLandmarkerResult>(videoResult, image));
                    }

                    break;
            }
        }

        public void AddCallback(string name, Action<ImageMPResultWrapper<HandLandmarkerResult>> callback) {
            callbacks[name] = callback;
        }
        
        public void RemoveCallback(string name) {
            callbacks.Remove(name);
        }
    }
}


