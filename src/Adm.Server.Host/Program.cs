using Adm.Server.Host;

try
{
    await using var app = ServerHostFactory.Create(args);
    await app.RunAsync();
}
catch (IOException exception) when (exception.Message.Contains("address", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Serverを起動できません。指定されたポートが使用中です。");
    return 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Serverを起動できません。{exception.Message}");
    return 1;
}

return 0;
