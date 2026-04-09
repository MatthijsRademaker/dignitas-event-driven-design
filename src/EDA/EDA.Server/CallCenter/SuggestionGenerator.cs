namespace EDA.Server.CallCenter;

public static class SuggestionGenerator
{
    public static IReadOnlyList<SuggestionSeed> FromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [new SuggestionSeed("Ask a clarifying question.", "General")];
        }

        var normalized = text.ToLowerInvariant();
        var suggestions = new List<SuggestionSeed>();

        AddWhen(normalized, suggestions, ["refund", "charge", "billing"], "Confirm refund options and eligibility.", "Billing");
        AddWhen(normalized, suggestions, ["cancel", "terminate"], "Review cancellation policy and confirm intent.", "Account");
        AddWhen(normalized, suggestions, ["angry", "upset", "frustrated"], "Acknowledge frustration and summarize next steps.", "De-escalation");
        AddWhen(normalized, suggestions, ["manager", "supervisor"], "Prepare escalation and capture the reason.", "Escalation");
        AddWhen(normalized, suggestions, ["late", "delay", "shipping"], "Check delivery status and offer updated ETA.", "Delivery");
        AddWhen(normalized, suggestions, ["password", "login", "access"], "Offer account recovery and verify identity.", "Account");

        if (suggestions.Count == 0)
        {
            suggestions.Add(new SuggestionSeed("Ask a clarifying question.", "General"));
        }

        return suggestions;
    }

    private static void AddWhen(
        string normalized,
        List<SuggestionSeed> suggestions,
        IReadOnlyList<string> keywords,
        string text,
        string category)
    {
        if (keywords.Any(normalized.Contains))
        {
            suggestions.Add(new SuggestionSeed(text, category));
        }
    }
}

public sealed record SuggestionSeed(string Text, string Category);
