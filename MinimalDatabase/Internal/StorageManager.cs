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
            if (pagingManager == null)
                throw new ArgumentNullException(nameof(pagingManager));

            _pagingManager = pagingManager;
        }

        public uint AllocateStorage(long initialNumberOfBytes)
        {
            StorageHeaderPage firstHeaderPage = new StorageHeaderPage()
            {
                NextHeaderPageId = PagingManager.NullPageId,
                PreviousHeaderPageId = PagingManager.NullPageId,
                NumberOfDataPages = 0,
                NumberOfBytes = 0
            };

            uint firstHeaderPageId = _pagingManager.AllocatePage();
            _pagingManager.WritePage(firstHeaderPageId, firstHeaderPage);

            if(initialNumberOfBytes > 0)
                GetStorageStream(firstHeaderPageId).SetLength(initialNumberOfBytes);

            Logger.WriteLine(LogSenderName, "Allocating storage... Reserved {0} Byte managed at page {1}.", initialNumberOfBytes, firstHeaderPageId);

            return firstHeaderPageId;
        }
        
        public void DeallocateStorage(uint pageId)
        {
            GetStorageStream(pageId).SetLength(0);
            _pagingManager.DeallocatePage(pageId);

            Logger.WriteLine(LogSenderName, "Deallocating storage... Recycled storage managed at page {0}.", pageId);
        }

        public Stream GetStorageStream(uint pageId)
        {
            return new StorageStream(_pagingManager, pageId);
        }
    }
}
