using System.Collections.Concurrent;
using UnityEngine;

namespace System {
    public class CustomTextureManager : MonoBehaviour {
        private static readonly ConcurrentQueue<Texture> TextureBin = new();
        public static void ScheduleDeletion(Texture toDelete) {
            TextureBin.Enqueue(toDelete);
        }

        public static void DeleteTextures() {
            while (TextureBin.TryDequeue(out var texture)) {
                texture.hideFlags = HideFlags.None;
                DestroyImmediate(texture);
            }
        }

        public static void DeleteNow(Texture toDelete) {
            DestroyImmediate(toDelete);
        }
    }
}