using System;
using ProtoBuf;
using ProtoBuf.Meta; 

namespace ReefEditor.ContentLoading {
    public class SNTypeModel : TypeModel {
        /*
        protected override int GetKeyImpl(Type type)
        {
            return GetKey(ref type);
        }

        protected override void Serialize(int key, object value, ProtoWriter dest)
        {
            ProtoWriter.WriteObject(value, key, dest);
        }

        protected override object Deserialize(int key, object value, ProtoReader source)
        {
            return ProtoReader.ReadObject(value, key, source);
        }
        */
    }
}
