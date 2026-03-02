namespace StoryTime.Api.Contracts;

public sealed record ParentGateVerifyRequest(string ChallengeId, ParentGateAssertion Assertion);

public sealed record ParentGateAssertion(
    string CredentialId,
    string ClientDataJson,
    string AuthenticatorData,
    string Signature,
    string Type);
