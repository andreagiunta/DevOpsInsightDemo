using System;
using System.Collections.Generic;

namespace DevOpsInsightFunction
{
    public class ProjectDto : IDisposable
    {
        private bool disposedValue;

        public class ProjectValue
        {
            public string id { get; set; }
            public string name { get; set; }
            public string url { get; set; }
            public string state { get; set; }
            public int revision { get; set; }
            public string visibility { get; set; }
            public DateTime lastUpdateTime { get; set; }
            public string description { get; set; }
        }
        public int count { get; set; }
        public List<ProjectValue> value { get; set; }

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

