using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MinimalDatabase.Persistence
{
    public class FilePagedPersistence : IPagedPersistence, IDisposable
    {
        private FileStream _fileStream;
        private bool _isReadonly;
        private uint _pageSize;
        private uint _numberOfPages;
        
        public FilePagedPersistence(string filePath, bool isReadonly, uint pageSize)
        {
            _isReadonly = isReadonly;
            _pageSize = pageSize;
            _fileStream = new FileStream(filePath, FileMode.OpenOrCreate, isReadonly ? FileAccess.Read : FileAccess.ReadWrite);
            _numberOfPages = (uint)(_fileStream.Length / _pageSize);
        }

        public void Flush()
        {
            CheckWriteAccess();
            _fileStream.Flush();
        }

        public void Dispose()
        {
            _fileStream.Dispose();
        }

        public void WritePage(uint id, byte[] data)
        {
            CheckWriteAccess();
            _fileStream.Seek((long)id * _pageSize, SeekOrigin.Begin);
            _fileStream.Write(data, 0, (int)_pageSize);
        }

        public byte[] ReadPage(uint id)
        {
            byte[] data = new byte[_pageSize];
            _fileStream.Seek((long)id * _pageSize, SeekOrigin.Begin);
            _fileStream.Read(data, 0, (int)_pageSize);
            return data;
        }

        public void SetNumberOfPages(uint numberOfPages)
        {
            CheckWriteAccess();
            _fileStream.SetLength((long)numberOfPages * _pageSize);
            _numberOfPages = numberOfPages;
        }

        private void CheckWriteAccess()
        {
            if (_isReadonly)
                throw new InvalidOperationException("File persistence service is readonly and thus cannot be written to.");
        }

        public uint PageSize
        {
            get
            {
                return _pageSize;
            }
        }

        public uint NumberOfPages
        {
            get
            {
                return _numberOfPages;
            }
        }

        public bool IsReadonly
        {
            get
            {
                return _isReadonly;
            }
        }
    }
}
