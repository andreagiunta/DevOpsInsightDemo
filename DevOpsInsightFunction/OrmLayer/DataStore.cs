using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using DevOpsInsightFunction.OrmLayer.Entities;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.WebJobs;

namespace DevOpsInsightFunction.OrmLayer
{
    public interface IDataStore
    {
        int UpdateSnapshot(Guid queryId, Guid projectID, int value);
        Guid GetProjectId(string projectName);
        Guid GetQueryId(string queryName);
    }
    
    public class DataStore : IDataStore
    {
        //private readonly ExecutionContext context;
        Dictionary<Guid, string> QueryNames;
        Dictionary<Guid, string> ProjectNames;
        public DataStore()
        {
            QueryNames = GetAllQueries();
            ProjectNames = GetAllProjects();
        }

        private Dictionary<Guid, string> GetAllQueries()
        {
            var queries = new Dictionary<Guid, string>();
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlDbConnection")))
            {
                connection.Open();
                queries = connection.Query<QueryEntity>("Select * from Queries").ToDictionary(x => x.QueryId, x => x.QueryName);
                connection.Close();
            }
            return queries;
        }
        private Dictionary<Guid,string> GetAllProjects()
        {
            var projs = new Dictionary<Guid, string>();
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlDbConnection")))
            {
                connection.Open();
                projs = connection.Query<ProjectEntity>("Select * from Projects").ToDictionary(x => x.ProjectId, x => x.ProjectName);
                connection.Close();
            }
            return projs;
        }

        public int UpdateSnapshot(Guid queryId, Guid projectID, int value)
        {
            try
            {
                using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlDbConnection")))
                {
                    connection.Open();
                    var time = DateTime.Now;
                    string command = @"INSERT INTO [dbo].[Snapshots](QueryId,ProjectId,Value,Time) VALUES(@QueryId,@ProjectID,@Value,CURRENT_TIMESTAMP)";
                    var result = connection.Execute(command, new
                    {
                        queryId,
                        projectID,
                        value
                    });
                    connection.Close();
                    return result;
                }
            }
            catch(Exception ex)
            {
                // Skipping some Relationship integrity. to be handled
                return 0;
            }
        }

        public Guid GetProjectId(string projectName)
        {
            if (string.IsNullOrEmpty(projectName)) throw new ArgumentNullException(nameof(projectName));
            return this.ProjectNames.FirstOrDefault(x => x.Value.Equals(projectName)).Key;
        }

        public Guid GetQueryId(string queryName)
        {
            if (string.IsNullOrEmpty(queryName)) throw new ArgumentNullException(nameof(queryName));
            return this.QueryNames.FirstOrDefault(x => x.Value.Equals(queryName)).Key;
        }
    }
}
