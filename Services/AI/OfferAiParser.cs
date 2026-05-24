using Microsoft.Extensions.Options;
using panelapp.ViewModels.AiOffers;
using System.Text;
using System.Text.Json;

namespace panelapp.Services.AI
{
    public class OfferAiParser : IOfferAiParser
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;

        public OfferAiParser(
            HttpClient httpClient,
            IOptions<GeminiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<OfferAiDraftViewModel> ParseAsync(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            var systemPrompt = """
You are an electrical panel quotation assistant.

Extract structured quotation data from the user message.

Return ONLY valid JSON.

Rules:
- No markdown
- No explanations
- No comments
- No ```json blocks
- Prefer exact material/cabinet codes when present
- Keep supplierName empty unless the user explicitly mentions supplier
- discountPercent must always be 0 unless user explicitly gives discount
- laborCost and profitAmount must be numeric
- extraItems are custom items that do not exist in catalog
- unit must be only: pcs or meters

JSON structure:

{
  "customerName": "string",
  "description": "string",
  "materials": [
    {
      "supplierName": "",
      "codeOrDescription": "string",
      "quantity": number,
      "discountPercent": 0
    }
  ],
  "cabinets": [
    {
      "supplierName": "",
      "codeOrDescription": "string",
      "quantity": number,
      "discountPercent": 0
    }
  ],
  "extraItems": [
    {
      "itemCode": "string",
      "description": "string",
      "unit": "pcs",
      "quantity": number,
      "unitPrice": number,
      "discountPercent": 0
    }
  ],
  "laborCost": number,
  "profitAmount": number
}
""";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = $"{systemPrompt}\n\nUSER:\n{prompt}"
                            }
                        }
                    }
                }
            };

            var requestJson = JsonSerializer.Serialize(requestBody);

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("Gemini API key is missing. Check user-secrets Gemini:ApiKey.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent");

            request.Headers.Add("X-goog-api-key", _options.ApiKey);

            request.Content = new StringContent(
                requestJson,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Gemini error {(int)response.StatusCode}: {error}");
            }

            //response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            using var document = JsonDocument.Parse(responseContent);

            var text = document
                .RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                return new OfferAiDraftViewModel();
            }

            text = text
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var draft = JsonSerializer.Deserialize<OfferAiDraftViewModel>(text, options)
                        ?? new OfferAiDraftViewModel();

            draft.Materials ??= new();
            draft.Cabinets ??= new();
            draft.ExtraItems ??= new();

            return draft;
        }
    }
}