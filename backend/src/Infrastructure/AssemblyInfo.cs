using System.Runtime.CompilerServices;

// Lets Infrastructure.Tests exercise internal implementations (e.g.
// InMemoryUnitOfWork) directly, rather than only through the public
// IUnitOfWork interface — standard .NET testing convention, not a new
// architectural pattern.
[assembly: InternalsVisibleTo("TutorFlow.Infrastructure.Tests")]
