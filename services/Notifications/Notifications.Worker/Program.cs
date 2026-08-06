using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Notifications.Application.Interfaces;
using Notifications.Application.Services;
using Notifications.Infrastructure.Providers;
using Notifications.Worker;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Application
        services.AddSingleton<INotificationDispatcher, NotificationDispatcher>();
        services.AddSingleton<ITemplateService, Notifications.Infrastructure.Services.ScribanTemplateService>();

        // Infrastructure Providers
        services.AddSingleton<INotificationProvider, SmtpEmailProvider>();

        // Hosted Services (Background Workers)
        services.AddHostedService<KafkaConsumerBackgroundService>();
    })
    .Build();

await host.RunAsync();
