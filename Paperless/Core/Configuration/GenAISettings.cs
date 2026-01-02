namespace Core.Configuration
{
    public class GenAISettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-1.5-flash";
        public int MaxTokens { get; set; } = 500;
        public double Temperature { get; set; } = 0.7;
    }
}

