using Microsoft.Extensions.Hosting;

namespace Adm.Server.Host.Health;

public sealed class AdmHealthLifecycle
{
    private int started;
    private int stopping;

    public bool IsStarted => Volatile.Read(ref started) == 1;

    public bool IsStopping => Volatile.Read(ref stopping) == 1;

    internal void MarkStarted() => Interlocked.Exchange(ref started, 1);

    internal void MarkStopping() => Interlocked.Exchange(ref stopping, 1);
}

public static class HealthServiceCollectionExtensions
{
    public static IServiceCollection AddAdmHealth(this IServiceCollection services)
    {
        services.AddSingleton<AdmHealthLifecycle>(serviceProvider =>
        {
            var lifecycle = new AdmHealthLifecycle();
            var applicationLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
            applicationLifetime.ApplicationStarted.Register(lifecycle.MarkStarted);
            applicationLifetime.ApplicationStopping.Register(lifecycle.MarkStopping);
            return lifecycle;
        });
        services.AddSingleton<AdmHealthRegistry>();
        return services;
    }
}
