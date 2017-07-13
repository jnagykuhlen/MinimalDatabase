using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MinimalDatabase.Paging;

namespace MinimalDatabase
{
    public class Table<T>
    {
        private string _name;
        private RecordSerializer<T> _serializer;
        private PageReference _page;

        internal Table(string name, RecordSerializer<T> serializer, PageReference page)
        {
            _name = name;
            _serializer = serializer;
            _page = page;
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public RecordSerializer<T> Serializer
        {
            get
            {
                return _serializer;
            }
        }

        internal PageReference Page
        {
            get
            {
                return _page;
            }
        }
    }
}
