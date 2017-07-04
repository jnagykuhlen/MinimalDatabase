using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalDatabase
{
    public interface IPersistenceService
    {
        void WritePage(uint id, byte[] data);
        byte[] ReadPage(uint id);
        void Reserve(uint numberOfPages);

        uint PageSize { get; }
        uint NumberOfPages { get; }
        bool IsReadonly { get; }
    }
}
