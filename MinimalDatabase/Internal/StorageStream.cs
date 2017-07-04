using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MinimalDatabase.Internal
{
    public class StorageStream : Stream
    {
        private PagingManager _pagingManager;
        private uint _pageId;
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
            _pageId = pageId;

            StorageHeaderPage headerPage = new StorageHeaderPage();
            _pagingManager.ReadPage(_pageId, headerPage);

            _length = headerPage.TotalNumberOfDataPages * pagingManager.PageSize - headerPage.TotalNumberOfUnusedBytes;
            _position = 0;
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

        private void LocateDataPage(long position)
        {
            if (position < _currentHeaderPageOffset)
            {
                _pagingManager.ReadPage(_pageId, _currentHeaderPage);
                _currentHeaderPageOffset = 0;
            }

            while (position > _currentHeaderPageOffset + _currentHeaderPage.NumberOfDataPages * _pagingManager.PageSize)
            {
                _currentHeaderPageOffset += _currentHeaderPage.NumberOfDataPages * _pagingManager.PageSize;
                _pagingManager.ReadPage(_currentHeaderPage.NextHeaderPageId, _currentHeaderPage);
            }

            int dataPageIndex = (int)((position - _currentHeaderPageOffset) / _pagingManager.PageSize);

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
                throw new ArgumentNullException("buffer");

            if (offset + count > buffer.Length)
                throw new ArgumentException("Write data exceeds buffer size.", "buffer");

            if (offset < 0)
                throw new ArgumentException("Offset must not be negative.", "offset");

            if (count < 0)
                throw new ArgumentException("Count must not be negative.", "count");

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

        public override void SetLength(long value)
        {
            // TODO
            throw new NotImplementedException();
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
    }
}
