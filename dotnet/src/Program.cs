using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using static System.Environment;


var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

string azureOpenAIEndpoint = configuration["AZURE_OPENAI_ENDPOINT"];
string azureOpenAIKey = configuration["AZURE_OPENAI_API_KEY"];
string deploymentName = configuration["AZURE_OPENAI_DEPLOYMENT_NAME"];
//string deploymentName = "gpt-35-turbo-16k";
string searchEndpoint = configuration["AZURE_AI_SEARCH_ENDPOINT"];
string searchKey = configuration["AZURE_AI_SEARCH_API_KEY"];
string searchIndex = configuration["AZURE_AI_SEARCH_INDEX"];
//string searchIndex = "azureblob-index";

var client = new OpenAIClient(new Uri(azureOpenAIEndpoint), new AzureKeyCredential(azureOpenAIKey));

var chatCompletionsOptions = new ChatCompletionsOptions()
{
    Messages =
    {
        new ChatRequestUserMessage("What are my available health plans?"),
    },
    AzureExtensionsOptions = new AzureChatExtensionsOptions()
    {
        Extensions =
        {
            new AzureCognitiveSearchChatExtensionConfiguration()
            {
                SearchEndpoint = new Uri(searchEndpoint),
                Key = searchKey,
                IndexName = searchIndex,
            },
        }
    },
    DeploymentName = deploymentName
};

Response<ChatCompletions> response = client.GetChatCompletions(chatCompletionsOptions);

ChatResponseMessage responseMessage = response.Value.Choices[0].Message;

Console.WriteLine($"Message from {responseMessage.Role}:");
Console.WriteLine("===");
Console.WriteLine(responseMessage.Content);
Console.WriteLine("===");

Console.WriteLine($"Context information (e.g. citations) from chat extensions:");
Console.WriteLine("===");
foreach (ChatResponseMessage contextMessage in responseMessage.AzureExtensionsContext.Messages)
{
    string contextContent = contextMessage.Content;
    try
    {
        var contextMessageJson = JsonDocument.Parse(contextMessage.Content);
        contextContent = JsonSerializer.Serialize(contextMessageJson, new JsonSerializerOptions()
        {
            WriteIndented = true,
        });
    }
    catch (JsonException)
    {}
    Console.WriteLine($"{contextMessage.Role}: {contextContent}");
}
Console.WriteLine("===");