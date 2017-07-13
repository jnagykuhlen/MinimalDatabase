using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MinimalDatabase.Paging
{
    public class PagingHeaderPage : Page
    {
        public uint PageSize { get; set; }
        public uint NextFreePageId { get; set; }
        public uint NextHeaderPageId { get; set; }

        public override void ReadFromPersistence(BinaryReader reader)
        {
            PageSize = reader.ReadUInt32();
            NextFreePageId = reader.ReadUInt32();
            NextHeaderPageId = reader.ReadUInt32();
        }

        public override void WriteToPersistence(BinaryWriter writer)
        {
            writer.Write(PageSize);
            writer.Write(NextFreePageId);
            writer.Write(NextHeaderPageId);
        }
    }
}
