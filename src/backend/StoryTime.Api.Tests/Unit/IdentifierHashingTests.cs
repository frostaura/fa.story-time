using StoryTime.Api.Domain;

namespace StoryTime.Api.Tests.Unit;

public sealed class IdentifierHashingTests
{
    [Fact]
    public void HashIdentifier_ReturnsAnonymousForMissingIdentifier()
    {
        var hash = IdentifierHashing.HashIdentifier(" ", 8);

        Assert.Equal("anonymous", hash);
    }

    [Fact]
    public void HashIdentifier_ReturnsDeterministicHashAndNeverLeaksRawIdentifier()
    {
        const string rawIdentifier = "parent-sensitive-user-123";

        var first = IdentifierHashing.HashIdentifier(rawIdentifier, 6);
        var second = IdentifierHashing.HashIdentifier(rawIdentifier, 6);

        Assert.Equal(first, second);
        Assert.Equal(12, first.Length);
        Assert.DoesNotContain(rawIdentifier, first, StringComparison.Ordinal);
    }
}
