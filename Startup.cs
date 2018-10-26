using ExpressBase.Common;
using ExpressBase.Common.Constants;
using ExpressBase.Common.EbServiceStack.ReqNRes;
using ExpressBase.Common.ServiceClients;
using ExpressBase.Common.ServiceStack.Auth;
using ExpressBase.Objects.ServiceStack_Artifacts;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.RabbitMq;
using ServiceStack.Redis;
using System;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
//using Quartz;
//using ServiceStack.Quartz;
//using ExpressBase.MessageQueue.Services.Quartz;

namespace ExpressBase.Scheduler
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDataProtection(opts =>
            {
                opts.ApplicationDiscriminator = "expressbase.scheduler";
            });
            // Add framework services.
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseServiceStack(new AppHost());


        }
    }

    public class AppHost : AppHostBase
    {
        public AppHost() : base("EXPRESSbase Scheduler", typeof(AppHost).Assembly)
        {
        }

        public override void Configure(Container container)
        {
            LogManager.LogFactory = new ConsoleLogFactory(debugEnabled: true);

            var jwtprovider = new JwtAuthProviderReader
            {
                HashAlgorithm = "RS256",
                PublicKeyXml = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_JWT_PUBLIC_KEY_XML),
#if (DEBUG)
                RequireSecureConnection = false,
                //EncryptPayload = true,
#endif
            };

            this.Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                new IAuthProvider[] {
                    jwtprovider,
                }));

#if (DEBUG)
            SetConfig(new HostConfig { DebugMode = true });
#endif
            SetConfig(new HostConfig { DefaultContentType = MimeTypes.Json });



            RabbitMqMessageFactory rabitFactory = new RabbitMqMessageFactory();
            rabitFactory.ConnectionFactory.UserName = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_USER);
            rabitFactory.ConnectionFactory.Password = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_PASSWORD);
            rabitFactory.ConnectionFactory.HostName = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_HOST);
            rabitFactory.ConnectionFactory.Port = Convert.ToInt32(Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_PORT));
            rabitFactory.ConnectionFactory.VirtualHost = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_VHOST);

            var mqServer = new RabbitMqServer(rabitFactory);

            mqServer.RetryCount = 1;

            mqServer.RegisterHandler<SchedulerRequest105>(base.ExecuteMessage);

            mqServer.Start();

            container.AddScoped<IMessageProducer, RabbitMqProducer>(serviceProvider =>
            {
                return mqServer.CreateMessageProducer() as RabbitMqProducer;
            });

            container.AddScoped<IMessageQueueClient, RabbitMqQueueClient>(serviceProvider =>
            {
                return mqServer.CreateMessageQueueClient() as RabbitMqQueueClient;
            });

            //****************SCHEDULER********************
            string conn = string.Format("Server = {0}; Database = {1}; Uid = {2}; Pwd = {3}",
                 Environment.GetEnvironmentVariable(EnvironmentConstants.EB_INFRA_DB_SERVER),
                 Environment.GetEnvironmentVariable(EnvironmentConstants.EB_INFRA_DBNAME),
                 Environment.GetEnvironmentVariable(EnvironmentConstants.EB_INFRA_DB_RW_USER),
                 Environment.GetEnvironmentVariable(EnvironmentConstants.EB_INFRA_DB_RW_PASSWORD));
            var properties = new NameValueCollection
            {
                ["quartz.serializer.type"] = "json",
                ["quartz.scheduler.instanceName"] = "DotnetCoreScheduler",
                ["quartz.scheduler.instanceId"] = "instance_one",
                ["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quartz",
                ["quartz.threadPool.threadCount"] = "5",
                ["quartz.jobStore.misfireThreshold"] = "60000",
                ["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz",
                ["quartz.jobStore.useProperties"] = "false",
                ["quartz.jobStore.dataSource"] = "myDS",
                ["quartz.jobStore.tablePrefix"] = "QRTZ_",
                ["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.PostgreSQLDelegate, Quartz",
                ["quartz.dataSource.myDS.provider"] = "Npgsql ",
                ["quartz.dataSource.myDS.connectionString"] = conn
            };



            StdSchedulerFactory factory = new Quartz.Impl.StdSchedulerFactory(properties);
            IScheduler scheduler = factory.GetScheduler().Result;
            scheduler.Start().Wait();

            container.AddScoped<IScheduler>(serviceProvider =>
            {
                return scheduler;
            });

            this.GlobalRequestFilters.Add((req, res, requestDto) =>
        {
            ILog log = LogManager.GetLogger(GetType());

            log.Info("In GlobalRequestFilters");
            try
            {
                log.Info("In Try");
                if (requestDto != null)
                {
                    log.Info("In Auth Header");
                    var auth = req.Headers[HttpHeaders.Authorization];
                    if (string.IsNullOrEmpty(auth))
                        res.ReturnAuthRequired();
                    else
                    {
                        if (req.Headers[CacheConstants.RTOKEN] != null)
                        {
                            Resolve<IEbServerEventClient>().AddAuthentication(req);
                        }
                        var jwtoken = new JwtSecurityToken(auth.Replace("Bearer", string.Empty).Trim());
                        foreach (var c in jwtoken.Claims)
                        {
                            if (c.Type == "cid" && !string.IsNullOrEmpty(c.Value))
                            {
                                RequestContext.Instance.Items.Add(CoreConstants.SOLUTION_ID, c.Value);
                                if (requestDto is IEbSSRequest)
                                    (requestDto as IEbSSRequest).SolnId = c.Value;
                                if (requestDto is EbServiceStackAuthRequest)
                                    (requestDto as EbServiceStackAuthRequest).SolnId = c.Value;
                                continue;
                            }
                            if (c.Type == "uid" && !string.IsNullOrEmpty(c.Value))
                            {
                                RequestContext.Instance.Items.Add("UserId", Convert.ToInt32(c.Value));
                                if (requestDto is IEbSSRequest)
                                    (requestDto as IEbSSRequest).UserId = Convert.ToInt32(c.Value);
                                if (requestDto is EbServiceStackAuthRequest)
                                    (requestDto as EbServiceStackAuthRequest).UserId = Convert.ToInt32(c.Value);
                                continue;
                            }
                            if (c.Type == "wc" && !string.IsNullOrEmpty(c.Value))
                            {
                                RequestContext.Instance.Items.Add("wc", c.Value);
                                if (requestDto is EbServiceStackAuthRequest)
                                    (requestDto as EbServiceStackAuthRequest).WhichConsole = c.Value.ToString();
                                continue;
                            }
                            if (c.Type == "sub" && !string.IsNullOrEmpty(c.Value))
                            {
                                RequestContext.Instance.Items.Add("sub", c.Value);
                                if (requestDto is EbServiceStackAuthRequest)
                                    (requestDto as EbServiceStackAuthRequest).UserAuthId = c.Value.ToString();
                                continue;
                            }
                        }
                        log.Info("Req Filter Completed");
                    }
                }
            }
            catch (Exception e)
            {
                log.Info("ErrorStackTraceNontokenServices..........." + e.StackTrace);
                log.Info("ErrorMessageNontokenServices..........." + e.Message);
                log.Info("InnerExceptionNontokenServices..........." + e.InnerException);
            }
        });
        }
    }
}

//https://github.com/ServiceStack/ServiceStack/blob/master/tests/ServiceStack.Server.Tests/Messaging/MqServerIntroTests.cs