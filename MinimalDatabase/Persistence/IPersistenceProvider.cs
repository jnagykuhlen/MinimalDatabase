using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalDatabase.Persistence
{
    public interface IPersistenceProvider
    {
        IPagedPersistence OpenDatabase(uint pageSize);
        IPagedPersistence OpenJournal(uint pageSize);
        void DeleteJournal();
        bool JournalExists { get; }
    }
}
