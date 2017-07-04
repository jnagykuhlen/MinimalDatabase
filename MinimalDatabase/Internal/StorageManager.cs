using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using MinimalDatabase.Logging;

namespace MinimalDatabase.Internal
{
    public class StorageManager
    {
        private const string LogSenderName = "Storage";

        private PagingManager _pagingManager;

        public StorageManager(PagingManager pagingManager)
        {
            _pagingManager = pagingManager;
        }

        public uint AllocateStorage(uint numberOfBytes)
        {
            uint totalNumberOfDataPages = numberOfBytes / _pagingManager.PageSize;
            if (numberOfBytes % _pagingManager.PageSize > 0)
                totalNumberOfDataPages++;

            uint totalNumberOfUnusedBytes = totalNumberOfDataPages * _pagingManager.PageSize - numberOfBytes;
            uint numberOfDataPagesPerHeaderPage = (_pagingManager.PageSize - StorageHeaderPage.FixedHeaderLength) / sizeof(uint);

            uint firstHeaderPageId = _pagingManager.AllocatePage();
            uint currentHeaderPageId = firstHeaderPageId;
            uint remainingNumberOfDataPages = totalNumberOfDataPages;

            uint[] dataPageIds = new uint[numberOfDataPagesPerHeaderPage];

            do
            {
                uint numberOfDataPages = remainingNumberOfDataPages;
                uint nextHeaderPageId = PagingManager.NullPageId;

                if (remainingNumberOfDataPages > numberOfDataPagesPerHeaderPage)
                {
                    numberOfDataPages = numberOfDataPagesPerHeaderPage;
                    nextHeaderPageId = _pagingManager.AllocatePage();
                }

                for (int i = 0; i < numberOfDataPages; ++i)
                    dataPageIds[i] = _pagingManager.AllocatePage();

                StorageHeaderPage storageHeaderPage = new StorageHeaderPage()
                {
                    NextHeaderPageId = nextHeaderPageId,
                    NumberOfDataPages = numberOfDataPages,
                    TotalNumberOfDataPages = totalNumberOfDataPages,
                    TotalNumberOfUnusedBytes = totalNumberOfUnusedBytes,
                    DataPageIds = dataPageIds
                };

                _pagingManager.WritePage(currentHeaderPageId, storageHeaderPage);

                currentHeaderPageId = nextHeaderPageId;
                remainingNumberOfDataPages -= numberOfDataPages;
            }
            while (remainingNumberOfDataPages > 0);

            Logger.WriteLine(LogSenderName, "Allocating storage... Reserved {0} Byte managed at page {1}.", numberOfBytes, firstHeaderPageId);

            return firstHeaderPageId;
        }
        
        public void DeallocateStorage(uint pageId)
        {
            StorageHeaderPage headerPage = new StorageHeaderPage();
            uint currentHeaderPageId = pageId;

            while(currentHeaderPageId != PagingManager.NullPageId)
            {
                _pagingManager.ReadPage(currentHeaderPageId, headerPage);

                for (int i = (int)headerPage.NumberOfDataPages - 1; i >= 0; --i)
                    _pagingManager.DeallocatePage(headerPage.DataPageIds[i]);

                _pagingManager.DeallocatePage(currentHeaderPageId);
                currentHeaderPageId = headerPage.NextHeaderPageId;
            }

            Logger.WriteLine(LogSenderName, "Deallocating storage... Recycled storage managed at page {0}.", pageId);
        }

        public Stream GetStorageStream(uint pageId)
        {
            return new StorageStream(_pagingManager, pageId);
        }
    }
}
