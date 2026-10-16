using Microsoft.Extensions.Configuration;

namespace RaceDay.API.Tests.Helpers;

public static class TestConfiguration
{
    public static IConfiguration Build()
    {
        var settings = new Dictionary<string, string?>
        {
            { "Jwt:Key", "ThisIsATestSecretKeyForRaceDayAPITestsOnly1234567890" },
            { "Jwt:Issuer", "RaceDayTestIssuer" },
            { "Jwt:Audience", "RaceDayTestAudience" },
            { "Jwt:ExpiryMinutes", "60" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }
}