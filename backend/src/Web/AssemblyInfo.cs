using System.Runtime.CompilerServices;

// Mirrors src/Infrastructure/AssemblyInfo.cs's precedent: lets Web.Tests
// exercise internal middleware (e.g. AuthorizationMiddleware) directly
// without widening its public surface for production callers.
[assembly: InternalsVisibleTo("TutorFlow.Web.Tests")]
