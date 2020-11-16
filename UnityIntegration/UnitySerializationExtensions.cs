//#define UNITY_STRUCTS_EXIST // uncomment this line if you use Unity structs in non-unity project

#if UNITY_2018_1_OR_NEWER
#define UNITY_STRUCTS_EXIST
#endif

#if UNITY_STRUCTS_EXIST

using System.IO;
using UnityEngine;

namespace DaSerialization
{
    public static class UnitySerializationExtensions
    {
        public static Vector2 ReadVector2(this BinaryReader reader)
        {
            Vector2 v;
            v.x = reader.ReadSingle();
            v.y = reader.ReadSingle();
            return v;
        }
        public static void WriteVector2(this BinaryWriter writer, Vector2 v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
        }

        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            Vector3 v;
            v.x = reader.ReadSingle();
            v.y = reader.ReadSingle();
            v.z = reader.ReadSingle();
            return v;
        }
        public static void WriteVector3(this BinaryWriter writer, Vector3 v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
        }

        public static Vector2Int ReadVector2Int(this BinaryReader reader)
        {
            Vector2Int v = new Vector2Int();
            v.x = reader.ReadInt32();
            v.y = reader.ReadInt32();
            return v;
        }
        public static void WriteVector2Int(this BinaryWriter writer, Vector2Int v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
        }

        public static Vector3Int ReadVector3Int(this BinaryReader reader)
        {
            Vector3Int v = new Vector3Int();
            v.x = reader.ReadInt32();
            v.y = reader.ReadInt32();
            v.z = reader.ReadInt32();
            return v;
        }
        public static void WriteVector3Int(this BinaryWriter writer, Vector3Int v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
        }

        public static Quaternion ReadQuaternion(this BinaryReader reader)
        {
            // TODO: optimize
            Quaternion q;
            q.x = reader.ReadSingle();
            q.y = reader.ReadSingle();
            q.z = reader.ReadSingle();
            q.w = reader.ReadSingle();
            return q;
        }
        public static void WriteQuaternion(this BinaryWriter writer, Quaternion q)
        {
            // TODO: optimize
            writer.Write(q.x);
            writer.Write(q.y);
            writer.Write(q.z);
            writer.Write(q.w);
        }
    }
}

#endif