using System;
using System.Collections.Generic;
using Mediapipe;
using UnityEngine;
using UnityEngine.Rendering;

namespace Camera {
    public interface ICamera {
        void Poll();
        void Pause();
        void AddCallback(string name, Action<Texture2D> callback);
        void RemoveCallback(string name);
    }

    public enum CameraSelector {
        FIRST_FRONT_CAMERA,
        FIRST_BACK_CAMERA
    }
}