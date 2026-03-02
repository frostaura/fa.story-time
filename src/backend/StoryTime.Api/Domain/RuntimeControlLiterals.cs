namespace StoryTime.Api.Domain;

public static class CatalogProviders
{
    public const string InMemory = "InMemory";
    public const string FileSystem = "FileSystem";
}

public static class CheckoutProviderModes
{
    public const string InMemory = "InMemory";
    public const string External = "External";
}

public static class ParentGateAssertionTypes
{
    public const string WebAuthnGet = "webauthn.get";
}

public static class CorsPolicies
{
    public const string Frontend = "frontend";
}
