using System;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MinimalDatabase;
using MinimalDatabase.Internal;
using MinimalDatabase.Persistence;

namespace UnitTests
{
    [TestClass]
    public class StorageTest
    {
        private const string FilePath = "TestDB.db";
        private const uint StorageLength = 10000000;

        private PagingManager _pagingManager;
        private StorageManager _storageManager;
        private Stream _storageStream;
        private uint _storagePageId;

        [TestInitialize]
        public void Initialize()
        {
            _pagingManager = new PagingManager(new FilePersistenceProvider(FilePath, false));
            _storageManager = new StorageManager(_pagingManager);
            _storagePageId = _storageManager.AllocateStorage(StorageLength);
            _storageStream = _storageManager.GetStorageStream(_storagePageId);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _storageStream.Dispose();
            _storageManager.DeallocateStorage(_storagePageId);
            _pagingManager.Dispose();
            File.Delete(FilePath);
        }

        [TestMethod]
        public void TestRandomAccess()
        {
            Random random = new Random(42);
            byte[] writeData = new byte[9247];
            byte[] readData = new byte[writeData.Length];
            int sampleLength = writeData.Length - 100;

            random.NextBytes(writeData);

            long[] samplePositions = new long[]
            {
                9956421,
                 151345,
                6635489,
                 348913,
                3448949,
                1234565
            };

            random = new Random(42);
            for (int i = 0; i < samplePositions.Length; ++i)
            {
                int writeOffset = random.Next(0, 100);
                _storageStream.Position = samplePositions[i];
                _storageStream.Write(writeData, writeOffset, sampleLength);
            }

            random = new Random(42);
            for (int i = 0; i < samplePositions.Length; ++i)
            {
                _storageStream.Position = samplePositions[i];
                int readBytes = _storageStream.Read(readData, i, sampleLength);
                int writeOffset = random.Next(0, 100);

                Assert.IsTrue(readBytes == sampleLength, "Less bytes read than written.");

                bool isDataEqual = true;
                for (int j = 0; j < sampleLength; ++j)
                {
                    if (readData[i + j] != writeData[writeOffset + j])
                    {
                        isDataEqual = false;
                        break;
                    }
                }

                Assert.IsTrue(isDataEqual, "Read data does not match written data.");
            }
        }

        [TestMethod]
        public void TestResize()
        {
            Random random = new Random(42);
            byte[] writeData = new byte[StorageLength];
            byte[] readData = new byte[StorageLength];
            random.NextBytes(writeData);

            _storageStream.Write(writeData, 0, writeData.Length);

            long[] sampleLengths = new long[]
            {
                9956421,
                6635489,
                3448949,
                5551334,
                8345612,
                 348913,
                1234565
            };

            long minimumLength = _storageStream.Length;
            foreach(long length in sampleLengths)
            {
                Array.Clear(readData, 0, readData.Length);
                _storageStream.SetLength(length);
                _storageStream.Position = 0;

                if (length < minimumLength)
                    minimumLength = length;
                
                Assert.IsTrue(_storageStream.Length == length, "Stream length does not match set length.");

                int readBytes = _storageStream.Read(readData, 0, readData.Length);
                Assert.IsTrue(readBytes == length, "Did not read full storage data.");

                for (int i = 0; i < minimumLength; ++i)
                    Assert.IsTrue(readData[i] == writeData[i], "Read data does not match written data.");

                for (int i = (int)length; i < writeData.Length; ++i)
                    Assert.IsTrue(readData[i] == 0, "Read data exceeds end of stream.");
            }
        }
    }
}
