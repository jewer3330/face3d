using System;
using JetBrains.Annotations;
using UnityEngine;

namespace ARFace
{
    [PublicAPI]
    public static class MathUtils
    {
        public static float Remap(float fromA, float fromB, float toA, float toB, float value)
        {
            float t = Mathf.InverseLerp(fromA, fromB, value);
            return Mathf.LerpUnclamped(toA, toB, t);
        }

        /// <summary>Remap [0,0.5],[0.5,1] to [0,mid],[mid,max] </summary>
        public static float Remap01Linear(float value, float midValue, float maxValue)
        {
            if (value < 0.5f)
                return Remap(0, 0.5f, 0, midValue, value);
            else
                return Remap(0.5f, 1, midValue, maxValue, value);
        }

        /// <summary>InverseRemap [0,0.5],[0.5,1] to [0,mid],[mid,max] </summary>
        public static float InverseRemap01Linear(float value, float midValue, float maxValue)
        {
            if (value < midValue)
                return Remap(0, midValue, 0, 0.5f, value);
            else
                return Remap(midValue, maxValue, 0.5f, 1, value);
        }

        /// <summary>Remap [0,0.5],[0.5,1] to [0,mid],[mid,max] </summary>
        public static float Remap01Log(float value, float midValue, float maxValue)
        {
            // y = v^Log[x/m]/Log[2] x;
            float p = Mathf.Log(maxValue / midValue, 2);
            return Mathf.Pow(value, p) * maxValue;
        }

        /// <summary>InverseRemap [0,0.5],[0.5,1] to [0,mid],[mid,max] </summary>
        public static float InverseRemap01Log(float value, float midValue, float maxValue)
        {
            // (y/x)^(Log[2]/Log[x/m])
            return Mathf.Pow(value / maxValue, Mathf.Log(2, maxValue / midValue));
        }

        public static float CubeRoot(float d)
        {
            return (float)(Math.Pow(Math.Abs(d), 1.0 / 3) * Math.Sign(d));
        }

        public static float NormalizeAngle2Pi(float rad)
        {
            if (float.IsInfinity(rad) || float.IsNaN(rad))
                throw new ArgumentException();

            const float _2Pi = Mathf.PI * 2;
            while (rad > _2Pi)
            {
                rad -= _2Pi;
            }

            while (rad < 0)
            {
                rad += _2Pi;
            }

            return rad;
        }
    }
}
