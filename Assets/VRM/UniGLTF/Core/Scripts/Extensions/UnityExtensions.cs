﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UniGLTF
{
    public struct PosRot
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public static PosRot FromGlobalTransform(Transform t)
        {
            return new PosRot
            {
                Position = t.position,
                Rotation = t.rotation,
            };
        }
    }

    public class BlendShape
    {
        public string Name;

        public BlendShape(string name)
        {
            Name = name;
        }

        public List<Vector3> Positions = new List<Vector3>();
        public List<Vector3> Normals = new List<Vector3>();
        public List<Vector3> Tangents = new List<Vector3>();
    }

    public static class UnityExtensions
    {
        public static Vector4 ReverseZ(this Vector4 v)
        {
            return new Vector4(v.x, v.y, -v.z, v.w);
        }

        public static Vector3 ReverseZ(this Vector3 v)
        {
            return new Vector3(v.x, v.y, -v.z);
        }

        public static Vector2 ReverseY(this Vector2 v)
        {
            return new Vector2(v.x, -v.y);
        }

        public static Quaternion ReverseZ(this Quaternion q)
        {
            float angle;
            Vector3 axis;
            q.ToAngleAxis(out angle, out axis);
            return Quaternion.AngleAxis(-angle, ReverseZ(axis));
        }

        public static Matrix4x4 Matrix4x4FromColumns(Vector4 c0, Vector4 c1, Vector4 c2, Vector4 c3)
        {
#if UNITY_2017_1_OR_NEWER
            return new Matrix4x4(c0, c1, c2, c3);
#else
            var m = default(Matrix4x4);
            m.SetColumn(0, c0);
            m.SetColumn(1, c1);
            m.SetColumn(2, c2);
            m.SetColumn(3, c3);
            return m;
#endif
        }

        public static Matrix4x4 Matrix4x4FromRotation(Quaternion q)
        {
#if UNITY_2017_1_OR_NEWER
            return Matrix4x4.Rotate(q);
#else
            var m = default(Matrix4x4);
            m.SetTRS(Vector3.zero, q, Vector3.one);
            return m;
#endif
        }

        public static Matrix4x4 ReverseZ(this Matrix4x4 m)
        {
#if false
//#if UNITY_2017_1_OR_NEWER
            m.SetTRS(m.GetColumn(3).ReverseZ(), m.rotation.ReverseZ(), Vector3.one);
#else
            // ToDo
            m.SetTRS(m.ExtractPosition().ReverseZ(), m.ExtractRotation().ReverseZ(), Vector3.one);
#endif
            return m;
        }

        public static Quaternion ExtractRotation(this Matrix4x4 matrix)
        {
            Vector3 forward;
            forward.x = matrix.m02;
            forward.y = matrix.m12;
            forward.z = matrix.m22;

            Vector3 upwards;
            upwards.x = matrix.m01;
            upwards.y = matrix.m11;
            upwards.z = matrix.m21;

            return Quaternion.LookRotation(forward, upwards);
        }

        public static Vector3 ExtractPosition(this Matrix4x4 matrix)
        {
            Vector3 position;
            position.x = matrix.m03;
            position.y = matrix.m13;
            position.z = matrix.m23;
            return position;
        }

        public static Vector3 ExtractScale(this Matrix4x4 matrix)
        {
            Vector3 scale;
            scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
            scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
            scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
            return scale;
        }

        public static string RelativePathFrom(this Transform self, Transform root)
        {
            var path = new List<String>();
            for (var current = self; current != null; current = current.parent)
            {
                if (current == root)
                {
                    return String.Join("/", path.ToArray());
                }

                path.Insert(0, current.name);
            }

            throw new Exception("no RelativePath");
        }

        public static Transform GetChildByName(this Transform self, string childName)
        {
            foreach (Transform child in self)
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            throw new KeyNotFoundException();
        }

        public static Transform GetFromPath(this Transform self, string path)
        {
            var current = self;

            var splited = path.Split('/');

            foreach (var childName in splited)
            {
                current = current.GetChildByName(childName);
            }

            return current;
        }

        public static IEnumerable<Transform> GetChildren(this Transform self)
        {
            foreach (Transform child in self)
            {
                yield return child;
            }
        }

        public static IEnumerable<Transform> Traverse(this Transform t)
        {
            yield return t;
            foreach (Transform x in t)
            {
                foreach (Transform y in x.Traverse())
                {
                    yield return y;
                }
            }
        }

        public static Transform FindDescenedant(this Transform t, string name)
        {
            return t.Traverse().First(x => x.name == name);
        }

        public static IEnumerable<Transform> Ancestors(this Transform t)
        {
            yield return t;
            if (t.parent != null)
            {
                foreach (Transform x in t.parent.Ancestors())
                {
                    yield return x;
                }
            }
        }

        public static float[] ToArray(this Quaternion q)
        {
            return new float[] { q.x, q.y, q.z, q.w };
        }

        public static float[] ToArray(this Vector3 v)
        {
            return new float[] { v.x, v.y, v.z };
        }

        public static float[] ToArray(this Vector4 v)
        {
            return new float[] { v.x, v.y, v.z, v.w };
        }

        public static float[] ToArray(this Color c)
        {
            return new float[] { c.r, c.g, c.b, c.a };
        }

        public static void ReverseZ(this Transform root)
        {
            var globalMap = root.Traverse().ToDictionary(x => x, x => PosRot.FromGlobalTransform(x));

            foreach (var x in root.Traverse())
            {
                x.position = globalMap[x].Position.ReverseZ();
                x.rotation = globalMap[x].Rotation.ReverseZ();
            }
        }

        public static Mesh GetSharedMesh(this Transform t)
        {
            var meshFilter = t.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                return meshFilter.sharedMesh;
            }

            var skinnedMeshRenderer = t.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                return skinnedMeshRenderer.sharedMesh;
            }

            return null;
        }

        public static Material[] GetSharedMaterials(this Transform t)
        {
            var renderer = t.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.sharedMaterials;
            }

            return new Material[] { };
        }

        public static IEnumerable<Texture2D> GetTextures(this Material m)
        {
#if UNITY_EDITOR
            for (int i = 0; i < ShaderUtil.GetPropertyCount(m.shader); ++i)
            {
                if(ShaderUtil.GetPropertyType(m.shader, i)==ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    var texture = m.GetTexture(ShaderUtil.GetPropertyName(m.shader, i)) as Texture2D;
                    if (texture != null)
                    {
                        yield return texture;
                    }
                }
            }

#else
            var texture = m.mainTexture as Texture2D;
            if (texture != null)
            {
                yield return texture;
            }
#endif
        }

        public static bool Has<T>(this Transform transform, T t) where T : Component
        {
            return transform.GetComponent<T>() == t;
        }
    }
}