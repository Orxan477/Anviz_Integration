using System.Text;
using System.Text.Json;
using Anviz_Integration_Api.DTOs;
using Anviz_Integration_Api.Model;
using Anviz_Integration_Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Anviz_Integration_Api.Services.Implementations
{
    public class AnvizService : IAnvizService
    {
        private static readonly HttpClient client = new HttpClient();
        public string webhookUrl;
        public StringContent requestBodyContent;
        public HttpResponseMessage response;
        public String responseContent;
        private IConfiguration _configuration;
        private AppDbContext _context;

        public AnvizService(IConfiguration configuration,AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }
        //public string mainWebhookUrl =;

        public async Task GetToken()
        {
            var tokenRequest = new
            {
                header = new
                {
                    nameSpace = "authorize.token",
                    nameAction = "token",
                    version = "1.0",
                    requestId = Guid.NewGuid().ToString(),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
                },
                payload = new
                {
                    api_key = _configuration.GetSection("Anviz")["ApiKey"],
                    api_secret = _configuration.GetSection("Anviz")["ApiSecret"]
                }
            };
            var tokenResponse = await SendPostRequest(tokenRequest);
            var valueProperty = tokenResponse.Value as JsonElement?;
            var token = valueProperty?.GetProperty("payload")
                                      .GetProperty("token");
            await GetRecord(Convert.ToString(token));
        }

        public async Task GetRecord(string tokenRequestCustom)
        {
            var time = GetTime();
            var recordRequest = new
            {
                header = new
                {
                    nameSpace = "attendance.record",
                    nameAction = "getrecord",
                    version = "1.0",
                    requestId = Guid.NewGuid().ToString(),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
                },
                authorize = new
                {
                    type = "token",
                    token = tokenRequestCustom,
                },
                payload = new
                {
                    begin_time = time.Start,
                    end_time = time.End,
                    workno = "",
                    order = "asc",
                    page = "1",
                    per_page = "100"
                }
            };
            var recordResponse = await SendPostRequest(recordRequest);
            var valueProperty = recordResponse.Value as JsonElement?;
            var list = valueProperty?.GetProperty("payload")
                                        .GetProperty("list");
            var list_json = (JsonElement)list;
            var lastItem = list_json.EnumerateArray()
                                    .LastOrDefault()
                                    .GetProperty("employee")
                                    .GetProperty("workno");
            var id = Convert.ToString(lastItem);
            var id_num = Convert.ToInt32(id);
            await Bitrix(id_num);
        }

        public async Task Bitrix(int id)
        {
            string webhook = await IsExpired(id);
            if (webhookUrl != "null")
            {
                webhookUrl = webhook + "timeman.status.json";
                var requestBody = new
                {
                    USER_ID = id
                };
                var requestBodyJson = JsonConvert.SerializeObject(requestBody);
                requestBodyContent = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");
                response = await client.PostAsync(webhookUrl, requestBodyContent);
                responseContent = await response.Content.ReadAsStringAsync();
                var jObject = JObject.Parse(responseContent);
                if ((string)jObject["result"]["STATUS"] == "OPENED")
                    webhookUrl = webhook + "timeman.close.json";

                else
                    webhookUrl = webhook + "timeman.open.json";

                response = await client.PostAsync(webhookUrl, requestBodyContent);
                responseContent = await response.Content.ReadAsStringAsync();
            }
        }
        private async Task<string> IsExpired(int id)
        {
            var model = await _context.Logs.Where(x => x.UserId == id).FirstOrDefaultAsync();

            DateTime now = DateTime.Now;
            DateTime expireDate = now.AddMinutes(3);
            if (model != null)
            {
                if (DateTime.Now > model.ExpireDate)
                {
                    model.ExpireDate = expireDate;
                    await _context.SaveChangesAsync();
                    return model.WebhookUrl;
                }
                else
                {
                    return "null";
                }
            }
            else return "null";
            
        }
        private DateDto GetTime() {
            DateTime currentDate = DateTime.UtcNow;
            DateTime startOfDay = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 0, 0, 0, DateTimeKind.Utc);
            DateTime endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            return new DateDto()
            {
                Start = startOfDay.ToString("yyyy-MM-ddTHH:mm:ss"),
                End = endOfDay.ToString("yyyy-MM-ddTHH:mm:ss"),
            };
        }
        private async Task<JsonResult> SendPostRequest(object requestBody)
        {
            var jsonRequest = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_configuration.GetSection("Anviz")["ApiUrl"], content);
            using var responseStream = await response.Content.ReadAsStreamAsync();
            return new JsonResult(await System.Text.Json.JsonSerializer.DeserializeAsync<dynamic>(responseStream));
               
        }

    }
}

