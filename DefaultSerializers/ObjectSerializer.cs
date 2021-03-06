﻿using UnityEngine.Scripting;

namespace DaSerialization.Serialization
{
    // to make possible DeepCopy for non-generic calls (also for value types with boxing)
    // we introduce a fixed value for type 'object'
    // this means we can serialize 'object'-typed
    [TypeId(100011287, typeof(object), false)]
    [Preserve]
    public class ObjectSerializer_v1 : AFullSerializer<object, BinaryStream>
    {
        public override int Version => 1;

        public override void ReadDataToObject(ref object obj, BinaryStream stream, AContainer<BinaryStream> container)
        {
            if (obj == null)
                obj = new object();
            // no inner fields
        }

        public override void WriteObject(object obj, BinaryStream stream, AContainer<BinaryStream> container)
        {
            // nothing to write
        }
    }
}