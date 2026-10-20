using Xunit;

// Integration tests spin up multiple ASP.NET hosts that share the ML model path.
// Running them in parallel can race model creation/loading and make TestServer
// requests time out. Keep the suite deterministic; production request handling
// remains concurrent.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
