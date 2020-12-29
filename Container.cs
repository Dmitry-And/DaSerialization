using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace DaSerialization
{
    public enum Metadata
    {
        ObjectID,
        TypeID, // 0 if object is null
        Version, // 0 if object is null
        CollectionSize,
    }

    public struct SerializedObjectInfo
    {
        public int ObjectId;
        public int TypeId;
        public int LocalVersion;
        public uint Length;
        public long Position;

        public bool Fits(int objectId, int typeId) { return TypeId == typeId & ObjectId == objectId; }
        public bool Fits(int objectId, int typeId, int version) { return TypeId == typeId & ObjectId == objectId & LocalVersion == version; }

        public override string ToString()
            => $"Type {TypeId} Obj {ObjectId} v{LocalVersion} Pos {Position}-{Position + Length}";
    }
}