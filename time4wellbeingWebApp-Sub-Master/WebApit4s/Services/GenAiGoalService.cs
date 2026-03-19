using OpenAI;
using OpenAI.Chat;
using Org.BouncyCastle.Asn1.Crmf;
using System.Data;



public class GenAiGoalService
{
    private readonly ChatClient _chatClient;

    public GenAiGoalService(IConfiguration config)
    {
        string apiKey = config["OpenAI:ApiKey"];
        _chatClient = new ChatClient(model: "gpt-4", apiKey: apiKey);
    }

    public async Task<string> GeneratePersonalisedGoalAsync(string healthCategory, string issue, int score, int age)
    {
        ChatCompletion completion = await _chatClient.CompleteChatAsync(
            new List<ChatMessage>
            {
                new SystemChatMessage("You are a child wellbeing coach. Provide kind, age-appropriate personalised goals."),
                new UserChatMessage($"A child aged {age} has a low score of {score} in {healthCategory} due to {issue}. Suggest a short goal.")
            });

        return completion.Content.FirstOrDefault()?.Text ?? "No goal generated.";
    }
}

