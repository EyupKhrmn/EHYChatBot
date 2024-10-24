using System.Diagnostics;
using EHYChatBot;


const string apiKey =
    "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";

string threadId = await AskGpt.CreateThreadAsync(apiKey);

string assistantId = "asst_K2eT2OL4aWiqTDZENZ5KXGkJ";

while (true)
{
    Console.WriteLine("Ask me anything!");
    string promt = Console.ReadLine();

    string asistantResponse = await AskGpt.CallAssistantAsync(apiKey, assistantId, threadId, promt);
    Console.WriteLine(asistantResponse);
}


