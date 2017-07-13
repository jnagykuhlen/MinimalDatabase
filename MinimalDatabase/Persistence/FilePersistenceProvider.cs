using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MinimalDatabase.Persistence
{
    public class FilePersistenceProvider : IPersistenceProvider
    {
        private string _filePath;
        private bool _isReadonly;

        public FilePersistenceProvider(string filePath)
            : this(filePath, false) { }

        public FilePersistenceProvider(string filePath, bool isReadonly)
        {
            _filePath = filePath;
            _isReadonly = isReadonly;
        }

        public IPagedPersistence OpenDatabase(uint pageSize)
        {
            return new FilePagedPersistence(DatabaseFilePath, _isReadonly, pageSize);
        }

        public IPagedPersistence OpenJournal(uint pageSize)
        {
            return new FilePagedPersistence(JournalFilePath, _isReadonly, pageSize);
        }

        public void DeleteJournal()
        {
            File.Delete(JournalFilePath);
        }
        
        public bool JournalExists
        {
            get
            {
                return File.Exists(JournalFilePath);
            }
        }

        public string DatabaseFilePath
        {
            get
            {
                return _filePath;
            }
        }

        public string JournalFilePath
        {
            get
            {
                return _filePath + ".journal";
            }
        }
    }
}
