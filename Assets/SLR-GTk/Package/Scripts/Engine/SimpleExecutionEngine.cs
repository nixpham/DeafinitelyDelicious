using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Model;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using RunningMode = Mediapipe.Tasks.Vision.Core.RunningMode;

namespace Engine {
    public class SimpleExecutionEngine : MonoBehaviour
    {
        [SerializeField] private Preview.UnityMpHandPreviewPainter screen;
        [SerializeField] private Camera.StreamCamera inputCamera;

        public SLRTfLiteModel<string> recognizer;
        public Buffer<HandLandmarkerResult> buffer;
        public MediapipeHandModelManager posePredictor;

        [FormerlySerializedAs("_modelFile")] [SerializeField] private TextAsset modelFile;
        [FormerlySerializedAs("_mappingFile")] [SerializeField] private TextAsset mappingFile;
        [FormerlySerializedAs("_mediapipeGraph")] [SerializeField] private TextAsset mediapipeGraph;
        [FormerlySerializedAs("_isInterpolating")] [SerializeField] private bool isInterpolating;

        private float lastTime = -1;
        
        private static class Config
        {
            public static readonly int NumInputFrames = 60;
            public static readonly int NumInputPoints = 21;
        }

        void Start()
        {
            buffer = new Buffer<HandLandmarkerResult>();
            string[] mapping = mappingFile.text.Split("\n");

            for (int i = 0; i < mapping.Length; i++)
            {
                mapping[i] = mapping[i].Trim().ToLower();
            }


            recognizer = new SLRTfLiteModel<string>(modelFile, new List<string>(mapping));
            posePredictor = new MediapipeHandModelManager(mediapipeGraph.bytes, RunningMode.LIVE_STREAM);

            posePredictor.AddCallback("buffer", result =>
            {                        
                if (result.Result.handLandmarks == null || result.Result.handLandmarks.Count <= 0 ||
                    result.Result.handLandmarks[0].landmarks.Count <= 0) { }
                else {
                    buffer.AddElement(result.Result);
                }
                if (screen) screen.UpdateLandmarks(result.Result);
                if (result.Image != null)
                    if (screen) screen.UpdateImage(result.Image);
                    else
                        Debug.Log("Got null screen");
            });
            buffer.AddCallback("trigger", bufferedResults =>
            {
                List<float> inputArray = new List<float>();

                if (bufferedResults.Count <= 0) return;
                
                //Debug.Log("Starting Buffer trigger: Interpolating=" + isInterpolating);
                
                if (isInterpolating && bufferedResults.Count < Config.NumInputFrames && bufferedResults.Count > 0)
                {
                    //Debug.Log("Underfill");
                    foreach (var landmark in bufferedResults)
                    {
                        for (int j = 0; j < Config.NumInputPoints; j++)
                        {
                            inputArray.Add(1- landmark.handLandmarks[0].landmarks[j].x);
                            inputArray.Add(1-landmark.handLandmarks[0].landmarks[j].y);
                        }
                    }

                    var midpoint = bufferedResults[bufferedResults.Count / 2];

                    for (int i = 0; i < Config.NumInputFrames - bufferedResults.Count; i++)
                    {
                        for (int j = 0; j < Config.NumInputPoints; j++)
                        {
                            inputArray.Add(1-midpoint.handLandmarks[0].landmarks[j].x);
                            inputArray.Add(1-midpoint.handLandmarks[0].landmarks[j].y);
                        }
                    }
                }
                else if (bufferedResults.Count >= 60)
                {
                    //Debug.Log("Overfill");
                    var lastFrames = bufferedResults.GetRange(bufferedResults.Count - Config.NumInputFrames, Config.NumInputFrames);
                    //Debug.Log("Buffer Gets " + lastFrames.Count);
                    foreach (var landmark in lastFrames)
                    {
                        for (int j = 0; j < Config.NumInputPoints; j++)
                        {

                            inputArray.Add(1-landmark.handLandmarks[0].landmarks[j].x);
                            inputArray.Add(1-landmark.handLandmarks[0].landmarks[j].y);
                        }
                    }
                }
                
                
                
                //Debug.Log("Input array got " + inputArray.Count);

                if (inputArray.Count > 0)
                {
                    recognizer.RunModel(inputArray.ToArray());
                }
                buffer.Clear();
            });
            recognizer.AddCallback("default", (translation) => {
               // Debug.Log(translation);
            });
            if (inputCamera) inputCamera.AddCallback("default", image => {
                    posePredictor.Single(image, (int)(Time.realtimeSinceStartup * 1000));
            });
        
            if (screen) screen.Show();

            buffer.trigger = new NoTrigger<HandLandmarkerResult>();
            Poll();
        }

        public void Poll() {
            inputCamera.Poll();
        }

        public void Pause() {
            inputCamera.Pause();
            buffer.TriggerCallbacks();
        }

        public void Toggle() {
            if (screen.Visible) screen.Hide();
            else screen.Show();
        }
        static List<string> MakeLowercase(List<string> words)
        {
            for (int i = 0; i < words.Count; i++)
            {
                words[i] = words[i].ToLower();
            }
            return words;
        }
    }
}
