using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using MinimalDatabase.Paging;

namespace MinimalDatabase
{
    public class DatabaseHeaderPage : Page
    {
        public uint Identifier { get; set; }
        public uint Version { get; set; }

        public override void ReadFromPersistence(BinaryReader reader)
        {
            Identifier = reader.ReadUInt32();
            Version = reader.ReadUInt32();
        }

        public override void WriteToPersistence(BinaryWriter writer)
        {
            writer.Write(Identifier);
            writer.Write(Version);
        }
    }
}
