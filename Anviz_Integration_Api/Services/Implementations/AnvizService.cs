using System.Text;
using System.Text.Json;
using Anviz_Integration_Api.DTOs;
using Anviz_Integration_Api.Model;
using Anviz_Integration_Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

//using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Anviz_Integration_Api.Services.Implementations
{
    public class AnvizService : IAnvizService
    {
        private static readonly HttpClient _client = new HttpClient();
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public AnvizService(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

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
                    api_key = _configuration["Anviz:ApiKey"],
                    api_secret = _configuration["Anviz:ApiSecret"]
                }
            };

            var tokenResponse = await SendPostRequest(tokenRequest);
            var valueProperty = tokenResponse.Value as JsonElement?;
            var token = valueProperty?.GetProperty("payload").GetProperty("token").GetString();

            if (!string.IsNullOrEmpty(token))
            {
                await GetRecord(token);
            }
        }

        public async Task GetRecord(string token)
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
                    token
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
            var list = valueProperty?.GetProperty("payload").GetProperty("list");

            if (list?.ValueKind == JsonValueKind.Array)
            {
                var lastItem = list.Value.EnumerateArray().LastOrDefault();
                if (lastItem.ValueKind != JsonValueKind.Undefined)
                {
                    var id = lastItem.GetProperty("employee").GetProperty("workno").GetString();
                    if (int.TryParse(id, out int idNum))
                    {
                        await Bitrix(idNum);
                    }
                }
            }
        }

        public async Task Bitrix(int id)
        {
            string webhook = await IsExpired(id);
            if (webhook != "null")
            {
                await UpdateTimemanStatus(webhook, id);
            }
        }

        private async Task<string> IsExpired(int id)
        {
            var model = await _context.Logs.FirstOrDefaultAsync(x => x.UserId == id);
            DateTime expireDate = DateTime.Now.AddMinutes(3);

            if (model != null && DateTime.Now > model.ExpireDate)
            {
                model.ExpireDate = expireDate;
                await _context.SaveChangesAsync();
                return model.WebhookUrl;
            }
            return "null";
        }

        private async Task UpdateTimemanStatus(string webhook, int userId)
        {
            var requestBody = new { USER_ID = userId };
            var requestBodyJson = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");

            string webhookUrl = $"{webhook}timeman.status.json";
            var response = await _client.PostAsync(webhookUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var jObject = JObject.Parse(responseContent);

            webhookUrl = (string)jObject["result"]["STATUS"] == "OPENED"
                ? $"{webhook}timeman.close.json"
                : $"{webhook}timeman.open.json";

            await _client.PostAsync(webhookUrl, content);
        }

        private DateDto GetTime()
        {
            DateTime startOfDay = DateTime.UtcNow.Date;
            DateTime endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            return new DateDto
            {
                Start = startOfDay.ToString("yyyy-MM-ddTHH:mm:ss"),
                End = endOfDay.ToString("yyyy-MM-ddTHH:mm:ss")
            };
        }

        private async Task<JsonResult> SendPostRequest(object requestBody)
        {
            var jsonRequest = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(_configuration["Anviz:ApiUrl"], content);

            using var responseStream = await response.Content.ReadAsStreamAsync();
            var result = await System.Text.Json.JsonSerializer.DeserializeAsync<dynamic>(responseStream);
            return new JsonResult(result);
        }
    }
}
