using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using MinimalDatabase.Internal;
using MinimalDatabase.Persistence;

namespace MinimalDatabase
{
    public class Database : IDisposable
    {
        private const uint Identifier = 109237914;
        private const uint CurrentVersion = 1;

        private PagingManager _pagingManager;
        private StorageManager _storageManager;
        private Dictionary<string, PageReference> _tables;

        public Database(IPersistenceProvider persistenceProvider)
            : this(new PagingManager(persistenceProvider)) { }

        public Database(IPersistenceProvider persistenceProvider, Encoding encoding, uint pageSize)
            : this(new PagingManager(persistenceProvider, encoding, pageSize)) { }

        private Database(PagingManager pagingManager)
        {
            _pagingManager = pagingManager;
            _storageManager = new StorageManager(_pagingManager);
            _tables = new Dictionary<string, PageReference>();

            if (_pagingManager.NextHeaderPageId == PagingManager.NullPageId)
                Initialize();

            DatabaseHeaderPage databaseHeaderPage = new DatabaseHeaderPage();
            _pagingManager.ReadPage(_pagingManager.NextHeaderPageId, databaseHeaderPage);

            if (databaseHeaderPage.Identifier != Identifier)
                throw new DatabaseException("Corrupt header information.");
        }

        private void Initialize()
        {
            DatabaseHeaderPage databaseHeaderPage = new DatabaseHeaderPage()
            {
                Identifier = Identifier,
                Version = CurrentVersion
            };

            uint databaseHeaderPageId = _pagingManager.AllocatePage();
            _pagingManager.WritePage(databaseHeaderPageId, databaseHeaderPage);
            _pagingManager.SetNextHeaderPage(databaseHeaderPageId);
        }
        
        public Table<T> GetTable<T>(string name, RecordSerializer<T> serializer)
        {
            PageReference page;
            if(!_tables.TryGetValue(name, out page))
            {
                page = new PageReference(PagingManager.NullPageId);
                _tables.Add(name, page);
            }

            return new Table<T>(name, serializer, page);
        }

        public void Dispose()
        {
            _pagingManager.Dispose();
        }
        
        public StorageManager StorageManager
        {
            get
            {
                return _storageManager;
            }
        }
    }
}
