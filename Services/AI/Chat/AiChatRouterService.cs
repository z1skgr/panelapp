using Microsoft.EntityFrameworkCore;
using panelapp.Constants;
using panelapp.Data;
using panelapp.Services.AI.Chat.Helpers;
using panelapp.ViewModels.AI.Chat;
using System.Text.Json;
namespace panelapp.Services.AI.Chat
{
    public class AiChatRouterService : IAiChatRouterService
    {
        private readonly IAiChatIntentService _intentService;
        private readonly IOfferAiParser _offerAiParser;
        private readonly IOfferAiOperationParser _operationParser;
        private readonly IOfferAiOperationExecutor _operationExecutor;
        private readonly IOfferAiSummaryService _summaryService;
        private readonly ApplicationDbContext _context;
        public AiChatRouterService(
            IAiChatIntentService intentService,
            IOfferAiParser offerAiParser,
            IOfferAiOperationParser operationParser,
            IOfferAiOperationExecutor operationExecutor,
            IOfferAiSummaryService summaryService,
            ApplicationDbContext context)
        {
            _intentService = intentService;
            _offerAiParser = offerAiParser;
            _operationParser = operationParser;
            _operationExecutor = operationExecutor;
            _summaryService = summaryService;
            _context = context;
        }

        public async Task<AiChatResponseViewModel> HandleAsync(
            AiChatRequestViewModel model,
            CancellationToken cancellationToken = default)
        {
            var message = model.Message?.Trim();

            if (string.IsNullOrWhiteSpace(message))
            {
                return new AiChatResponseViewModel
                {
                    Message = "Δεν δόθηκε μήνυμα.",
                    ResponseType = AiResponseTypes.Error
                };
            }



            var intent = _intentService.DetectIntent(message);
            switch (intent.IntentType)
            {
                case AiIntentTypes.OfferOperation:
                    return await HandleOfferOperationAsync(
                        model,
                        cancellationToken);

                case AiIntentTypes.OfferCreate:
                    return await HandleOfferCreateAsync(
                        model,
                        cancellationToken);

                case AiIntentTypes.OfferSummary:
                    return await HandleOfferSummaryAsync(model, cancellationToken);

                case AiIntentTypes.MaterialSearch:
                    return await HandleMaterialSearchAsync(model, cancellationToken);

                case AiIntentTypes.OutOfScope:
                    return new AiChatResponseViewModel
                    {
                        Message = intent.Message,
                        ResponseType = AiResponseTypes.OutOfScope
                    };

                default:
                    return new AiChatResponseViewModel
                    {
                        Message = "Μπορώ να βοηθήσω με προσφορές, υλικά και αλλαγές σε προσφορές.",
                        ResponseType = AiResponseTypes.Help
                    };
            }
        }


        private async Task<AiChatResponseViewModel> HandleOfferCreateAsync(
    AiChatRequestViewModel model,
    CancellationToken cancellationToken)
        {
            var draft = await _offerAiParser.ParseAsync(
                model.Message,
                cancellationToken);

            var serializedDraft = JsonSerializer.Serialize(
                draft,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            return new AiChatResponseViewModel
            {
                Message = "Έφτιαξα ένα draft προσφοράς από την περιγραφή σου. Πριν δημιουργηθεί, πρέπει να ελέγξουμε πελάτη, υλικά, ποσότητες και κόστη στο preview.",
                ResponseType = AiResponseTypes.OfferPreview,
                RequiresConfirmation = true,
                ActionUrl = "/AI/OfferPreviewFromDraft",
                ActionLabel = "Άνοιγμα Preview",
                SerializedDraft = serializedDraft
            };
        }


        private async Task<AiChatResponseViewModel> HandleOfferOperationAsync(
    AiChatRequestViewModel model,
    CancellationToken cancellationToken)
        {
            if (!model.OfferId.HasValue || model.OfferId.Value <= 0)
            {
                return new AiChatResponseViewModel
                {
                    Message = "Για να αλλάξω υλικό σε προσφορά, άνοιξε πρώτα τα Details της προσφοράς ή γράψε μου τον κωδικό προσφοράς.",
                    ResponseType = AiResponseTypes.OfferOperationMissingContext
                };
            }

            var operation = await _operationParser.ParseAsync(
                model.Message,
                cancellationToken);

            var resultMessage = await _operationExecutor.ExecuteAsync(
                model.OfferId.Value,
                operation,
                cancellationToken);

            var success =
                !resultMessage.StartsWith("Δεν ") &&
                !resultMessage.StartsWith("Λείπει") &&
                !resultMessage.Contains("δεν υποστηρίζεται");

            return new AiChatResponseViewModel
            {
                Message = success
                    ? $"{resultMessage}\n\nΚάνε ανανέωση στη σελίδα για να δεις την αλλαγή."
                    : resultMessage,
                ResponseType = success
                    ? AiResponseTypes.OfferOperationSuccess
                    : AiResponseTypes.OfferOperationFailed
            };
        }
        private async Task<AiChatResponseViewModel> HandleOfferSummaryAsync(
    AiChatRequestViewModel model,
    CancellationToken cancellationToken)
        {
            if (!model.OfferId.HasValue || model.OfferId.Value <= 0)
            {
                return new AiChatResponseViewModel
                {
                    Message = "Για να κάνω σύνοψη προσφοράς, άνοιξε πρώτα τα Details της προσφοράς.",
                    ResponseType = AiResponseTypes.OfferSummaryMissingContext
                };
            }

            var summary = await _summaryService.GenerateSummaryAsync(
                model.OfferId.Value,
                cancellationToken);

            return new AiChatResponseViewModel
            {
                Message = summary,
                ResponseType = AiResponseTypes.OfferSummary
            };
        }


        private async Task<AiChatResponseViewModel> HandleMaterialSearchAsync(
    AiChatRequestViewModel model,
    CancellationToken cancellationToken)
        {
            var term = AiMaterialSearchHelper.ExtractMaterialSearchTerm(model.Message);

            if (string.IsNullOrWhiteSpace(term))
            {
                return new AiChatResponseViewModel
                {
                    Message = "Γράψε μου τι υλικό θέλεις να ψάξω, π.χ. «βρες υλικό ODE-3-120023-1F12».",
                    ResponseType = AiResponseTypes.MaterialSearch
                };
            }

            var normalizedTerm = AiMaterialSearchHelper.NormalizeMaterialText(term);

            var candidates = await _context.Materials
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Where(x => x.Active)
                .Take(500)
                .ToListAsync(cancellationToken);

            var materials = candidates
                .Where(x =>
                    AiMaterialSearchHelper.NormalizeMaterialText(x.MaterialCode).Contains(normalizedTerm) ||
                    AiMaterialSearchHelper.NormalizeMaterialText(x.Description).Contains(normalizedTerm) ||
                    (x.Supplier != null && AiMaterialSearchHelper.NormalizeMaterialText(x.Supplier.SupplierName).Contains(normalizedTerm)))
                .OrderBy(x => x.MaterialCode)
                .Take(5)
                .Select(x => new
                {
                    x.MaterialCode,
                    x.Description,
                    x.CurrentPrice,
                    SupplierName = x.Supplier != null ? x.Supplier.SupplierName : "-"
                })
                .ToList();

            if (!materials.Any())
            {
                return new AiChatResponseViewModel
                {
                    Message = $"Δεν βρήκα υλικό που να ταιριάζει με «{term}».",
                    ResponseType = AiResponseTypes.MaterialSearch
                };
            }

            var lines = materials.Select(x =>
                $"• {x.MaterialCode} — {x.Description}\n  Προμηθευτής: {x.SupplierName} · Τιμή: {x.CurrentPrice:N2} €");

            return new AiChatResponseViewModel
            {
                Message = "Βρήκα τα παρακάτω υλικά:\n\n" + string.Join("\n\n", lines),
                ResponseType = AiResponseTypes.MaterialSearch
            };
        }





    }
}