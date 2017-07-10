﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalDatabase.Internal
{
    public class StorageHeaderPage : Page
    {
        public const uint FixedHeaderLength = sizeof(uint) * 4;

        public uint NextHeaderPageId { get; set; }
        public uint NumberOfDataPages { get; set; }
        public uint TotalNumberOfDataPages { get; set; }
        public uint TotalNumberOfUnusedBytes { get; set; }
        public uint[] DataPageIds { get; set; }

        public override void ReadFromPersistence(BinaryReader reader)
        {
            NextHeaderPageId = reader.ReadUInt32();
            NumberOfDataPages = reader.ReadUInt32();
            TotalNumberOfDataPages = reader.ReadUInt32();
            TotalNumberOfUnusedBytes = reader.ReadUInt32();

            DataPageIds = new uint[NumberOfDataPages];
            for (int i = 0; i < NumberOfDataPages; ++i)
                DataPageIds[i] = reader.ReadUInt32();
        }

        public override void WriteToPersistence(BinaryWriter writer)
        {
            writer.Write(NextHeaderPageId);
            writer.Write(NumberOfDataPages);
            writer.Write(TotalNumberOfDataPages);
            writer.Write(TotalNumberOfUnusedBytes);

            for (int i = 0; i < NumberOfDataPages; ++i)
                writer.Write(DataPageIds[i]);
        }
    }
}