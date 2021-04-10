using System;

namespace ARFace.Landmarks
{
    public enum LandmarkType
    {
        FacePP,
        Apple,
        FacePPOnline,
        Tencent,
        MTShowShow,
        MeituOffline,
        ARKitRemote,
    }

    public static class LandmarkTypeExtensions
    {
        public static bool IsOnline(this LandmarkType landmarkType)
        {
            switch (landmarkType)
            {
                case LandmarkType.FacePPOnline:
                case LandmarkType.Tencent:
                case LandmarkType.MTShowShow:
                    return true;
                case LandmarkType.FacePP:
                case LandmarkType.MeituOffline:
                case LandmarkType.Apple:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(landmarkType), landmarkType, null);
            }
        }

        public static bool IsResultUvSpace(this LandmarkType landmarkType)
        {
            switch (landmarkType)
            {
                case LandmarkType.FacePPOnline:
                case LandmarkType.Tencent:
                case LandmarkType.MTShowShow:
                case LandmarkType.FacePP:
                case LandmarkType.Apple:
                    return false;
                case LandmarkType.MeituOffline:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(landmarkType), landmarkType, null);
            }
        }
    }
}