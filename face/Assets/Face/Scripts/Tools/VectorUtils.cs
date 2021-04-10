using System;
using System.Collections.Generic;
using System.Linq;
using ARFace;
using UnityEngine;
using UnityEngine.Assertions;

// ReSharper disable InconsistentNaming


public static class VectorUtils
{
    public static Vector3 Divide(Vector3 lhs, Vector3 rhs)
    {
        return new Vector3(
            lhs.x / rhs.x,
            lhs.y / rhs.y,
            lhs.z / rhs.z);
    }

    public static Vector3 Inverse(this Vector3 @this)
    {
        return new Vector3(
            1 / @this.x,
            1 / @this.y,
            1 / @this.z);
    }
    
    public static Vector3 Abs(this Vector3 @this)
    {
        return new Vector3(
            Mathf.Abs(@this.x),
            Mathf.Abs(@this.y),
            Mathf.Abs(@this.z));
    }

    public static bool IsUniform(this Vector3 @this)
    {
        return @this.x == @this.y && @this.x == @this.z;
    }

    public static bool IsUniformApproximately(this Vector3 @this)
    {
        return Mathf.Approximately(@this.x, @this.y) &&
               Mathf.Approximately(@this.x, @this.z);
    }

    public static void Deconstruct(this Vector3 v, out float x, out float y, out float z)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public static bool Approximately(this Vector4 lhs, Vector4 rhs)
    {
        return
            Mathf.Approximately(lhs.x, rhs.x) &&
            Mathf.Approximately(lhs.y, rhs.y) &&
            Mathf.Approximately(lhs.z, rhs.z) &&
            Mathf.Approximately(lhs.w, rhs.w);
    }
    public static bool Approximately(this Matrix4x4 lhs, Matrix4x4 rhs)
    {
        return
            Mathf.Approximately(lhs.m00, rhs.m00) &&
            Mathf.Approximately(lhs.m01, rhs.m01) &&
            Mathf.Approximately(lhs.m02, rhs.m02) &&
            Mathf.Approximately(lhs.m03, rhs.m03) &&
            Mathf.Approximately(lhs.m10, rhs.m10) &&
            Mathf.Approximately(lhs.m11, rhs.m11) &&
            Mathf.Approximately(lhs.m12, rhs.m12) &&
            Mathf.Approximately(lhs.m13, rhs.m13) &&
            Mathf.Approximately(lhs.m20, rhs.m20) &&
            Mathf.Approximately(lhs.m21, rhs.m21) &&
            Mathf.Approximately(lhs.m22, rhs.m22) &&
            Mathf.Approximately(lhs.m23, rhs.m23) &&
            Mathf.Approximately(lhs.m30, rhs.m30) &&
            Mathf.Approximately(lhs.m31, rhs.m31) &&
            Mathf.Approximately(lhs.m32, rhs.m32) &&
            Mathf.Approximately(lhs.m33, rhs.m33);
    }



    public static bool IsOrthogonal(this Matrix4x4 @this)
    {
        return Approximately(@this * @this.transpose, Matrix4x4.identity);
    }

    public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        m.m03 = m.m13 = m.m23 = 0;
        Assert.AreEqual(m.m30, 0);
        Assert.AreEqual(m.m31, 0);
        Assert.AreEqual(m.m32, 0);
        Assert.AreEqual(m.m33, 1);
        var Q2 = MathUtils.CubeRoot(m.determinant);

        m = m * Matrix4x4.Scale(Vector3.one / Q2);
        // Assert.IsTrue(m.IsOrthogonal(), (m * m.transpose).ToString());
        return m.rotation;
    }

    public static string ToStringRaw(this Vector2 @this)
    {
        return string.Format("({0}, {1})", @this.x, @this.y);
    }
    public static string ToStringRaw(this Vector3 @this)
    {
        return string.Format("({0}, {1}, {2})", @this.x, @this.y, @this.z);
    }
    public static string ToStringRaw(this Quaternion @this)
    {
        return string.Format("({0}, {1}, {2}, {3})", @this.x, @this.y, @this.z, @this.w);
    }

    public static Vector2 yz(this Vector3 @this)
    {
        return new Vector2(@this.y, @this.z);
    }

    public static Vector2 xz(this Vector3 @this)
    {
        return new Vector2(@this.x, @this.z);
    }

    public static Rect CalcBounds(this IList<Vector2> list)
    {
        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        foreach (var v in list)
        {
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    public static Vector2 Average(this IEnumerable<Vector2> source)
    {
        if (source == null)
            throw new ArgumentNullException();
        using (var enumerator = source.GetEnumerator())
        {
            if (!enumerator.MoveNext())
                throw new ArgumentException("NoElements");
            var current = enumerator.Current;
            long num = 1;
            while (enumerator.MoveNext())
            {
                current += enumerator.Current;
                checked { ++num; }
            }
            return current / num;
        }
    }

    public static Vector3 Average(this IEnumerable<Vector3> source)
    {
        if (source == null)
            throw new ArgumentNullException();
        using (var enumerator = source.GetEnumerator())
        {
            if (!enumerator.MoveNext())
                throw new ArgumentException("NoElements");
            var current = enumerator.Current;
            long num = 1;
            while (enumerator.MoveNext())
            {
                current += enumerator.Current;
                checked { ++num; }
            }
            return current / num;
        }
    }

    public static Vector2 Rotate(this Vector2 vector, float angle)
    {
        float theta = Mathf.Atan2(vector.y, vector.x) + angle * Mathf.Deg2Rad;
        return vector.magnitude * new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
    }

    public static float Area(this Rect rect)
    {
        return rect.width * rect.height;
    }

    public static float ToAngle(this Vector2 @this)
    {
        return Mathf.Atan2(@this.y, @this.x);
    }

    public static Vector2 HalfDirection(Vector2 fromDir, Vector2 toDir, bool ccw)
    {
        if(fromDir == Vector2.zero)
            throw new ArgumentException();
        if(toDir == Vector2.zero)
            throw new ArgumentException();
        
        if (ccw)
        {
            var angle = Mathf.Rad2Deg * MathUtils.NormalizeAngle2Pi(toDir.ToAngle() - fromDir.ToAngle()); 
            var halfDir = fromDir.normalized.Rotate(angle / 2);
            return halfDir;
        }
        else
        {
            return HalfDirection(toDir, fromDir, true);
        }
    }

    public static IEnumerable<Vector2> Rotate(this IEnumerable<Vector2> @this, float angle)
    {
        return @this.Select(v => v.Rotate(angle));
    }

    public static IEnumerable<Vector2> UvRotate(this IEnumerable<Vector2> @this, float angle)
    {
        return @this.Select(v =>
        {
            var offset = new Vector2(0.5f, 0.5f);
            return (v - offset).Rotate(angle) + offset;
        });
    }
}
