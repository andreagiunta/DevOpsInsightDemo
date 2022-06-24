using System;
using System.Collections.Generic;

namespace DevOpsInsightFunction
{
    public class ExecutedQueryDto
    {
        public class Column
        {
            public string referenceName { get; set; }
            public string name { get; set; }
            public string url { get; set; }
        }
        public class WorkItem
        {
            public int id { get; set; }
            public string url { get; set; }
        }
        public string queryType { get; set; }
        public string queryResultType { get; set; }
        public DateTime asOf { get; set; }
        public List<Column> columns { get; set; }
        public List<WorkItem> workItems { get; set; }
    }

}

