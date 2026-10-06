namespace Angur.Application.UnitTests.Fakes;

/// <summary>A clock that is stopped at <paramref name="utcNow"/>.</summary>
internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
