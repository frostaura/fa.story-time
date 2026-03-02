using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Buffers.Binary;
using Microsoft.Extensions.Options;
using StoryTime.Api.Contracts;
using StoryTime.Api.Domain;
using StoryTime.Api.Services;

namespace StoryTime.Api.Tests.Unit;

public sealed class ParentSettingsServiceTests
{
    private readonly ParentSettingsService _service = new(Options.Create(StoryTimeOptionsFactory.Create()));

    [Fact]
    public void CreateChallenge_UsesConfiguredChallengeByteLength()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.ParentGate.ChallengeByteLength = 48;
        var service = new ParentSettingsService(Options.Create(options));

        var challenge = service.CreateChallenge("sized-user", DateTimeOffset.UtcNow);
        var challengeBytes = DecodeBase64Url(challenge.Challenge);

        Assert.Equal(48, challengeBytes.Length);
    }

    [Fact]
    public void VerifyGate_RequiresChallengeAndAssertion()
    {
        var now = DateTimeOffset.UtcNow;
        var (credentialId, publicKey, privateKey) = CreateCredentialPair();
        var registered = _service.RegisterCredential("parent-user", credentialId, publicKey);
        Assert.True(registered);

        var challenge = _service.CreateChallenge("parent-user", now);

        var missingAssertion = _service.VerifyGate(
            "parent-user",
            challenge.ChallengeId,
            new ParentGateAssertion("", "", "", "", ""),
            now.AddSeconds(5));
        Assert.Null(missingAssertion);

        var assertion = BuildAssertion(challenge.Challenge, credentialId, privateKey);
        var session = _service.VerifyGate("parent-user", challenge.ChallengeId, assertion, now.AddSeconds(10));
        Assert.NotNull(session);
    }

    [Fact]
    public void UpdateSettings_RequiresAuthorizedSession()
    {
        var now = DateTimeOffset.UtcNow;
        var (credentialId, publicKey, privateKey) = CreateCredentialPair();
        var registered = _service.RegisterCredential("settings-user", credentialId, publicKey);
        Assert.True(registered);

        var challenge = _service.CreateChallenge("settings-user", now);
        var assertion = BuildAssertion(challenge.Challenge, credentialId, privateKey);
        var session = _service.VerifyGate("settings-user", challenge.ChallengeId, assertion, now.AddSeconds(1));

        Assert.NotNull(session);

        var updated = _service.UpdateSettings(
            "settings-user",
            session!.GateToken,
            notificationsEnabled: true,
            analyticsEnabled: false,
            now.AddSeconds(2));

        Assert.NotNull(updated);
        Assert.True(updated!.NotificationsEnabled);
        Assert.False(updated.AnalyticsEnabled);
    }

    [Fact]
    public void VerifyGate_RejectsMismatchedRelyingPartyHash()
    {
        var now = DateTimeOffset.UtcNow;
        var (credentialId, publicKey, privateKey) = CreateCredentialPair();
        var registered = _service.RegisterCredential("rp-user", credentialId, publicKey);
        Assert.True(registered);

        var challenge = _service.CreateChallenge("rp-user", now);
        var assertion = BuildAssertion(challenge.Challenge, credentialId, privateKey, rpId: "malicious.example");
        var session = _service.VerifyGate("rp-user", challenge.ChallengeId, assertion, now.AddSeconds(5));

        Assert.Null(session);
    }

    [Fact]
    public void VerifyGate_RejectsReplayedSignatureCounter()
    {
        var now = DateTimeOffset.UtcNow;
        var (credentialId, publicKey, privateKey) = CreateCredentialPair();
        var registered = _service.RegisterCredential("counter-user", credentialId, publicKey);
        Assert.True(registered);

        var firstChallenge = _service.CreateChallenge("counter-user", now);
        var firstAssertion = BuildAssertion(firstChallenge.Challenge, credentialId, privateKey, signCount: 10);
        var firstSession = _service.VerifyGate("counter-user", firstChallenge.ChallengeId, firstAssertion, now.AddSeconds(1));
        Assert.NotNull(firstSession);

        var secondChallenge = _service.CreateChallenge("counter-user", now.AddSeconds(2));
        var replayedAssertion = BuildAssertion(secondChallenge.Challenge, credentialId, privateKey, signCount: 10);
        var replayedSession = _service.VerifyGate("counter-user", secondChallenge.ChallengeId, replayedAssertion, now.AddSeconds(3));

        Assert.Null(replayedSession);
    }

    private static ParentGateAssertion BuildAssertion(
        string challenge,
        string credentialId,
        byte[] privateKey,
        string rpId = "localhost",
        uint signCount = 1)
    {
        var clientDataRaw = JsonSerializer.Serialize(new
        {
            type = ParentGateAssertionTypes.WebAuthnGet,
            challenge,
            origin = "http://localhost"
        });
        var clientDataBytes = Encoding.UTF8.GetBytes(clientDataRaw);
        var authenticatorDataBytes = BuildAuthenticatorData(rpId, signCount);
        var clientDataHash = SHA256.HashData(clientDataBytes);

        var signedPayload = new byte[authenticatorDataBytes.Length + clientDataHash.Length];
        Buffer.BlockCopy(authenticatorDataBytes, 0, signedPayload, 0, authenticatorDataBytes.Length);
        Buffer.BlockCopy(clientDataHash, 0, signedPayload, authenticatorDataBytes.Length, clientDataHash.Length);

        using var key = ECDsa.Create();
        key.ImportECPrivateKey(privateKey, out _);
        var signature = key.SignData(signedPayload, HashAlgorithmName.SHA256);

        return new ParentGateAssertion(
            CredentialId: credentialId,
            ClientDataJson: Convert.ToBase64String(clientDataBytes),
            AuthenticatorData: Convert.ToBase64String(authenticatorDataBytes),
            Signature: Convert.ToBase64String(signature),
            Type: ParentGateAssertionTypes.WebAuthnGet);
    }

    private static byte[] BuildAuthenticatorData(string rpId, uint signCount)
    {
        var rpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(rpId));
        var authenticatorData = new byte[37];
        Buffer.BlockCopy(rpIdHash, 0, authenticatorData, 0, rpIdHash.Length);
        authenticatorData[32] = 0x01;
        BinaryPrimitives.WriteUInt32BigEndian(authenticatorData.AsSpan(33, 4), signCount);
        return authenticatorData;
    }

    private static (string CredentialId, string PublicKey, byte[] PrivateKey) CreateCredentialPair()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var credentialId = Guid.NewGuid().ToString("N");
        var publicKey = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());
        var privateKey = key.ExportECPrivateKey();
        return (credentialId, publicKey, privateKey);
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        var padding = normalized.Length % 4;
        if (padding > 0)
        {
            normalized = normalized.PadRight(normalized.Length + (4 - padding), '=');
        }

        return Convert.FromBase64String(normalized);
    }
}
