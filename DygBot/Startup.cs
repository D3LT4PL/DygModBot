﻿using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

using DygBot.Addons;
using DygBot.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

using Quartz;
using Quartz.Impl;

using Reddit;

using Serilog;

namespace DygBot
{
    public static class Startup
    {
#pragma warning disable CA2211 // Non-constant fields should not be visible
        public static bool KeepRunning = true;
#pragma warning restore CA2211 // Non-constant fields should not be visible

        public static async Task RunAsync()
        {
            // Create a new instance of a service collection
            var services = new ServiceCollection();
            await ConfigureServices(services);

            var provider = services.BuildServiceProvider(); // Build the service provider
            provider.GetRequiredService<LoggingService>();  // Start the logging service
            provider.GetRequiredService<CommandHandler>();  // Start the command handler service

            await provider.GetRequiredService<StartupService>().StartAsync();   // Start the startup service
            var scheduler = provider.GetRequiredService<IScheduler>();
            await scheduler.Start();

            while (KeepRunning)
            {
                await Task.Delay(1000);
            }

            await LoggingService.OnLogAsync(new LogMessage(LogSeverity.Info, "Program", "Stopping"));
            await scheduler.Shutdown();
            await StartupService.Discord.StopAsync();
            await LoggingService.OnLogAsync(new LogMessage(LogSeverity.Info, "Program", "Stopped"));

            Log.CloseAndFlush();
        }

        private static async Task ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig   // Add Discord to the collection
                {
                    LogLevel = LogSeverity.Debug,    // Set the log level
                    MessageCacheSize = 1000, // Cache 1000 messages per channel
                    ExclusiveBulkDelete = true,  // Fire only bulk delete or delete event (by default both get fired on bulk delete)
                    AlwaysDownloadUsers = true
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig   // Add the command service to the collection
                {
                    LogLevel = LogSeverity.Debug, // Set the log level
                    DefaultRunMode = RunMode.Async,  // Force all commands to run async by default
                    CaseSensitiveCommands = false
                }))
                .AddSingleton(await new StdSchedulerFactory(new NameValueCollection
                    {{"quartz.serializer.type", "binary" }})
                        .GetScheduler())
                .AddDbContext<AppDbContext>(options =>
                    options.UseMySql(GetMySqlConnectionString(), mySqlOptions => mySqlOptions
                        .ServerVersion(new Version(5, 6, 47), ServerType.MySql)))
                .AddSingleton<HttpClient>() // Add the HttpClient to the collection
                .AddSingleton<GitHubService>()  // Add the GitHub service to the collection
                .AddSingleton<CommandHandler>() // Add the command handler to the collection
                .AddSingleton<LoggingService>() // Add the logging service to the collection
                .AddSingleton<Random>()      // Add random to the collection
                .AddSingleton<InteractiveService>()
                .AddSingleton<ExtendedInteractiveService>()
                .AddSingleton(prov =>
                {
                    var git = prov.GetRequiredService<GitHubService>();
                    git.DownloadConfig().Wait();
                    var config = git.Config;
                    return new RedditClient(config.RedditAppId, config.RedditAppRefreshToken, config.RedditAppSecret, userAgent: "DygModBot by D3LT4PL/1.0");
                })
                .AddSingleton<StartupService>(); // Add the startup service to the collection
        }

        private static string GetMySqlConnectionString()
        {
            string rawString = Environment.GetEnvironmentVariable("CLEARDB_DATABASE_URL") ?? throw new ArgumentNullException();

            var user = new Regex("/([A-z0-9]*):").Match(rawString).Groups[1].Value;
            var password = new Regex(":([A-z0-9]*)@").Match(rawString).Groups[1].Value;
            var server = new Regex("@([-A-z0-9/_.]*)/").Match(rawString).Groups[1].Value;
            var database = new Regex("/(heroku_[A-z0-9]*)").Match(rawString).Groups[1].Value;

            return $"Server={server}; Database={database}; Uid={user}; Pwd={password}";
        }
    }
}
