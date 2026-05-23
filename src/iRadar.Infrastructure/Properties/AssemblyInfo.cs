using System.Runtime.CompilerServices;

// Test project sees internals — kept tight so production callers go through
// the public ITelemetrySource port only.
[assembly: InternalsVisibleTo("iRadar.Infrastructure.Tests")]
