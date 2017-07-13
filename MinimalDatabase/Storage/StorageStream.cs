using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

using MinimalDatabase.Paging;

namespace MinimalDatabase.Storage
{
    public class StorageStream : Stream
    {
        private PagingManager _pagingManager;
        private uint _firstHeaderPageId;
        private uint _lastHeaderPageId;
        private long _length;
        private long _position;

        private StorageHeaderPage _currentHeaderPage;
        private long _currentHeaderPageOffset;
        private byte[] _currentDataPage;
        private uint _currentDataPageId;
        private long _currentDataPageOffset;
        private bool _isCurrentDataPageDirty;
        
        public StorageStream(PagingManager pagingManager, uint pageId)
        {
            _pagingManager = pagingManager;
            _firstHeaderPageId = pageId;
            _length = 0;
            _position = 0;

            StorageHeaderPage headerPage = new StorageHeaderPage();
            uint currentHeaderPageId = _firstHeaderPageId;

            while (currentHeaderPageId != PagingManager.NullPageId)
            {
                _pagingManager.ReadPage(currentHeaderPageId, headerPage);

                _lastHeaderPageId = currentHeaderPageId;
                _length += headerPage.NumberOfBytes;

                currentHeaderPageId = headerPage.NextHeaderPageId;
            }
            
            _pagingManager.ReadPage(_firstHeaderPageId, headerPage);
            _currentHeaderPage = headerPage;
            _currentHeaderPageOffset = 0;
            _currentDataPage = null;
            _currentDataPageId = PagingManager.NullPageId;
            _currentDataPageOffset = 0;
            _isCurrentDataPageDirty = false;
        }
        
        public override void Flush()
        {
           if(_isCurrentDataPageDirty)
            {
                _pagingManager.WritePage(_currentDataPageId, _currentDataPage);
                _isCurrentDataPageDirty = false;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position = Position + offset;
                    break;
                case SeekOrigin.End:
                    Position = Position + Length + offset;
                    break;
                default:
                    throw new ArgumentException("Invalid seek origin.", "origin");
            }

            return Position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset + count > buffer.Length)
                throw new ArgumentException("Read data exceeds buffer size.", "buffer");

            if (offset < 0)
                throw new ArgumentException("Offset must not be negative.", "offset");

            if (count < 0)
                throw new ArgumentException("Count must not be negative.", "count");

            if (_position + count > _length)
                count = Math.Max((int)(_length - _position), 0);

            if (count == 0)
                return count;

            int remainingBytes = count;
            while(remainingBytes > 0)
            {
                int availableBytes = (int)(_pagingManager.PageSize - (_position % _pagingManager.PageSize));
                int bytesToRead = Math.Min(remainingBytes, availableBytes);

                if(!IsLocated(_position))
                    LocateDataPage(_position);

                Array.Copy(_currentDataPage, _position - _currentDataPageOffset, buffer, offset, bytesToRead);

                _position += bytesToRead;
                offset += bytesToRead;
                remainingBytes -= bytesToRead;
            }

            return count;
        }

        private void ResetCurrentHeaderPage()
        {
            _pagingManager.ReadPage(_firstHeaderPageId, _currentHeaderPage);
            _currentHeaderPageOffset = 0;
        }

        private void LocateDataPage(long position)
        {
            if (position < _currentHeaderPageOffset)
                ResetCurrentHeaderPage();

            while (position >= _currentHeaderPageOffset + _currentHeaderPage.NumberOfDataPages * _pagingManager.PageSize)
            {
                Debug.Assert(_currentHeaderPage.NextHeaderPageId != PagingManager.NullPageId, "Trying to locate position not included in headers.");
                _currentHeaderPageOffset += _currentHeaderPage.NumberOfDataPages * _pagingManager.PageSize;
                _pagingManager.ReadPage(_currentHeaderPage.NextHeaderPageId, _currentHeaderPage);
            }

            int dataPageIndex = (int)((position - _currentHeaderPageOffset) / _pagingManager.PageSize);
            Debug.Assert(dataPageIndex >= 0 && dataPageIndex < _currentHeaderPage.NumberOfDataPages, "Data page index exceeds header page IDs.");

            _currentDataPageId = _currentHeaderPage.DataPageIds[dataPageIndex];
            _currentDataPageOffset = _currentHeaderPageOffset + dataPageIndex * _pagingManager.PageSize;
            _currentDataPage = _pagingManager.ReadPage(_currentDataPageId);
        }

        private bool IsLocated(long position)
        {
            return _currentDataPageId != PagingManager.NullPageId && position >= _currentDataPageOffset && position < _currentDataPageOffset + _pagingManager.PageSize;
        }
        
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset + count > buffer.Length)
                throw new ArgumentException("Write data exceeds buffer size.", nameof(buffer));

            if (offset < 0)
                throw new ArgumentException("Offset must not be negative.", nameof(offset));

            if (count < 0)
                throw new ArgumentException("Count must not be negative.", nameof(count));

            if (_position + count > _length)
                count = Math.Max((int)(_length - _position), 0);

            int remainingBytes = count;
            while (remainingBytes > 0)
            {
                int availableBytes = (int)(_pagingManager.PageSize - (_position % _pagingManager.PageSize));
                int bytesToWrite = Math.Min(remainingBytes, availableBytes);

                if (!IsLocated(_position))
                {
                    Flush();
                    LocateDataPage(_position);
                }

                Array.Copy(buffer, offset, _currentDataPage, _position - _currentDataPageOffset, bytesToWrite);
                _isCurrentDataPageDirty = true;

                _position += bytesToWrite;
                offset += bytesToWrite;
                remainingBytes -= bytesToWrite;
            }

            Flush();
        }

        public override void SetLength(long totalNumberOfBytes)
        {
            uint existingNumberOfDataPages = GetNumberOfPages(_length, _pagingManager.PageSize);
            uint requiredNumberOfDataPages = GetNumberOfPages(totalNumberOfBytes, _pagingManager.PageSize);
            uint numberOfDataPagesPerHeaderPage = (_pagingManager.PageSize - StorageHeaderPage.FixedHeaderLength) / sizeof(uint);
            uint numberOfBytesPerHeaderPage = numberOfDataPagesPerHeaderPage * _pagingManager.PageSize;

            if (totalNumberOfBytes > _length)
            {
                AllocateStorage(
                    totalNumberOfBytes,
                    existingNumberOfDataPages,
                    requiredNumberOfDataPages,
                    numberOfDataPagesPerHeaderPage,
                    numberOfBytesPerHeaderPage
                );
            }
            else if (totalNumberOfBytes < _length)
            {
                DeallocateStorage(
                    totalNumberOfBytes,
                    existingNumberOfDataPages,
                    requiredNumberOfDataPages,
                    numberOfDataPagesPerHeaderPage,
                    numberOfBytesPerHeaderPage
                );
            }

            _length = totalNumberOfBytes;
            ResetCurrentHeaderPage();
        }

        private void AllocateStorage(
            long totalNumberOfBytes,
            uint existingNumberOfDataPages,
            uint requiredNumberOfDataPages,
            uint numberOfDataPagesPerHeaderPage,
            uint numberOfBytesPerHeaderPage)
        {
            StorageHeaderPage lastHeaderPage = new StorageHeaderPage();
            _pagingManager.ReadPage(_lastHeaderPageId, lastHeaderPage);

            uint currentHeaderPageId = _lastHeaderPageId;
            uint previousHeaderPageId = lastHeaderPage.PreviousHeaderPageId;
            uint remainingNumberOfDataPages = requiredNumberOfDataPages - existingNumberOfDataPages + lastHeaderPage.NumberOfDataPages;
            uint[] dataPageIds = new uint[numberOfDataPagesPerHeaderPage];
            uint offsetPages = lastHeaderPage.NumberOfDataPages;

            Array.Copy(lastHeaderPage.DataPageIds, dataPageIds, offsetPages);

            do
            {
                uint numberOfDataPages = remainingNumberOfDataPages;
                uint nextHeaderPageId = PagingManager.NullPageId;
                uint numberOfBytes = (uint)(totalNumberOfBytes % numberOfBytesPerHeaderPage);
                bool allocateNextHeaderPage = false;

                if (remainingNumberOfDataPages > numberOfDataPagesPerHeaderPage)
                {
                    numberOfDataPages = numberOfDataPagesPerHeaderPage;
                    numberOfBytes = numberOfBytesPerHeaderPage;
                    allocateNextHeaderPage = true;
                }

                for (uint i = offsetPages; i < numberOfDataPages; ++i)
                    dataPageIds[i] = _pagingManager.AllocatePage();

                if (allocateNextHeaderPage)
                    nextHeaderPageId = _pagingManager.AllocatePage();

                StorageHeaderPage storageHeaderPage = new StorageHeaderPage()
                {
                    NextHeaderPageId = nextHeaderPageId,
                    PreviousHeaderPageId = previousHeaderPageId,
                    NumberOfDataPages = numberOfDataPages,
                    NumberOfBytes = numberOfBytes,
                    DataPageIds = dataPageIds
                };

                _pagingManager.WritePage(currentHeaderPageId, storageHeaderPage);

                previousHeaderPageId = currentHeaderPageId;
                currentHeaderPageId = nextHeaderPageId;
                remainingNumberOfDataPages -= numberOfDataPages;
                offsetPages = 0;
            }
            while (remainingNumberOfDataPages > 0);

            _lastHeaderPageId = previousHeaderPageId;
        }

        public void DeallocateStorage(
            long totalNumberOfBytes,
            uint existingNumberOfDataPages,
            uint requiredNumberOfDataPages,
            uint numberOfDataPagesPerHeaderPage,
            uint numberOfBytesPerHeaderPage)
        {
            StorageHeaderPage currentHeaderPage = new StorageHeaderPage();
            uint currentHeaderPageId = _lastHeaderPageId;
            uint remainingNumberOfDataPages = existingNumberOfDataPages - requiredNumberOfDataPages;
            
            while(true)
            {
                _pagingManager.ReadPage(currentHeaderPageId, currentHeaderPage);
                
                if (remainingNumberOfDataPages < currentHeaderPage.NumberOfDataPages || currentHeaderPageId == _firstHeaderPageId)
                {
                    uint usedPages = currentHeaderPage.NumberOfDataPages - remainingNumberOfDataPages;

                    for (int i = (int)(currentHeaderPage.NumberOfDataPages - 1); i >= usedPages; --i)
                        _pagingManager.DeallocatePage(currentHeaderPage.DataPageIds[i]);

                    currentHeaderPage.NumberOfDataPages = usedPages;
                    currentHeaderPage.NumberOfBytes = (uint)(totalNumberOfBytes % numberOfBytesPerHeaderPage);
                    currentHeaderPage.NextHeaderPageId = PagingManager.NullPageId;

                    _pagingManager.WritePage(currentHeaderPageId, currentHeaderPage);
                    break;
                }
                else
                {
                    for (int i = (int)(currentHeaderPage.NumberOfDataPages - 1); i >= 0; --i)
                        _pagingManager.DeallocatePage(currentHeaderPage.DataPageIds[i]);

                    _pagingManager.DeallocatePage(currentHeaderPageId);
                    remainingNumberOfDataPages -= currentHeaderPage.NumberOfDataPages;
                    currentHeaderPageId = currentHeaderPage.PreviousHeaderPageId;
                }
            }

            _lastHeaderPageId = currentHeaderPageId;
        }

        private static uint GetNumberOfPages(long numberOfBytes, uint pageSize)
        {
            uint numberOfPages = (uint)(numberOfBytes / pageSize);
            if (numberOfBytes % pageSize > 0)
                ++numberOfPages;

            return numberOfPages;
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override long Length
        {
            get
            {
                return _length;
            }
        }

        public override long Position
        {
            get
            {
                return _position;
            }

            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", value, "Stream position must not be negative.");

                _position = value;
            }
        }

        private List<KeyValuePair<uint, StorageHeaderPage>> GetHeaderList()
        {
            List<KeyValuePair<uint, StorageHeaderPage>> headerList =
                new List<KeyValuePair<uint, StorageHeaderPage>>(16);
            
            uint currentHeaderPageId = _firstHeaderPageId;

            while (currentHeaderPageId != PagingManager.NullPageId)
            {
                StorageHeaderPage headerPage = new StorageHeaderPage();
                _pagingManager.ReadPage(currentHeaderPageId, headerPage);

                headerList.Add(new KeyValuePair<uint, StorageHeaderPage>(currentHeaderPageId, headerPage));
                currentHeaderPageId = headerPage.NextHeaderPageId;
            }

            return headerList;
        }
    }
}
