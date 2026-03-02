using System.Collections.Concurrent;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StoryTime.Api.Contracts;

namespace StoryTime.Api.Services;

public sealed class ParentSettingsService(IOptions<StoryTimeOptions> options) : IParentSettingsService
{
    private readonly StoryTimeOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, ParentState> _states = new(StringComparer.Ordinal);

    public bool RegisterCredential(string softUserId, string credentialId, string publicKey)
    {
        if (string.IsNullOrWhiteSpace(softUserId) || string.IsNullOrWhiteSpace(credentialId) || string.IsNullOrWhiteSpace(publicKey))
        {
            return false;
        }

        if (!TryDecodePublicKey(publicKey, out var keyBytes))
        {
            return false;
        }

        var state = _states.GetOrAdd(softUserId, _ => CreateState());
        lock (state.SyncRoot)
        {
            state.Credentials[credentialId] = keyBytes;
            state.SignatureCounters[credentialId] = 0;
            return true;
        }
    }

    public ParentGateChallenge CreateChallenge(string softUserId, DateTimeOffset now)
    {
        var state = _states.GetOrAdd(softUserId, _ => CreateState());
        lock (state.SyncRoot)
        {
            CleanupExpired(state, now);
            var challengeId = Guid.NewGuid().ToString("N");
            var challengeValue = RandomNumberGenerator.GetBytes(_options.ParentGate.ChallengeByteLength);
            var expiresAt = now.AddMinutes(Math.Max(1, _options.ParentGate.ChallengeTtlMinutes));
            state.Challenges[challengeId] = new ParentChallengeState(challengeValue, expiresAt);
            return new ParentGateChallenge(
                ChallengeId: challengeId,
                Challenge: Base64UrlEncode(challengeValue),
                RpId: _options.ParentGate.RelyingPartyId,
                ExpiresAt: expiresAt);
        }
    }

    public ParentGateSession? VerifyGate(string softUserId, string challengeId, ParentGateAssertion assertion, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(challengeId) || assertion is null)
        {
            return null;
        }

        if (_options.ParentGate.RequireAssertion &&
            (string.IsNullOrWhiteSpace(assertion.CredentialId) ||
             string.IsNullOrWhiteSpace(assertion.ClientDataJson) ||
             string.IsNullOrWhiteSpace(assertion.AuthenticatorData) ||
             string.IsNullOrWhiteSpace(assertion.Signature) ||
             string.IsNullOrWhiteSpace(assertion.Type)))
        {
            return null;
        }

        var state = _states.GetOrAdd(softUserId, _ => CreateState());
        lock (state.SyncRoot)
        {
            CleanupExpired(state, now);
            if (!state.Challenges.TryGetValue(challengeId, out var challengeState) || challengeState.ExpiresAt <= now)
            {
                return null;
            }

            if (!IsAssertionMatch(challengeState, assertion, state))
            {
                return null;
            }

            state.Challenges.Remove(challengeId);

            var gateToken = Guid.NewGuid().ToString("N");
            var expiresAt = now.AddMinutes(Math.Max(1, _options.ParentGate.SessionTtlMinutes));
            state.Sessions[gateToken] = expiresAt;
            return new ParentGateSession(gateToken, expiresAt);
        }
    }

    public ParentSettingsSnapshot? GetSettings(string softUserId, string gateToken, DateTimeOffset now)
    {
        var state = _states.GetOrAdd(softUserId, _ => CreateState());
        lock (state.SyncRoot)
        {
            CleanupExpired(state, now);
            return IsAuthorized(state, gateToken, now)
                ? new ParentSettingsSnapshot(state.NotificationsEnabled, state.AnalyticsEnabled)
                : null;
        }
    }

    public bool IsGateAuthorized(string softUserId, string gateToken, DateTimeOffset now)
    {
        var state = _states.GetOrAdd(softUserId, _ => CreateState());
        lock (state.SyncRoot)
        {
            CleanupExpired(state, now);
            return IsAuthorized(state, gateToken, now);
        }
    }

    public ParentSettingsSnapshot? UpdateSettings(string softUserId, string gateToken, bool notificationsEnabled, bool analyticsEnabled, DateTimeOffset now)
    {
        var state = _states.GetOrAdd(softUserId, _ => CreateState());
        lock (state.SyncRoot)
        {
            CleanupExpired(state, now);
            if (!IsAuthorized(state, gateToken, now))
            {
                return null;
            }

            state.NotificationsEnabled = notificationsEnabled;
            state.AnalyticsEnabled = analyticsEnabled;
            return new ParentSettingsSnapshot(state.NotificationsEnabled, state.AnalyticsEnabled);
        }
    }

    private ParentState CreateState() => new()
    {
        NotificationsEnabled = _options.ParentDefaults.NotificationsEnabled,
        AnalyticsEnabled = _options.ParentDefaults.AnalyticsEnabled
    };

    private bool IsAssertionMatch(
        ParentChallengeState challengeState,
        ParentGateAssertion assertion,
        ParentState state)
    {
        if (!_options.ParentGate.RequireChallengeBoundAssertion)
        {
            return true;
        }

        return _options.ParentGate.RequireRegisteredCredential &&
            IsSignedAssertionMatch(challengeState, assertion, state);
    }

    private bool IsSignedAssertionMatch(ParentChallengeState challengeState, ParentGateAssertion assertion, ParentState state)
    {
        if (!state.Credentials.TryGetValue(assertion.CredentialId, out var keyBytes))
        {
            return false;
        }

        if (keyBytes.Length == 0)
        {
            return false;
        }

        if (!TryDecodeBase64(assertion.ClientDataJson, out var clientDataBytes) ||
            !TryDecodeBase64(assertion.AuthenticatorData, out var authenticatorDataBytes) ||
            !TryDecodeBase64(assertion.Signature, out var signatureBytes))
        {
            return false;
        }

        if (!TryParseAuthenticatorData(authenticatorDataBytes, out var authenticatorData))
        {
            return false;
        }

        WebAuthnClientData? clientData;
        try
        {
            clientData = JsonSerializer.Deserialize<WebAuthnClientData>(
                clientDataBytes,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return false;
        }

        if (clientData is null || !string.Equals(clientData.Type, _options.ParentGate.AssertionType, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(assertion.Type, _options.ParentGate.AssertionType, StringComparison.Ordinal))
        {
            return false;
        }

        if (!ChallengeMatches(clientData.Challenge, challengeState.Value))
        {
            return false;
        }

        if (_options.ParentGate.AllowedOrigins.Count > 0 &&
            !_options.ParentGate.AllowedOrigins.Any(origin =>
                string.Equals(origin, clientData.Origin, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!authenticatorData.UserPresent)
        {
            return false;
        }

        if (_options.ParentGate.RequireUserVerification && !authenticatorData.UserVerified)
        {
            return false;
        }

        if (!IsRelyingPartyMatch(authenticatorData.RelyingPartyHash, _options.ParentGate.RelyingPartyId))
        {
            return false;
        }

        var clientDataHash = SHA256.HashData(clientDataBytes);
        var signedPayload = new byte[authenticatorDataBytes.Length + clientDataHash.Length];
        Buffer.BlockCopy(authenticatorDataBytes, 0, signedPayload, 0, authenticatorDataBytes.Length);
        Buffer.BlockCopy(clientDataHash, 0, signedPayload, authenticatorDataBytes.Length, clientDataHash.Length);

        using var key = ECDsa.Create();
        key.ImportSubjectPublicKeyInfo(keyBytes, out _);
        if (!key.VerifyData(signedPayload, signatureBytes, HashAlgorithmName.SHA256))
        {
            return false;
        }

        return IsSignatureCounterValid(state, assertion.CredentialId, authenticatorData.SignCount);
    }

    private static bool TryDecodePublicKey(string publicKey, out byte[] keyBytes)
    {
        keyBytes = [];
        try
        {
            keyBytes = Convert.FromBase64String(publicKey);
            using var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(keyBytes, out _);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static bool TryDecodeBase64(string value, out byte[] bytes)
    {
        bytes = [];
        try
        {
            bytes = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool ChallengeMatches(string receivedChallenge, byte[] expected)
    {
        if (string.IsNullOrWhiteSpace(receivedChallenge))
        {
            return false;
        }

        var expectedChallenge = Base64UrlEncode(expected);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedChallenge);
        var receivedBytes = Encoding.UTF8.GetBytes(receivedChallenge);
        return expectedBytes.Length == receivedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, receivedBytes);
    }

    private static bool TryParseAuthenticatorData(byte[] raw, out ParsedAuthenticatorData data)
    {
        data = new ParsedAuthenticatorData([], UserPresent: false, UserVerified: false, SignCount: 0);
        if (raw.Length < 37)
        {
            return false;
        }

        var rpIdHash = raw.AsSpan(0, 32).ToArray();
        var flags = raw[32];
        var signCount = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(33, 4));
        data = new ParsedAuthenticatorData(
            RelyingPartyHash: rpIdHash,
            UserPresent: (flags & 0x01) != 0,
            UserVerified: (flags & 0x04) != 0,
            SignCount: signCount);
        return true;
    }

    private static bool IsRelyingPartyMatch(byte[] rpIdHash, string rpId)
    {
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(rpId));
        return expected.Length == rpIdHash.Length &&
            CryptographicOperations.FixedTimeEquals(expected, rpIdHash);
    }

    private static bool IsSignatureCounterValid(ParentState state, string credentialId, uint currentCounter)
    {
        if (!state.SignatureCounters.TryGetValue(credentialId, out var lastCounter))
        {
            state.SignatureCounters[credentialId] = currentCounter;
            return true;
        }

        if (lastCounter == 0 && currentCounter == 0)
        {
            return true;
        }

        if (currentCounter <= lastCounter)
        {
            return false;
        }

        state.SignatureCounters[credentialId] = currentCounter;
        return true;
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool IsAuthorized(ParentState state, string gateToken, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(gateToken))
        {
            return false;
        }

        return state.Sessions.TryGetValue(gateToken, out var expiresAt) && expiresAt > now;
    }

    private static void CleanupExpired(ParentState state, DateTimeOffset now)
    {
        foreach (var (token, challenge) in state.Challenges.Where(entry => entry.Value.ExpiresAt <= now).ToArray())
        {
            state.Challenges.Remove(token);
        }

        foreach (var (token, expiresAt) in state.Sessions.Where(entry => entry.Value <= now).ToArray())
        {
            state.Sessions.Remove(token);
        }
    }

    private sealed class ParentState
    {
        public object SyncRoot { get; } = new();

        public Dictionary<string, ParentChallengeState> Challenges { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, DateTimeOffset> Sessions { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, byte[]> Credentials { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, uint> SignatureCounters { get; } = new(StringComparer.Ordinal);

        public bool NotificationsEnabled { get; set; }

        public bool AnalyticsEnabled { get; set; }
    }

    private sealed record ParentChallengeState(byte[] Value, DateTimeOffset ExpiresAt);

    private sealed record WebAuthnClientData(string Type, string Challenge, string Origin);

    private sealed record ParsedAuthenticatorData(byte[] RelyingPartyHash, bool UserPresent, bool UserVerified, uint SignCount);
}
