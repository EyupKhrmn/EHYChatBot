using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EHYChatBot;

public static class AskGpt
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly string OPEN_AI_BASE_URL = "https://api.openai.com/v1";

    public static async Task<string> CreateThreadAsync(string apiKey)
    {
        string createThreadUrl = $"{OPEN_AI_BASE_URL}/threads";
        var requestBody = new
        {
            /* Include any necessary data for creating a thread */
        };

        try
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, createThreadUrl);
            httpRequestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
            httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v2");
            httpRequestMessage.Content =
                new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            using (var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage))
            {
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    string jsonResponse = await httpResponseMessage.Content.ReadAsStringAsync();
                    JObject threadData = JObject.Parse(jsonResponse);
                    return threadData["id"]?.ToString();
                }
                else
                {
                    Console.WriteLine($"Error: {httpResponseMessage.ReasonPhrase}");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return null;
        }
    }

    public static async Task<string> CallAssistantAsync(string apiKey, string assistantId, string threadId,
        string userPrompt)
    {
        try
        {
            if (string.IsNullOrEmpty(threadId) || string.IsNullOrEmpty(userPrompt))
                throw new ArgumentException("Thread ID and user prompt must not be null or empty.");

            string messageId = await AddMessageToThreadAsync(apiKey, userPrompt, threadId);
            string runId = await RunMessageThreadAsync(apiKey, assistantId, threadId);
            if (string.IsNullOrEmpty(runId))
                throw new InvalidOperationException("Failed to start assistant on the thread.");

            string assistantResponse = await GetAssistantResponseAsync(apiKey, threadId, messageId);
            return assistantResponse ?? "Seems to be a delay in response. Please try again, or try back later.";
        }
        catch (Exception ex)
        {
            // Handle exceptions or log errors here
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    public static async Task<string> AddMessageToThreadAsync(string apiKey, string userPrompt, string threadId)
    {
        string url = $"{OPEN_AI_BASE_URL}/threads/{threadId}/messages";
        var requestBody = new { role = "user", content = userPrompt };

        var response = await SendPostRequestAsync(url, apiKey, requestBody);
        return response?.GetValue("id")?.ToString();
    }

    public static async Task<string> RunMessageThreadAsync(string apiKey, string assistantId, string threadId)
    {
        string url = $"{OPEN_AI_BASE_URL}/threads/{threadId}/runs";
        var requestBody = new { assistant_id = assistantId };

        var response = await SendPostRequestAsync(url, apiKey, requestBody);
        return response?.GetValue("id")?.ToString();
    }

    public static async Task<string> GetAssistantResponseAsync(string apiKey, string threadId, string messageId)
    {
        int maxAttempts = 5;
        int attempts = 0;
        string assistantResponse = null;

        while (attempts < maxAttempts)
        {
            await Task.Delay(4000); // Wait for 4 seconds before checking for a response

            string url = $"{OPEN_AI_BASE_URL}/threads/{threadId}/messages";
            var response = await SendGetRequestAsync(url, apiKey);

            var messages = response?.GetValue("data") as JArray;
            if (messages != null)
            {
                foreach (var message in messages)
                {
                    if (message.Value<string>("role") == "assistant")
                    {
                        assistantResponse = message["content"]?.FirstOrDefault()?["text"]?["value"]?.ToString();
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(assistantResponse))
                break;

            attempts++;
        }

        return assistantResponse;
    }

    public static async Task<JObject> SendPostRequestAsync(string url, string apiKey, object requestBody)
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
        httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v2");
        httpRequestMessage.Content =
            new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
        using (var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage))
        {
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                string jsonResponse = await httpResponseMessage.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<JObject>(jsonResponse);
            }
            else
            {
                Console.WriteLine($"Error: {httpResponseMessage.ReasonPhrase}");
                return null;
            }
        }
    }

    public static async Task<JObject> SendGetRequestAsync(string url, string apiKey)
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
        httpRequestMessage.Headers.Add("OpenAI-Beta", "assistants=v2");
        using (var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage))
        {
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                string jsonResponse = await httpResponseMessage.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<JObject>(jsonResponse);
            }
            else
            {
                Console.WriteLine($"Error: {httpResponseMessage.ReasonPhrase}");
                return null;
            }
        }
    }
}
    
