
using UpdatedAttendanceTask.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UpdatedAttendanceTask.Controllers
{
    public class HomeController : Controller
    {
        private const string API_KEY = "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";
        private static readonly HttpClient client = new HttpClient();

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var model = new Model();
            if (HttpContext.Session.TryGetValue("GeneratedResponse", out var generatedResponse))
            {
                model.Response = System.Text.Encoding.UTF8.GetString(generatedResponse);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Model model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Prompt) && model.File == null)
                {
                    ModelState.AddModelError("", "Please provide a prompt or upload a file.");
                    var model1 = new Model { Response = "No prompt provided." };
                    return View("Index", model1);
                }

                string fileData = null;

                if (model.File != null)
                {
                    using (var reader = new StreamReader(model.File.OpenReadStream()))
                    {
                        // Read data from the uploaded file
                        fileData = await reader.ReadToEndAsync();
                    }
                }

                // Prepare the data for OpenAI API request
                var messages = new List<dynamic>
        {
            new
            {
                role = "system",
                content = " "
            },
            new
            {
                role = "user",
                content = model.Prompt + (!string.IsNullOrEmpty(fileData) ? "\n" + fileData : "")
            }
        };

                var options = new
                {
                    model = "gpt-3.5-turbo",
                    messages,
                    max_tokens = 3500,
                    temperature = 0.2
                };

                var json = JsonConvert.SerializeObject(options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_KEY);

                // Send request to OpenAI API for text analysis
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();

                dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                string result = jsonResponse.choices[0].message.content;

                // Assign the output result to the model
                model.Response = result;

                // Save the response to Session for displaying it in the Index view
                HttpContext.Session.Set("GeneratedResponse", Encoding.UTF8.GetBytes(model.Response));

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                Debug.WriteLine(ex.Message);
                return Content("An error occurred.");
            }
        }


        public IActionResult DownloadResponse()
        {
            if (HttpContext.Session.TryGetValue("GeneratedResponse", out var generatedResponse))
            {
                var responseCsv = Encoding.UTF8.GetString(generatedResponse);
                var responseBytes = Encoding.UTF8.GetBytes(responseCsv);

                var responseFileName = $"{Guid.NewGuid().ToString()}.csv";

                var contentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileNameStar = responseFileName,
                    FileName = responseFileName
                };
                Response.Headers.Add(HeaderNames.ContentDisposition, contentDisposition.ToString());

                return new FileContentResult(responseBytes, "text/csv");
            }

            return Content("Response not found.");
        }
    }
}
