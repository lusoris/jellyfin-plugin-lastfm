---
applyTo: "**/*.Tests/**/*.cs,**/tests/**/*.cs,**/*Tests.cs,**/*Test.cs"
description: Testing patterns for Jellyfin plugin development
---

# Testing Patterns

## Framework Setup

The plugin uses **xUnit** with **Moq** for mocking and **Coverlet** for code coverage.

```xml
<!-- Test project dependencies -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.0.1" />
<PackageReference Include="coverlet.collector" Version="6.0.4" />
<PackageReference Include="Moq" Version="4.20.72" />
```

## Running Tests

### All Shells

```bash
# Bash/Zsh
dotnet test

# Fish
dotnet test

# PowerShell
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### With Coverage Report

```bash
# Bash/Zsh
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# PowerShell
dotnet test --collect:"XPlat Code Coverage" --results-directory .\coverage

# Generate HTML report (requires reportgenerator tool)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:./coverage/**/coverage.cobertura.xml -targetdir:./coverage/report
```

## Test Organization

### Naming Conventions

```csharp
// Test class: {ClassUnderTest}Tests
public class ScrobbleServiceTests { }
public class SignatureGeneratorTests { }
public class TrackMatcherServiceTests { }

// Test method: {Method}_{Scenario}_{ExpectedResult}
public void IsScrobbleEligible_TrackTooShort_ReturnsFalse() { }
public void CreateSignature_ValidParams_ReturnsCorrectHash() { }
```

### Test Structure (AAA Pattern)

```csharp
[Fact]
public void IsScrobbleEligible_PlayedHalfOfTrack_ReturnsTrue()
{
    // Arrange
    var logger = Mock.Of<ILogger<ScrobbleService>>();
    var service = new ScrobbleService(logger);
    var trackLength = TimeSpan.FromMinutes(6).Ticks;
    var playedTime = TimeSpan.FromMinutes(3).Ticks; // 50%

    // Act
    var result = service.IsScrobbleEligible(trackLength, playedTime);

    // Assert
    Assert.True(result);
}
```

## Mocking Jellyfin Interfaces

### ILibraryManager

```csharp
[Fact]
public async Task FindMatchingTrackAsync_WithMbid_FindsTrack()
{
    // Arrange
    var libraryManager = new Mock<ILibraryManager>();
    var logger = Mock.Of<ILogger<TrackMatcherService>>();
    
    var expectedTrack = new Audio
    {
        Name = "Test Track",
        ProviderIds = new Dictionary<string, string>
        {
            [MetadataProvider.MusicBrainzRecording.ToString()] = "test-mbid"
        }
    };

    libraryManager
        .Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>()))
        .Returns([expectedTrack]);

    var service = new TrackMatcherService(libraryManager.Object, logger);

    // Act
    var result = await service.FindMatchingTrackAsync("Artist", "Track", "test-mbid", Guid.NewGuid());

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test Track", result.Name);
}
```

### ILogger

```csharp
// Option 1: Mock.Of (silent logger)
var logger = Mock.Of<ILogger<MyService>>();

// Option 2: NullLogger
var logger = NullLogger<MyService>.Instance;

// Option 3: Verify logging calls
var loggerMock = new Mock<ILogger<MyService>>();
// ... test code ...
loggerMock.Verify(
    x => x.Log(
        LogLevel.Error,
        It.IsAny<EventId>(),
        It.IsAny<It.IsAnyType>(),
        It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
    Times.Once);
```

## Integration Testing Considerations

⚠️ **Jellyfin plugins cannot easily run full integration tests** because they depend on the Jellyfin server runtime. Use these approaches:

### 1. Unit Test Core Logic

Test business logic independently:
```csharp
// ✅ Good - testable logic
public class ScrobbleService
{
    public bool IsScrobbleEligible(long trackLength, long playedTicks) { }
}

// ✅ Good - testable signature generation
public class Md5SignatureGenerator
{
    public string CreateSignature(Dictionary<string, string> parameters) { }
}
```

### 2. Interface Segregation

Create testable interfaces:
```csharp
// ✅ Good - mockable interface
public interface ILastfmApiClient
{
    Task<bool> ScrobbleAsync(Scrobble scrobble, CancellationToken ct);
}

// Can easily mock in tests
var apiClient = new Mock<ILastfmApiClient>();
apiClient.Setup(x => x.ScrobbleAsync(It.IsAny<Scrobble>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(true);
```

### 3. Snapshot Testing (for API requests)

```csharp
[Fact]
public void ScrobbleRequest_BuildsCorrectParameters()
{
    var request = new ScrobbleRequest
    {
        Artist = "Test Artist",
        Track = "Test Track",
        Album = "Test Album",
        Timestamp = 1234567890
    };

    var parameters = request.ToDictionary();

    Assert.Equal("Test Artist", parameters["artist"]);
    Assert.Equal("Test Track", parameters["track"]);
    Assert.Equal("Test Album", parameters["album"]);
    Assert.Equal("1234567890", parameters["timestamp"]);
}
```

## Code Coverage Goals

| Metric | Target | Priority |
|--------|--------|----------|
| Line Coverage | >70% | Critical code paths |
| Branch Coverage | >60% | Error handling |
| Method Coverage | >80% | Public APIs |

**Focus coverage on:**
- Scrobble validation logic
- API request building
- Signature generation
- Track matching algorithms

**Skip coverage for:**
- Plugin entry points (DI registration)
- Configuration models
- Jellyfin callback handlers (require runtime)

## CI/CD Integration

Tests run automatically via GitHub Actions:

```yaml
- name: Run Tests
  run: dotnet test --configuration Release --no-build --verbosity normal
```

## Performance Testing

For performance-critical code (track matching, caching), use benchmarks:

```csharp
// Using BenchmarkDotNet (optional)
[MemoryDiagnoser]
public class TrackMatcherBenchmarks
{
    [Benchmark]
    public void FindByMusicBrainzId_Cached()
    {
        // Benchmark caching effectiveness
    }
}
```
