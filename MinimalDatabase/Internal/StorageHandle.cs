using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalDatabase.Internal
{
    public class StorageHandle
    {
        private uint _headerPageId;
        private uint _numberOfBytes;

        public StorageHandle(uint headerPageId, uint numberOfBytes)
        {
            _headerPageId = headerPageId;
            _numberOfBytes = numberOfBytes;
        }

        public uint HeaderPageId
        {
            get
            {
                return _headerPageId;
            }
        }

        public uint NumberOfBytes
        {
            get
            {
                return _numberOfBytes;
            }
        }
    }
}
