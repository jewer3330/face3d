using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

#if UNITY_EDITOR
namespace ARFace.Landmarks
{
    public static class ARKitRemoteLandmark
    {
        public static Dictionary<string, Vector2> Landmarks { get; private set; }

        [UnityEditor.InitializeOnLoadMethod]
        static void Setup()
        {
            UnityARSessionNativeInterface.EditorLandmarkUpdateEvent += EditorLandmarkUpdated;
        }

        private static void EditorLandmarkUpdated(Dictionary<string, Vector2> landmarks)
        {
            // Debug.Log("LandmarkUpdated: " + string.Join(",", landmarks.Values));
            Landmarks = landmarks;
        }
    }
}
#endif
