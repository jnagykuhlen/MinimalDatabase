using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using MinimalDatabase.Logging;

namespace MinimalDatabase.Internal
{
    public class PagingManager
    {
        public const uint NullPageId = 0;

        private const uint MinimumPageSize = 512;
        private const uint PagingHeaderPageId = 0;
        private const string LogSenderName = "Paging";

        private IPersistenceService _persistenceService;
        private Encoding _encoding;
        private uint _pageSize;
        private uint _nextFreePageId;
        private uint _nextHeaderPageId;

        public PagingManager(IPersistenceService persistenceService, Encoding encoding)
        {
            _persistenceService = persistenceService;
            _encoding = encoding;
            _pageSize = persistenceService.PageSize;
            _nextFreePageId = NullPageId;
            _nextHeaderPageId = NullPageId;
            
            if (_pageSize < MinimumPageSize)
                throw new DatabaseException(String.Format("Persistence service does not provide a minimum page size of {0} bytes.", MinimumPageSize));

            if (_persistenceService.NumberOfPages == 0)
                Initialize();

            PagingHeaderPage pagingHeaderPage = new PagingHeaderPage();
            ReadPage(PagingHeaderPageId, pagingHeaderPage);
            
            if (pagingHeaderPage.PageSize != _pageSize)
                throw new DatabaseException("Page sizes of database and persistence service do not match.");
            
            _nextFreePageId = pagingHeaderPage.NextFreePageId;
            _nextHeaderPageId = pagingHeaderPage.NextHeaderPageId;

            Logger.WriteLine(LogSenderName, "Loaded paging with page size {0} and next header at page {1}.", _pageSize, _nextHeaderPageId);
        }

        private void Initialize()
        {
            if (_persistenceService.IsReadonly)
                throw new DatabaseException("Cannot initialize database without write access to persistence service.");
            
            ReservePage(PagingHeaderPageId);
            UpdatePagingHeaderPage();

            Logger.WriteLine(LogSenderName, "Initialized paging with page size {0}.", _pageSize);
        }
        
        private void ReservePage(uint pageId)
        {
            _persistenceService.Reserve(pageId + 1);
        }

        private void UpdatePagingHeaderPage()
        {
            PagingHeaderPage defaultPagingHeaderPage = new PagingHeaderPage()
            {
                PageSize = _pageSize,
                NextFreePageId = _nextFreePageId,
                NextHeaderPageId = _nextHeaderPageId
            };

            WritePage(PagingHeaderPageId, defaultPagingHeaderPage);
        }

        public void SetNextHeaderPage(uint pageId)
        {
            _nextHeaderPageId = pageId;
            UpdatePagingHeaderPage();

            Logger.WriteLine(LogSenderName, "Next header is set to page {0}.", pageId);
        }
        
        public void WritePage(uint pageId, byte[] data)
        {
            Logger.WriteLine(LogSenderName, "Written raw data to page {0}.", pageId);
            _persistenceService.WritePage(pageId, data);
        }

        public byte[] ReadPage(uint pageId)
        {
            Logger.WriteLine(LogSenderName, "Read raw data from page {0}.", pageId);
            return _persistenceService.ReadPage(pageId);
        }
        
        public void WritePage(uint pageId, Page page)
        {
            byte[] data = new byte[_pageSize];
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream(data), _encoding))
            {
                page.WriteToPersistence(writer);
            }

            _persistenceService.WritePage(pageId, data);

            Logger.WriteLine(LogSenderName, "Written structured data ({0}) to page {1}.", page.GetType().Name, pageId);
        }

        public void ReadPage(uint pageId, Page page)
        {
            byte[] data = _persistenceService.ReadPage(pageId);
            using (BinaryReader reader = new BinaryReader(new MemoryStream(data), _encoding))
            {
                page.ReadFromPersistence(reader);
            }

            Logger.WriteLine(LogSenderName, "Read structured data ({0}) from page {1}.", page.GetType().Name, pageId);
        }

        public uint AllocatePage()
        {
            uint pageId;
            if (_nextFreePageId != NullPageId)
            {
                pageId = _nextFreePageId;
                byte[] data = ReadPage(pageId);
                _nextFreePageId = BitConverter.ToUInt32(data, 0);

                UpdatePagingHeaderPage();

                Logger.WriteLine(LogSenderName, "Allocating... Recycled page {0}.", pageId);
            }
            else
            {
                pageId = _persistenceService.NumberOfPages;
                _persistenceService.Reserve(pageId + 1);

                Logger.WriteLine(LogSenderName, "Allocating... Reserved more pages, return page {0}.", pageId);
            }

            return pageId;
        }

        public void DeallocatePage(uint pageId)
        {
            byte[] data = new byte[_pageSize];
            Array.Copy(BitConverter.GetBytes(_nextFreePageId), 0, data, 0, sizeof(uint));
            WritePage(pageId, data);
            _nextFreePageId = pageId;

            UpdatePagingHeaderPage();

            Logger.WriteLine(LogSenderName, "Deallocating... Marked page {0} for recycling.", pageId);
        }
        
        public uint NextHeaderPageId
        {
            get
            {
                return _nextHeaderPageId;
            }
        }

        public uint PageSize
        {
            get
            {
                return _pageSize;
            }
        }
    }
}
