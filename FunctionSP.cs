using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace JSON_Azure_Function
{
    public static class FunctionSP
    {
        private static string SPName { set; get; }
        private static string ConnectionString { set; get; }
        [FunctionName("FunctionSP")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ExecutionContext context,
            ILogger log)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true) // <- This gives you access to your application settings in your local development environment
                .AddEnvironmentVariables() // <- This is what actually gets you the application settings in Azure
                .Build();

            SPName = config["SPName"];
            ConnectionString = config["ConnectionStrings:ConnectionString"];

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestBody);
           

            var result = ExecuteSP(data);
            string responseMessage = Convert.ToString(result);

            return new OkObjectResult(responseMessage);
        }


        private static object ExecuteSP(Dictionary<string, string> parameter)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection sqlConn = new SqlConnection(ConnectionString))
                {
                    string sql = SPName;
                    using (SqlCommand sqlCmd = new SqlCommand(sql, sqlConn))
                    {
                        sqlCmd.CommandType = CommandType.StoredProcedure;

                        foreach (var item in parameter)
                        {
                            sqlCmd.Parameters.AddWithValue("@" + item.Key, item.Value);
                        }
                        sqlConn.Open();

                        var result = sqlCmd.ExecuteScalar();
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
