using ExpressBase.Common;
using ExpressBase.Common.ServiceClients;
using ExpressBase.Objects.Services;
using ExpressBase.Objects.ServiceStack_Artifacts;
using ExpressBase.Scheduler.Jobs;
using Quartz;
using Quartz.Impl;
using ServiceStack;
using ServiceStack.Messaging;
using ServiceStack.RabbitMq;
using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace ExpressBase.Scheduler
{
    [Restrict(InternalOnly = true)]
    public class EbSchedule : Service
    {
        protected RabbitMqProducer MessageProducer3 { get; private set; }

        protected RabbitMqQueueClient MessageQueueClient { get; private set; }

        protected IScheduler Scheduler { get; private set; }

        public EbSchedule(IMessageProducer _mqp, IMessageQueueClient _mqc, IScheduler _sch)
        {
            MessageProducer3 = _mqp as RabbitMqProducer;
            MessageQueueClient = _mqc as RabbitMqQueueClient;
            Scheduler = _sch;
        }

        [Authenticate]
        public SchedulerResponse Any(SchedulerRequest105 request)
        {
            SchedulerResponse resp = new SchedulerResponse();
            //string redisConnectionString = string.Format(
            //                    "redis://{0}@{1}:{2}",
            //                     Environment.GetEnvironmentVariable(EnvironmentConstants.EB_REDIS_PASSWORD),
            //                     Environment.GetEnvironmentVariable(EnvironmentConstants.EB_REDIS_SERVER),
            //                     Environment.GetEnvironmentVariable(EnvironmentConstants.EB_REDIS_PORT)
            //                    );
            //RedisManagerPool manager = new RedisManagerPool(redisConnectionString);

            //using (IRedisClient Redis = manager.GetClient())
            //{
            EbTask _emailtask = new EbTask { Expression = "0 * * * * ?", JobType = JobTypes.EmailTask };
            EbTask _smstask = new EbTask { Expression = "0 * * * * ?", JobType = JobTypes.SmsTask };
            EbTask _testtask = new EbTask { Expression = "0/5 * * * * ?", JobType = JobTypes.MyJob };
             TaskExecuter(_emailtask, 0).GetAwaiter().GetResult();
             TaskExecuter(_smstask, 1).GetAwaiter().GetResult();
            TaskExecuter(_testtask, 2).GetAwaiter().GetResult();
            //Redis.Set("email_task", _emailtask);
            //Redis.Set("sms_task", _smstask);
            //var Keys = Redis.GetKeysByPattern("*_task*");
            //int counter = 0;
            //foreach (string _key in Keys)
            //{
            //    EbTask _Task = Redis.Get<EbTask>(_key);
            //    TaskExecuter(_Task, counter).GetAwaiter().GetResult();
            //    counter++;
            //}
            //}
            return resp;
        }

        public async Task TaskExecuter(EbTask _task, int counter)
        {
            //NameValueCollection properties = new NameValueCollection();
            //properties["quartz.scheduler.instanceName"] = "MyApplicationScheduler"; // needed if you plan to use the same database for many schedulers
            //properties["quartz.scheduler.instanceId"] = System.Environment.MachineName + DateTime.UtcNow.Ticks; // requires uniqueness
            //properties["quartz.jobStore.type"] = "Quartz.Impl.Redis.RedisJobStore, Quartz.Impl.redis";

            //string conn = string.Format("Server = {0}; Database = {1}; Uid = {2}; Pwd = {3}",
            //    Environment.GetEnvironmentVariable(EnvironmentConstants.EB_INFRA_DB_SERVER),
            //    Environment.GetEnvironmentVariable(EnvironmentConstants.EB_INFRA_DBNAME),
            //    Environment.GetEnvironmentVariable(EnvironmentConstants.EB_INFRA_DB_RW_USER),
            //    Environment.GetEnvironmentVariable(EnvironmentConstants.EB_INFRA_DB_RW_PASSWORD));
            //var properties = new NameValueCollection
            //{
            //    ["quartz.serializer.type"] = "json",
            //    ["quartz.scheduler.instanceName"] = "DotnetCoreScheduler",
            //    ["quartz.scheduler.instanceId"] = "instance_one",
            //    ["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quartz",
            //    ["quartz.threadPool.threadCount"] = "5",
            //    ["quartz.jobStore.misfireThreshold"] = "60000",
            //    ["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz",
            //    ["quartz.jobStore.useProperties"] = "false",
            //    ["quartz.jobStore.dataSource"] = "myDS",
            //    ["quartz.jobStore.tablePrefix"] = "QRTZ_",
            //    ["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.PostgreSQLDelegate, Quartz",
            //    ["quartz.dataSource.myDS.provider"] = "Npgsql ",
            //    ["quartz.dataSource.myDS.connectionString"] = conn
            //};


            try
            {
                //StdSchedulerFactory factory = new Quartz.Impl.StdSchedulerFactory(properties);
                //IScheduler scheduler = factory.GetScheduler().Result;
                JobKey jobKey;
                IJobDetail job = null;
                //scheduler.Start().Wait();

                if (_task.JobType == JobTypes.EmailTask)
                {
                    jobKey = JobKey.Create("EmailJob" + counter);
                    job = JobBuilder.Create<EmailJob>().WithIdentity(jobKey).Build();
                }
                else if (_task.JobType == JobTypes.SmsTask)
                {
                    jobKey = JobKey.Create("SmsJob" + counter);
                    job = JobBuilder.Create<SmsJob>().WithIdentity(jobKey).Build();
                }
                else if (_task.JobType == JobTypes.MyJob)
                {
                    jobKey = JobKey.Create("MyJob" + counter);
                    job = JobBuilder.Create<MyJob>().WithIdentity(jobKey).Build();
                }

                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity("JobTrigger" + counter)
                    .StartNow()
                   .WithSchedule(CronScheduleBuilder.CronSchedule(_task.Expression))//"0/1 * * * * ?"
                    .Build();

                await Scheduler.ScheduleJob(job, trigger);
            }
            catch (Exception e)
            {
                Console.WriteLine("Task Executer Exception : " + e.Message);
            }
        }


        public class EbTask
        {
            public string Expression { get; set; }

            public JobTypes JobType { get; set; }
        }
        public enum JobTypes
        {
            EmailTask = 1,
            SmsTask = 2,
            MyJob = 3
        }
    }
}
