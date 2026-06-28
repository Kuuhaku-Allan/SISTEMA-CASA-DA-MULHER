namespace CasaMulher.Api.Services;

public sealed record WebAuthnEnvironmentInfo(
    string RpId,
    string RpName,
    IReadOnlySet<string> Origins,
    string EnvironmentName);
