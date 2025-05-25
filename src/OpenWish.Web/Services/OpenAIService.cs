namespace OpenWish.Web.Services;

public interface IOpenAIService
{
    Task<string> GenerateCompletionToJsonAsync(string model, List<string> prompts, List<string>? systemPrompts = null, double temperature = 0.7, int maxTokens = 1000);
}

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;

    public OpenAIService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("OpenAI");
    }

    public async Task<string> GenerateCompletionToJsonAsync(string model, List<string> prompts, List<string>? systemPrompts = null, double temperature = 0.7, int maxTokens = 1000)
    {
        // https://platform.openai.com/docs/api-reference/chat/create#chat-create-response_format
        // check both prompts and systemPrompts to ensure they include a JSON response format. one of them must include it (but not necessarily both)
        if (!prompts.Any(x => x.Contains("JSON", StringComparison.OrdinalIgnoreCase)) && (systemPrompts == null || !systemPrompts.Any(x => x.Contains("JSON", StringComparison.OrdinalIgnoreCase))))
        {
            throw new ArgumentException("Prompt (user or system) must instruct including a JSON response format");
        }

        var messages = new List<Message>();

        if (systemPrompts != null)
        {
            foreach (var systemPrompt in systemPrompts)
            {
                messages.Add(new Message { Role = "system", Content = systemPrompt });
            }
        }
        if (prompts.Count != 0)
        {
            foreach (var prompt in prompts)
            {
                messages.Add(new Message { Role = "user", Content = prompt });
            }
        }

        var requestBody = new
        {
            model,
            messages,
            temperature,
            max_tokens = maxTokens,
            response_format = new { type = "json_object" }
        };

        var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();

        return responseContent;
    }
}

public class Message
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

public class Choice
{
    public int Index { get; set; }
    public Message? Message { get; set; }
}

public class ChatResponse
{
    public string Id { get; set; }
    public string Object { get; set; }
    public string Model { get; set; }
    public List<Choice> Choices { get; set; }
}