using System;
using System.Text;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MinimalDatabase;
using MinimalDatabase.Internal;

namespace UnitTests
{
    [TestClass]
    public class StorageTest
    {
        private const string FilePath = "TestDB.db";
        private const uint StorageLength = 10000000;

        private FilePersistenceService _persistenceService;
        private PagingManager _pagingManager;
        private StorageManager _storageManager;
        private Stream _storageStream;
        private uint _storagePageId;

        [TestInitialize]
        public void Initialize()
        {
            _persistenceService = new FilePersistenceService(FilePath);
            _pagingManager = new PagingManager(_persistenceService, Encoding.UTF8);
            _storageManager = new StorageManager(_pagingManager);
            _storagePageId = _storageManager.AllocateStorage(StorageLength);
            _storageStream = _storageManager.GetStorageStream(_storagePageId);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _storageStream.Dispose();
            _storageManager.DeallocateStorage(_storagePageId);
            _persistenceService.Dispose();
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
    }
}
