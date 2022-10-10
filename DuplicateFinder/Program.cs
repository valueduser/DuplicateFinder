using Spectre.Console;
using DuplicateFinder.Models;

AnsiConsole.Markup("[underline red]Hello[/] World!");
//var container = new WindsorContainer();

//container.Register(Component.For<ICompositionRoot>().ImplementedBy<CompositionRoot>());
DuplicateFinder.Core.DuplicateFinder dupe = new DuplicateFinder.Core.DuplicateFinder();
DuplicateFinder.Models.Options options = new DuplicateFinder.Models.Options();
dupe.FindDuplicates(options);
