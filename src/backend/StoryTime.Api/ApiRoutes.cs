namespace StoryTime.Api;

public sealed class ApiRoutes
{
    public string HomeStatus { get; set; } = "";

    public string SubscriptionWebhook { get; set; } = "";

    public string SubscriptionPaywall { get; set; } = "";

    public string SubscriptionCheckoutSession { get; set; } = "";

    public string SubscriptionCheckoutComplete { get; set; } = "";

    public string ParentGateRegister { get; set; } = "";

    public string ParentGateChallenge { get; set; } = "";

    public string ParentGateVerify { get; set; } = "";

    public string ParentSettings { get; set; } = "";

    public string StoriesGenerate { get; set; } = "";

    public string StoryApprove { get; set; } = "";

    public string StoryFavorite { get; set; } = "";

    public string Library { get; set; } = "";

    public string LibraryStorageAudit { get; set; } = "";
}
