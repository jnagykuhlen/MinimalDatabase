using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalDatabase
{
    public interface IPersistenceService : IDisposable
    {
        void WritePage(uint id, byte[] data);
        byte[] ReadPage(uint id);
        void SetNumberOfPages(uint numberOfPages);

        uint PageSize { get; }
        uint NumberOfPages { get; }
    }
}
