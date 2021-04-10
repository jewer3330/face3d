using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "FaceConfig", menuName = "Face/FaceConfig", order = 5)]
public class FaceConfig : ScriptableObject
{
    public Mesh remapMesh;
    public Vector2[] liuhongTop;
    public Material liuhongMat;
    public Matrix4x4 faceToHead;
    public Transform allHead;
    public Texture2D vertexMask;
    public Vector3 hsl = new Vector3(0.4963489f, 0.561524f, 0.4469939f);
    public float gamma = 0.40f;
    public Material threeToOne;
    //右眼皮顶点
    public static readonly short[] DefaultFaceRightEyeBorder = { 1089, 1090, 1088, 1091, 1087, 1092, 1086, 1093, 1085, 1094, 1108, 1095, 1107, 1096, 1106, 1097, 1105, 1098, 1104, 1099, 1103, 1100, 1102, 1101 };
    //左眼皮顶点
    public static readonly short[] DefaultFaceLeftEyeBorder = { 1081, 1080, 1082, 1079, 1083, 1078, 1084, 1077, 1061, 1076, 1062, 1075, 1063, 1074, 1064, 1073, 1065, 1072, 1066, 1071, 1067, 1070, 1068, 1069 };
    //嘴巴顶点
    public static readonly short[] DefaultFaceMeshMouthBorder = { 684, 823, 834, 685, 740, 686, 683, 687, 682, 688, 710, 689, 725, 690, 709, 691, 700, 24, 25, 256, 265, 255, 274, 254, 290, 253, 275, 252, 247, 251, 248, 250, 305, 393, 404, 249 };

}