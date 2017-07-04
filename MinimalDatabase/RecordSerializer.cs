using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalDatabase
{
    public abstract class RecordSerializer<T> : IRecordSerializer
    {
        object IRecordSerializer.ReadEntry(BinaryReader reader)
        {
            return (object)ReadEntry(reader);
        }

        void IRecordSerializer.WriteEntry(object value, BinaryWriter writer)
        {
            WriteEntry((T)value, writer);
        }

        protected abstract T ReadEntry(BinaryReader reader);
        protected abstract void WriteEntry(T value, BinaryWriter writer);

        public uint BytesPerEntry { get; }
    }
}
