using System;
using System.Threading.Tasks;
using JsPlayBot.UpdateHandlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecurrentTasks;
using Telegram.Bot.Framework;

namespace JsPlayBot
{
    public class Startup
    {
        private readonly IConfigurationRoot _configuration;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables("JsPlayBot_");
            _configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTelegramBot<JsPlayBot>(_configuration.GetSection("JsPlayBot"))
                .AddUpdateHandler<StartCommand>()
                .Configure();

            services.AddTask<BotUpdateGetterTask<JsPlayBot>>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(_configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            ILogger logger = loggerFactory.CreateLogger<Startup>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler(appBuilder =>
                    appBuilder.Run(context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        return Task.CompletedTask;
                    })
                );
            }

            if (env.IsDevelopment())
            {
                app.UseTelegramBotLongPolling<JsPlayBot>();
                app.StartTask<BotUpdateGetterTask<JsPlayBot>>(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(3));
                logger.LogInformation("Update polling task is scheduled for bot " + nameof(JsPlayBot));
            }
            else
            {
                app.UseTelegramBotWebhook<JsPlayBot>();
                logger.LogInformation("Webhook is set for bot " + nameof(JsPlayBot));
            }
        }
    }
}
