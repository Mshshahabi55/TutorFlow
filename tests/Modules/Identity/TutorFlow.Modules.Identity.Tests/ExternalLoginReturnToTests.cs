using TutorFlow.Modules.Identity.Infrastructure;

namespace TutorFlow.Modules.Identity.Tests;

public class ExternalLoginReturnToTests
{
    [Fact]
    public void Sanitize_WithNullOrEmpty_ReturnsRoot()
    {
        Assert.Equal("/", ExternalLoginReturnTo.Sanitize(null));
        Assert.Equal("/", ExternalLoginReturnTo.Sanitize(string.Empty));
    }

    [Fact]
    public void Sanitize_WithALegitimateInternalPath_PassesThrough()
    {
        Assert.Equal("/become-a-tutor", ExternalLoginReturnTo.Sanitize("/become-a-tutor"));
        Assert.Equal("/tutors/jane-dev", ExternalLoginReturnTo.Sanitize("/tutors/jane-dev"));
    }

    [Theory]
    [InlineData("https://evil.example.com")]
    [InlineData("http://evil.example.com")]
    [InlineData("evil.example.com")]
    public void Sanitize_WithAnAbsoluteExternalUrl_NeutralizesToRoot(string malicious)
    {
        Assert.Equal("/", ExternalLoginReturnTo.Sanitize(malicious));
    }

    [Fact]
    public void Sanitize_WithAProtocolRelativeUrl_NeutralizesToRoot()
    {
        Assert.Equal("/", ExternalLoginReturnTo.Sanitize("//evil.example.com"));
    }

    [Fact]
    public void Sanitize_WithABackslashVariant_NeutralizesToRoot()
    {
        Assert.Equal("/", ExternalLoginReturnTo.Sanitize("/\\evil.example.com"));
    }
}
