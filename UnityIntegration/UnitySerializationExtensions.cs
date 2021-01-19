//#define UNITY_STRUCTS_EXIST // uncomment this line if you use Unity structs in non-unity project

#if UNITY_2018_1_OR_NEWER
#define UNITY_STRUCTS_EXIST
#endif

#if UNITY_STRUCTS_EXIST

using UnityEngine;

namespace DaSerialization
{
    public static class UnitySerializationExtensions
    {
        public static Vector2 ReadVector2(this BinaryStreamReader reader)
        {
            Vector2 v;
            v.x = reader.ReadSingle();
            v.y = reader.ReadSingle();
            return v;
        }
        public static void WriteVector2(this BinaryStreamWriter writer, Vector2 v)
        {
            writer.WriteSingle(v.x);
            writer.WriteSingle(v.y);
        }

        public static Vector3 ReadVector3(this BinaryStreamReader reader)
        {
            Vector3 v;
            v.x = reader.ReadSingle();
            v.y = reader.ReadSingle();
            v.z = reader.ReadSingle();
            return v;
        }
        public static void WriteVector3(this BinaryStreamWriter writer, Vector3 v)
        {
            writer.WriteSingle(v.x);
            writer.WriteSingle(v.y);
            writer.WriteSingle(v.z);
        }

        public static Vector2Int ReadVector2Int(this BinaryStreamReader reader)
        {
            Vector2Int v = new Vector2Int();
            v.x = reader.ReadInt32();
            v.y = reader.ReadInt32();
            return v;
        }
        public static void WriteVector2Int(this BinaryStreamWriter writer, Vector2Int v)
        {
            writer.WriteInt32(v.x);
            writer.WriteInt32(v.y);
        }

        public static Vector3Int ReadVector3Int(this BinaryStreamReader reader)
        {
            Vector3Int v = new Vector3Int();
            v.x = reader.ReadInt32();
            v.y = reader.ReadInt32();
            v.z = reader.ReadInt32();
            return v;
        }
        public static void WriteVector3Int(this BinaryStreamWriter writer, Vector3Int v)
        {
            writer.WriteInt32(v.x);
            writer.WriteInt32(v.y);
            writer.WriteInt32(v.z);
        }

        public static Quaternion ReadQuaternion(this BinaryStreamReader reader)
        {
            // TODO: optimize
            Quaternion q;
            q.x = reader.ReadSingle();
            q.y = reader.ReadSingle();
            q.z = reader.ReadSingle();
            q.w = reader.ReadSingle();
            return q;
        }
        public static void WriteQuaternion(this BinaryStreamWriter writer, Quaternion q)
        {
            // TODO: optimize
            writer.WriteSingle(q.x);
            writer.WriteSingle(q.y);
            writer.WriteSingle(q.z);
            writer.WriteSingle(q.w);
        }
    }
}

#endif