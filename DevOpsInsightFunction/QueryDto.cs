using System;
using System.Collections.Generic;

namespace DevOpsInsightFunction
{
    public class QueryDto : IDisposable
    {
        private bool disposedValue;
        public class QueryItem
        {
            public string id { get; set; }
            public string name { get; set; }
            public string path { get; set; }
            public bool isFolder { get; set; }
            public bool hasChildren { get; set; }
            public List<QueryItem> children { get; set; }

        }
        public int count { get; set; }
        public List<QueryItem> value { get; set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    this.value = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

