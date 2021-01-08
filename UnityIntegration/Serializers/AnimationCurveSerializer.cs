#if UNITY_2018_1_OR_NEWER

using DaSerialization;
using UnityEngine;

namespace Common.Serialization
{
    [TypeId(-541940, typeof(AnimationCurve))]
    public class AnimationCurveSerializer_v1 : AFullSerializer<AnimationCurve>
    {
        public override int Version => 1;

        public override void ReadDataToObject(ref AnimationCurve ac, BinaryStreamReader reader)
        {
            if (ac == null)
                ac = new AnimationCurve();
            int length = reader.ReadUInt16();
            var keys = ac.length == length ? ac.keys : new Keyframe[length];
            for (int i = 0; i < length; i++)
                keys[i] = new Keyframe(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            ac.keys = keys;
            ac.preWrapMode = (WrapMode)reader.ReadByte();
            ac.postWrapMode = (WrapMode)reader.ReadByte();
        }

        public override void WriteObject(AnimationCurve ac, BinaryStream stream)
        {
            var writer = stream.GetWriter();
            int len = ac.length;
            writer.Write(len.ClampToUInt16());
            for (int i = 0; i < len; i++)
            {
                var kf = ac[i];
                writer.Write(kf.time);
                writer.Write(kf.value);
                writer.Write(kf.inTangent);
                writer.Write(kf.outTangent);
            }
            writer.Write((byte)ac.preWrapMode);
            writer.Write((byte)ac.postWrapMode);
        }
    }
}

#endif