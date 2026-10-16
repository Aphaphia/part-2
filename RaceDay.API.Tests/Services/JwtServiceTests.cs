using Microsoft.Extensions.Configuration;
using RaceDay.API.Models;
using RaceDay.API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace RaceDay.API.Tests.Services;

public class JwtServiceTests
{
    /// <summary>
    /// Helper that builds a JwtService with a fake configuration.
    /// Real appsettings values aren't needed — any consistent values work for tests.
    /// </summary>
    private static JwtService BuildService()
    {
        var settings = new Dictionary<string, string?>
        {
            { "Jwt:Key", "ThisIsATestSecretKeyForRaceDayAPITestsOnly1234567890" },
            { "Jwt:Issuer", "RaceDayTestIssuer" },
            { "Jwt:Audience", "RaceDayTestAudience" },
            { "Jwt:ExpiryMinutes", "60" }
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new JwtService(config);
    }

    /// <summary>
    /// Helper: creates a dummy user for tests.
    /// </summary>
    private static User BuildUser() => new User
    {
        UserID = 42,
        FirstName = "Test",
        LastName = "User",
        Email = "test.user@example.com",
        Password = "not-used-in-token",
        Role = "Participant"
    };

    // ============================================================
    // TEST 1: GenerateToken returns a non-empty string
    // ============================================================
    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        // ARRANGE
        var service = BuildService();
        var user = BuildUser();

        // ACT
        var token = service.GenerateToken(user);

        // ASSERT
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    // ============================================================
    // TEST 2: The token contains the correct claims
    // ============================================================
    [Fact]
    public void GenerateToken_ContainsExpectedClaims()
    {
        // ARRANGE
        var service = BuildService();
        var user = BuildUser();

        // ACT
        var token = service.GenerateToken(user);

        // ASSERT
        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);

        Assert.Equal("42", parsed.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("test.user@example.com", parsed.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Participant", parsed.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal("Test User", parsed.Claims.First(c => c.Type == ClaimTypes.Name).Value);
    }

    // ============================================================
    // TEST 3: The token has the correct issuer and audience
    // ============================================================
    [Fact]
    public void GenerateToken_HasCorrectIssuerAndAudience()
    {
        // ARRANGE
        var service = BuildService();
        var user = BuildUser();

        // ACT
        var token = service.GenerateToken(user);

        // ASSERT
        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);

        Assert.Equal("RaceDayTestIssuer", parsed.Issuer);
        Assert.Contains("RaceDayTestAudience", parsed.Audiences);
    }

    // ============================================================
    // TEST 4: The token expires in the future (not already expired)
    // ============================================================
    [Fact]
    public void GenerateToken_HasFutureExpiry()
    {
        // ARRANGE
        var service = BuildService();
        var user = BuildUser();

        // ACT
        var token = service.GenerateToken(user);

        // ASSERT
        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);

        Assert.True(parsed.ValidTo > DateTime.UtcNow,
            "Token should expire in the future.");
    }

    // ============================================================
    // TEST 5: Different roles produce different tokens
    // ============================================================
    [Fact]
    public void GenerateToken_DifferentRoles_ProduceDifferentRoleClaims()
    {
        // ARRANGE
        var service = BuildService();
        var organiser = BuildUser();
        organiser.Role = "Organiser";

        var participant = BuildUser();
        participant.Role = "Participant";

        // ACT
        var organiserToken = service.GenerateToken(organiser);
        var participantToken = service.GenerateToken(participant);

        // ASSERT
        var handler = new JwtSecurityTokenHandler();
        var orgClaims = handler.ReadJwtToken(organiserToken).Claims;
        var partClaims = handler.ReadJwtToken(participantToken).Claims;

        Assert.Equal("Organiser", orgClaims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal("Participant", partClaims.First(c => c.Type == ClaimTypes.Role).Value);
    }
}
