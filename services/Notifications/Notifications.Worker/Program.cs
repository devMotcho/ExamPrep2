using Notifications.Application.Interfaces;
using Notifications.Application.Services;
using Notifications.Infrastructure.Providers;
using Notifications.Infrastructure.Services;
using Notifications.Worker;
using Notifications.Worker.Handlers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Application
        services.AddSingleton<INotificationDispatcher, NotificationDispatcher>();
        services.AddSingleton<ITemplateService, ScribanTemplateService>();

        // Infrastructure Providers
        services.AddSingleton<INotificationProvider, SmtpEmailProvider>();

        // Kafka Event Handlers (each handler self-declares which topic it handles)
        services.AddSingleton<IKafkaEventHandler, PartnerTransactionHandler>();
        services.AddSingleton<IKafkaEventHandler, EmailVerificationHandler>();
        services.AddSingleton<IKafkaEventHandler, PasswordChangeHandler>();
        services.AddSingleton<IKafkaEventHandler, PasswordResetHandler>();

        // Hosted Services (Background Workers)
        services.AddHostedService<KafkaConsumerBackgroundService>();
    })
    .Build();

await host.RunAsync();
