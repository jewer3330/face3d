using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARFace.Landmarks
{
    internal static class LandmarkManager
    {
        private static LandmarkType _activeLandmarkType = LandmarkType.MeituOffline;

        private static LandmarkType Fallback = LandmarkType.MTShowShow;

        public static LandmarkType ActiveLandmarkType
        {
            get
            {
#if UNITY_EDITOR
                if (!_activeLandmarkType.IsOnline())
                {
                    Debug.LogWarningFormat("Editor: Offline landmark not supported, fallback to: {0}.", Fallback);
                    return Fallback;
                }
#endif
                return _activeLandmarkType;
            }
			set
			{
				_activeLandmarkType = value;
			}
        }

        public static void GetLandmark(Texture tex, LandmarkType landmarkType,
            Action<Dictionary<string, Vector2>> callback, Action<float> uploadProgressCallback = null)
        {
            Debug.LogFormat("use landmark type = {0}", landmarkType);
            switch (landmarkType)
            {
                //case LandmarkType.MeituOffline:
                //    MeituSDK.ProcessLandmark(tex, callback);
                    //break;
#if UNITY_EDITOR
                case LandmarkType.ARKitRemote:
                    callback(ARKitRemoteLandmark.Landmarks);
                    break;
#endif
                case LandmarkType.MTShowShow:
                    LandmarkBase landmarkComp = UnityEngine.Object.FindObjectOfType<MTShowShowLandmark>();
                    if (!landmarkComp.isActiveAndEnabled)
                    {
                        landmarkComp.enabled = true;
                        landmarkComp.gameObject.SetActive(true);
                    }
                    landmarkComp.GetPhotoUVPosition(tex, callback, uploadProgressCallback);
                    break;

                default:
                    throw new NotSupportedException();
                    
                
            }
        }
    }
}