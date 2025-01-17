﻿using System.Globalization;
using Bit.Core;
using Bit.Core.Services;
using Bit.Core.Utilities;
using IdentityModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace Bit.Events
{
    public class Startup
    {
        public Startup(IHostingEnvironment env, IConfiguration configuration)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            Configuration = configuration;
            Environment = env;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment Environment { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Options
            services.AddOptions();

            // Settings
            var globalSettings = services.AddGlobalSettingsServices(Configuration);

            // Repositories
            services.AddSqlServerRepositories(globalSettings);

            // Context
            services.AddScoped<CurrentContext>();

            // Identity
            services.AddIdentityAuthenticationServices(globalSettings, Environment, config =>
            {
                config.AddPolicy("Application", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(JwtClaimTypes.AuthenticationMethod, "Application");
                    policy.RequireClaim(JwtClaimTypes.Scope, "api");
                });
            });

            // Services
            var usingServiceBusAppCache = CoreHelpers.SettingHasValue(globalSettings.ServiceBus.ConnectionString) &&
                CoreHelpers.SettingHasValue(globalSettings.ServiceBus.ApplicationCacheTopicName);
            if(usingServiceBusAppCache)
            {
                services.AddSingleton<IApplicationCacheService, InMemoryServiceBusApplicationCacheService>();
            }
            else
            {
                services.AddSingleton<IApplicationCacheService, InMemoryApplicationCacheService>();
            }
            services.AddScoped<IEventService, EventService>();
            if(!globalSettings.SelfHosted && CoreHelpers.SettingHasValue(globalSettings.Events.ConnectionString))
            {
                services.AddSingleton<IEventWriteService, AzureQueueEventWriteService>();
            }
            else
            {
                services.AddSingleton<IEventWriteService, RepositoryEventWriteService>();
            }

            // Mvc
            services.AddMvc();

            if(usingServiceBusAppCache)
            {
                services.AddHostedService<Core.HostedServices.ApplicationCacheHostedService>();
            }
        }

        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            IApplicationLifetime appLifetime,
            GlobalSettings globalSettings)
        {
            loggerFactory.AddSerilog(app, env, appLifetime, globalSettings, (e) =>
            {
                var context = e.Properties["SourceContext"].ToString();
                if(context.Contains("IdentityServer4.Validation.TokenValidator") ||
                    context.Contains("IdentityServer4.Validation.TokenRequestValidator"))
                {
                    return e.Level > LogEventLevel.Error;
                }

                return e.Level >= LogEventLevel.Error;
            });

            if(env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Default Middleware
            app.UseDefaultMiddleware(env);

            // Add Cors
            app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().AllowCredentials());

            // Add authentication to the request pipeline.
            app.UseAuthentication();

            // Add current context
            app.UseMiddleware<CurrentContextMiddleware>();

            // Add MVC to the request pipeline.
            app.UseMvc();
        }
    }
}
