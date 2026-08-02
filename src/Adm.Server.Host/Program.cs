using Adm.Server.Host;
using Adm.Infrastructure.Windows.Hosting;

try
{
    var launchConfiguration = WindowsServiceHostAdapter.Resolve(args);
    await using var app = ServerHostFactory.Create(
        args,
        startupMode: launchConfiguration.StartupMode,
        configureHost: hostBuilder => WindowsServiceHostAdapter.Configure(hostBuilder, launchConfiguration));
    await app.RunAsync();
}
catch (IOException exception) when (exception.Message.Contains("address", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Serverを起動できません。指定されたポートが使用中です。");
    return 2;
}
catch (Exception)
{
    Console.Error.WriteLine("Serverを起動できません。構成と起動条件を確認してください。");
    return 1;
}

return 0;
