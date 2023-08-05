//Pseudo code
// Controller HomeController
//     // Constants
//     API_KEY = "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"
//     MAX_TOKENS = 3500
//     TEMPERATURE = 0.2

//     // Dependencies
//     ILogger _logger

//     // Constructor
//     HomeController(ILogger logger)
//         _logger = logger

//     // Action: Index
//     Action Index()
//         model = new Model()
//         If Session contains "GeneratedResponse" Then
//             generatedResponse = Get the value from Session with key "GeneratedResponse"
//             model.Response = Convert generatedResponse to string using UTF8 encoding
//         End If

//         Return View with model
//     End Action

//     // Action: Create
//     Action Create(Model model)
//         Try
//             If model.Prompt is empty AND model.File is null Then
//                 AddModelError("Please provide a prompt or upload a file.")
//                 model1 = new Model with Response = "No prompt provided."
//                 Return View("Index", model1)
//             End If

//             fileData = null
//             If model.File is not null Then
//                 Open a StreamReader with model.File.OpenReadStream()
//                 Read data from the uploaded file using the StreamReader and store it in fileData
//             End If

//             messages = Create a List of dynamic objects
//             Add a dynamic object with role = "system" and content = " " to messages
//             Add a dynamic object with role = "user" and content = model.Prompt + "\n" + fileData (if fileData is not null) to messages

//             options = Create an anonymous object with properties:
//                 model = "gpt-3.5-turbo"
//                 messages = messages
//                 max_tokens = MAX_TOKENS
//                 temperature = TEMPERATURE

//             json = Serialize options to JSON
//             content = Create a StringContent object with json, Encoding.UTF8, and "application/json"

//             Set the Authorization header of client's DefaultRequestHeaders to use the API_KEY

//             response = Send a POST request to "https://api.openai.com/v1/chat/completions" with content using the client
//             Ensure the response is successful (Status code 200)

//             responseBody = Read the response content as a string
//             jsonResponse = Deserialize responseBody as dynamic JSON object
//             result = Get the content property from jsonResponse.choices[0].message as a string

//             model.Response = result

//             Convert model.Response to bytes using UTF8 encoding and store it in generatedResponse
//             Set the generatedResponse in Session with key "GeneratedResponse"

//             Redirect to the Index action
//         Catch Exception as ex
//             Log the error message in _logger
//             Return Content("An error occurred.")
//         End Try
//     End Action

//     // Action: DownloadResponse
//     Action DownloadResponse()
//         If Session contains "GeneratedResponse" Then
//             generatedResponse = Get the value from Session with key "GeneratedResponse"
//             responseCsv = Convert generatedResponse to string using UTF8 encoding

//             Convert responseCsv to bytes using UTF8 encoding and store it in responseBytes
//             Generate a random file name and store it in responseFileName
//             Set the Content-Disposition header to "attachment; filename=" + responseFileName
//             Add the Content-Disposition header to the response

//             Return a FileContentResult with responseBytes and "text/csv" content type
//         Else
//             Return Content("Response not found.")
//         End If
//     End Action
// End Controller



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
