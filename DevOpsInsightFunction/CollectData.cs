using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http.Json;
using System.Linq;
using System.Text.Json.Serialization;

namespace DevOpsInsightFunction
{
    public class CollectData
    {

        private readonly HttpClient httpClient;
        private string baseApi = System.Environment.GetEnvironmentVariable("BaseApi");
        private string PAT = System.Environment.GetEnvironmentVariable("PAT");
        private Dictionary<string, Dictionary<string, string>> ProjectQueries = new Dictionary<string, Dictionary<string, string>>(); // {ProjectName,{QueryName,QueryID}}
        private Dictionary<string, Dictionary<string, int>> QueriesResult = new Dictionary<string, Dictionary<string, int>>(); // {ProjectName,{QueryName,Results}}
        private string[] queriesNameList = System.Environment.GetEnvironmentVariable("Queries").Split(",");
        public CollectData(HttpClient httpClient)
        {
            this.httpClient = httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        System.Text.ASCIIEncoding.ASCII.GetBytes(
                            string.Format("{0}:{1}", "", PAT))));
        }

        [FunctionName("CollectData")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
                 ILogger log)
        {
            try
            {
                var projects = await httpClient.GetFromJsonAsync<ProjectDto>($"{baseApi}/_apis/projects");
                foreach (var project in projects.value.Where(x=>x.name.StartsWith("DevOps")))
                {
                    var queries = await GetQueries(project.name);
                    ProjectQueries.TryAdd(
                        project.name,
                        queries.value.ToDictionary(x => x.name, x => x.id));
                }
                
                //Should be moved on a Fan-Out / Fan-In design pattern upon
                //DurableFunction availabilities on IsolatedProcess

                foreach (var project in ProjectQueries)
                {
                    Dictionary<string, int> internalResult = new Dictionary<string, int>();
                    //Execution of each query on each project
                    foreach (var QueryItem in project.Value)
                    {
                        var result = await ExecuteQuery(project.Key, QueryItem.Value);
                        internalResult.TryAdd(QueryItem.Key, result);
                    }
                    QueriesResult.TryAdd(project.Key, internalResult);
                }

                //Make csv
                StringBuilder sb = new StringBuilder();
                foreach (var project in QueriesResult)
                {
                    sb.AppendLine($"{project.Key},QueryName,Count");
                    foreach (var queryItem in project.Value)
                        sb.AppendLine($",{queryItem.Key},{queryItem.Value},");
                }
                

                //Returning as FileContent
                string csv = sb.ToString();
                byte[] filebytes = Encoding.UTF8.GetBytes(csv);
                return new FileContentResult(filebytes, "application/octet-stream")
                {
                    FileDownloadName = "Export.csv"
                };
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<QueryDto> GetQueries(string projectName)
        {
            try
            {
                var queries = await httpClient.GetFromJsonAsync<QueryDto>($"{baseApi}/{projectName}/_apis/wit/queries?$expand=all&$depth=1&api-version=6.0");
                QueryDto retValue = new QueryDto() { value = new List<QueryDto.QueryItem>() };
                foreach (var item in queries.value)
                {
                    if (item.isFolder && item.hasChildren)
                        retValue.value.AddRange(item.children.Where(x => queriesNameList.Contains(x.name)));
                }
                retValue.count = retValue.value.Count;
                return retValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public async Task<int> ExecuteQuery(string projectName, string queryId)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                            System.Text.ASCIIEncoding.ASCII.GetBytes(
                                string.Format("{0}:{1}", "", PAT))));

                    using (HttpResponseMessage response = await client.GetAsync($"{baseApi}/{projectName}/_apis/wit/wiql/{queryId}?api-version=6.0"))
                    {
                        response.EnsureSuccessStatusCode();
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<ExecutedQueryDto>(responseBody);
                        if (result.workItems != null)
                            return result.workItems.Count;
                        else
                            return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 0;
            }
        }
    }
}

