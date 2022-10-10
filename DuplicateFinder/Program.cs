using Spectre.Console;
using Sentry;

using (SentrySdk.Init(o =>
{
	o.Dsn = "https://be1185ff38b64e5e8cbccf8a3be012c2@o141824.ingest.sentry.io/4503961617367040";
	// When configuring for the first time, to see what the SDK is doing:
	o.Debug = true;
	// Set traces_sample_rate to 1.0 to capture 100% of transactions for performance monitoring.
	// We recommend adjusting this value in production.
	o.TracesSampleRate = 1.0;
	// Enable Global Mode if running in a client app
	o.IsGlobalModeEnabled = true;
}))
{
	//var container = new WindsorContainer();
	//container.Register(Component.For<ICompositionRoot>().ImplementedBy<CompositionRoot>());
	DuplicateFinder.Core.DuplicateFinder dupe = new DuplicateFinder.Core.DuplicateFinder();
	DuplicateFinder.Models.Options options = new DuplicateFinder.Models.Options();
	try
	{
		dupe.FindDuplicates(options);
	}
	catch (Exception e)
	{
		SentrySdk.CaptureException(e);
		AnsiConsole.WriteException(e);
	}
}
