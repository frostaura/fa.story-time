using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StoryTime.Api.Contracts;

namespace StoryTime.Api.Services;

public sealed class ParentSettingsService(IOptions<StoryTimeOptions> options) : IParentSettingsService
{
    private readonly StoryTimeOptions _options = options.Value;
    private readonly string _stateFilePath = JsonFileStateStore.ResolvePath(options.Value.ParentGate.StateFilePath);
    private readonly object _persistSyncRoot = new();
    private readonly ConcurrentDictionary<string, ParentState> _states = LoadStates(
        JsonFileStateStore.ResolvePath(options.Value.ParentGate.StateFilePath),
        options.Value.ParentDefaults);

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
            PersistStates();
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
            PersistStates();
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
                ? new ParentSettingsSnapshot(state.NotificationsEnabled, state.AnalyticsEnabled, state.KidShelfEnabled)
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

    public bool IsKidShelfEnabled(string softUserId)
    {
        var state = _states.GetOrAdd(softUserId, _ => CreateState());
        lock (state.SyncRoot)
        {
            return state.KidShelfEnabled;
        }
    }

    public ParentSettingsSnapshot? UpdateSettings(
        string softUserId,
        string gateToken,
        bool notificationsEnabled,
        bool analyticsEnabled,
        bool kidShelfEnabled,
        DateTimeOffset now)
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
            state.KidShelfEnabled = kidShelfEnabled;
            PersistStates();
            return new ParentSettingsSnapshot(state.NotificationsEnabled, state.AnalyticsEnabled, state.KidShelfEnabled);
        }
    }

    private ParentState CreateState() => new()
    {
        NotificationsEnabled = _options.ParentDefaults.NotificationsEnabled,
        AnalyticsEnabled = _options.ParentDefaults.AnalyticsEnabled,
        KidShelfEnabled = _options.ParentDefaults.KidShelfEnabled
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
            !_options.ParentGate.AllowedOrigins.Any(origin => IsAllowedOriginMatch(origin, clientData.Origin)))
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
        if (!key.VerifyData(
                signedPayload,
                signatureBytes,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.Rfc3279DerSequence))
        {
            return false;
        }

        return IsSignatureCounterValid(state, assertion.CredentialId, authenticatorData.SignCount);
    }

    private void PersistStates()
    {
        lock (_persistSyncRoot)
        {
            var persisted = _states.ToDictionary(
                pair => pair.Key,
                pair =>
                {
                    lock (pair.Value.SyncRoot)
                    {
                        return new PersistedParentState(
                            pair.Value.NotificationsEnabled,
                            pair.Value.AnalyticsEnabled,
                            pair.Value.KidShelfEnabled,
                            pair.Value.Credentials.ToDictionary(
                                credential => credential.Key,
                                credential => Convert.ToBase64String(credential.Value),
                                StringComparer.Ordinal),
                            new Dictionary<string, uint>(pair.Value.SignatureCounters, StringComparer.Ordinal),
                            new Dictionary<string, DateTimeOffset>(pair.Value.Sessions, StringComparer.Ordinal));
                    }
                },
                StringComparer.Ordinal);

            JsonFileStateStore.Save(_stateFilePath, persisted);
        }
    }

    private static bool IsAllowedOriginMatch(string configuredOrigin, string actualOrigin)
    {
        if (string.Equals(configuredOrigin, actualOrigin, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!Uri.TryCreate(configuredOrigin, UriKind.Absolute, out var configuredUri) ||
            !Uri.TryCreate(actualOrigin, UriKind.Absolute, out var actualUri))
        {
            return false;
        }

        if (!string.Equals(configuredUri.Scheme, actualUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(configuredUri.Host, actualUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!HasExplicitPort(configuredOrigin))
        {
            return true;
        }

        return configuredUri.Port == actualUri.Port;
    }

    private static bool HasExplicitPort(string origin)
    {
        var schemeSeparator = origin.IndexOf("://", StringComparison.Ordinal);
        if (schemeSeparator < 0)
        {
            return false;
        }

        var authority = origin[(schemeSeparator + 3)..];
        var slashIndex = authority.IndexOf('/');
        if (slashIndex >= 0)
        {
            authority = authority[..slashIndex];
        }

        if (authority.StartsWith('['))
        {
            var closingBracket = authority.IndexOf(']');
            return closingBracket >= 0 &&
                   closingBracket + 1 < authority.Length &&
                   authority[closingBracket + 1] == ':';
        }

        return authority.Contains(':');
    }

    private static ConcurrentDictionary<string, ParentState> LoadStates(string path, ParentSettingsDefaults defaults)
    {
        var persisted = JsonFileStateStore.Load(
            path,
            new Dictionary<string, PersistedParentState>(StringComparer.Ordinal));
        var states = new ConcurrentDictionary<string, ParentState>(StringComparer.Ordinal);

        foreach (var (softUserId, value) in persisted)
        {
            var state = new ParentState
            {
                NotificationsEnabled = value.NotificationsEnabled,
                AnalyticsEnabled = value.AnalyticsEnabled,
                KidShelfEnabled = value.KidShelfEnabled
            };

            foreach (var (credentialId, publicKey) in value.Credentials)
            {
                if (TryDecodePublicKey(publicKey, out var keyBytes))
                {
                    state.Credentials[credentialId] = keyBytes;
                }
            }

            foreach (var (credentialId, signCount) in value.SignatureCounters)
            {
                state.SignatureCounters[credentialId] = signCount;
            }

            foreach (var (gateToken, expiresAt) in value.Sessions)
            {
                state.Sessions[gateToken] = expiresAt;
            }

            states[softUserId] = state;
        }

        return states;
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

        public bool KidShelfEnabled { get; set; }
    }

    private sealed record ParentChallengeState(byte[] Value, DateTimeOffset ExpiresAt);

    private sealed record WebAuthnClientData(string Type, string Challenge, string Origin);

    private sealed record ParsedAuthenticatorData(byte[] RelyingPartyHash, bool UserPresent, bool UserVerified, uint SignCount);

    private sealed record PersistedParentState(
        bool NotificationsEnabled,
        bool AnalyticsEnabled,
        bool KidShelfEnabled,
        IReadOnlyDictionary<string, string> Credentials,
        IReadOnlyDictionary<string, uint> SignatureCounters,
        IReadOnlyDictionary<string, DateTimeOffset> Sessions);
}
