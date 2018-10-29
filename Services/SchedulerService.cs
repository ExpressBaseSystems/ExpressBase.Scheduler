using ExpressBase.Common;
using ExpressBase.Common.Data;
using ExpressBase.Common.ServiceClients;
using ExpressBase.Common.Structures;
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

        public SchedulerResponse Any(SchedulerRequest request)
        {
            SchedulerResponse resp = new SchedulerResponse();
            TaskExecuter(request.Task).GetAwaiter().GetResult();
            return resp;
        }

        public async Task TaskExecuter(EbTask _task)
        {
            try
            {
                IJobDetail job = CreateJob(_task);
                ITrigger trigger = CreateTrigger(_task);
                await Scheduler.ScheduleJob(job, trigger);
            }
            catch (Exception e)
            {
                Console.WriteLine("Task Executer Exception : " + e.Message);
            }
        }

        public ITrigger CreateTrigger(EbTask _task)
        {
            ITrigger trigger = TriggerBuilder.Create()
                   .WithIdentity("JobTrigger" + DateTime.Now)
                   .StartNow()
                  .WithSchedule(CronScheduleBuilder.CronSchedule(_task.Expression))
                   .Build();
            return trigger;
        }

        public IJobDetail CreateJob(EbTask _task)
        {
            JobKey jobKey;
            IJobDetail job = null;
            JobDataMap _dataMap = new JobDataMap();
            EbJobArgs dat = new EbJobArgs { ObjId = _task.ObjId, Params = _task.Params, SolnId = _task.SolnId, UserId = 1 };
            _dataMap.Add("args", dat);
            if (_task.JobType == JobTypes.EmailTask)
            {
                jobKey = JobKey.Create("EmailJob" + DateTime.Now);
                job = JobBuilder.Create<EmailJob>().WithIdentity(jobKey).UsingJobData(_dataMap).Build();
            }
            else if (_task.JobType == JobTypes.SmsTask)
            {
                jobKey = JobKey.Create("SmsJob" + DateTime.Now);
                job = JobBuilder.Create<SmsJob>().WithIdentity(jobKey).UsingJobData(_dataMap).Build();
            }
            else if (_task.JobType == JobTypes.MyJob)
            {
                jobKey = JobKey.Create("MyJob" + DateTime.Now);
                job = JobBuilder.Create<MyJob>().WithIdentity(jobKey).Build();
            }
            return job;
        }
    }
}
