Toolkit built to make C# CLI apps easier to provide a simple custom text based interface.

Use Example:
```cs
var menu = new CliMenu("Main Menu", "Your menu description");

menu.AddOption("Run Database Scaffolding", async () => await RunDatabaseScaffolding());
menu.AddOption("Manage Share Protocol", () => ManageShareProtocol());
menu.AddOption("Exit", () => Environment.Exit(0));

await menu.ShowAsync();
```