using panelapp.Constants;
using panelapp.ViewModels.AI.Chat;
namespace panelapp.Services.AI.Chat
{
    public class AiChatIntentService : IAiChatIntentService
    {

        public AiChatIntentResult DetectIntent(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return new AiChatIntentResult
                {
                    IntentType = AiIntentTypes.Unknown,
                    Message = "Δεν δόθηκε μήνυμα."
                };
            }

            var text = message.Trim().ToLowerInvariant();

            if (LooksLikeOfferOperation(text))
            {
                return new AiChatIntentResult
                {
                    IntentType = AiIntentTypes.OfferOperation,
                    Message = "Εντοπίστηκε αλλαγή σε προσφορά."
                };
            }

            if (LooksLikeSummary(text))
            {
                return new AiChatIntentResult
                {
                    IntentType = AiIntentTypes.OfferSummary,
                    Message = "Εντοπίστηκε αίτημα σύνοψης."
                };
            }

            if (LooksLikeMaterialSearch(text))
            {
                return new AiChatIntentResult
                {
                    IntentType = AiIntentTypes.MaterialSearch,
                    Message = "Εντοπίστηκε αναζήτηση υλικού."
                };
            }

            if (LooksLikeOfferCreate(text))
            {
                return new AiChatIntentResult
                {
                    IntentType = AiIntentTypes.OfferCreate,
                    Message = "Εντοπίστηκε δημιουργία προσφοράς."
                };
            }

            if (LooksLikeHelp(text))
            {
                return new AiChatIntentResult
                {
                    IntentType = AiIntentTypes.Help,
                    Message = "Εντοπίστηκε αίτημα βοήθειας."
                };
            }

            return new AiChatIntentResult
            {
                IntentType = AiIntentTypes.OutOfScope,
                Message = "Μπορώ να βοηθήσω μόνο με εργασίες του Panel App: προσφορές, υλικά, πίνακες, αναζήτηση υλικών και αλλαγές μέσα σε ανοιχτή προσφορά."
            };
        }

        private static bool LooksLikeOfferOperation(string text)
        {
            return text.Contains("άλλαξε") ||
                   text.Contains("αλλαξε") ||
                   text.Contains("ποσότητα") ||
                   text.Contains("ποσοτητα") ||
                   text.Contains("τεμ") ||
                   text.Contains("έκπτωση") ||
                   text.Contains("εκπτωση") ||
                   text.Contains("βγάλε") ||
                   text.Contains("βγαλε") ||
                   text.Contains("αφαίρεσε") ||
                   text.Contains("αφαιρεσε") ||
                   text.Contains("διαγραφή") ||
                   text.Contains("διαγραφη");
        }

        private static bool LooksLikeMaterialSearch(string text)
        {
            return text.Contains("αναζήτηση υλικού") ||
                   text.Contains("αναζητηση υλικου") ||
                   text.Contains("ψάξε υλικό") ||
                   text.Contains("ψαξε υλικο") ||
                   text.Contains("βρες υλικό") ||
                   text.Contains("βρες υλικο") ||
                   text.Contains("material");
        }

        private static bool LooksLikeOfferCreate(string text)
        {
            return text.Contains("δημιούργησε προσφορά") ||
                   text.Contains("δημιουργησε προσφορα") ||
                   text.Contains("νέα προσφορά") ||
                   text.Contains("νεα προσφορα") ||
                   text.Contains("create offer") ||
                   text.Contains("draft");
        }

        private static bool LooksLikeSummary(string text)
        {
            return text.Contains("σύνοψη") ||
                   text.Contains("συνοψη") ||
                   text.Contains("περίληψη") ||
                   text.Contains("περιληψη") ||
                   text.Contains("summary");
        }

        private static bool LooksLikeHelp(string text)
        {
            return text.Contains("βοήθεια") ||
                   text.Contains("βοηθεια") ||
                   text.Contains("τι μπορείς") ||
                   text.Contains("τι μπορεις") ||
                   text.Contains("help");
        }
    }
}