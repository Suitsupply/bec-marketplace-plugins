// In ApplicationFactory.ConfigureServices — replace real client with mock:
RemoveAll<IFooClient>(services);
services.AddSingleton(FooClient.Object);

// In Hooks.BeforeScenario — reset per scenario:
factory.FooClient.Reset();

// In ConfigureWebHost — map new HTTP function route:
endpoints.MapPost("/api/foo/created", Route<FooReceiver>((fn, req, ct) => fn.Run(req, ct)));
