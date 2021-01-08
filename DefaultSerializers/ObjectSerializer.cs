namespace DaSerialization.Serialization
{
    // to make possible DeepCopy for non-generic calls (also for value types with boxing)
    // we introduce a fixed value for type 'object'
    // this means we can serialize 'object'-typed
    [TypeId(100011287, typeof(object), false)]
    public class ObjectSerializer_v1 : AFullSerializer<object>
    {
        public override int Version => 1;

        public override void ReadDataToObject(ref object obj, BinaryStreamReader reader)
        {
            if (obj == null)
                obj = new object();
            // no inner fields
        }

        public override void WriteObject(object obj, BinaryStream stream)
        {
            // nothing to write
        }
    }
}