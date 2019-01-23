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

        public ScheduleResponse Any(ScheduleRequest request)
        {
            ScheduleResponse resp = new ScheduleResponse();
            TaskExecuter(request.Task).GetAwaiter().GetResult();
            return resp;
        }

        public RescheduleResponse Any(RescheduleRequest request)
        {
            Scheduler.GetJobDetail(new JobKey(request.JobKey));
            Scheduler.RescheduleJob(new TriggerKey(request.TriggerKey), CreateTrigger(request.Task));
            return new RescheduleResponse();
        }

        public UnscheduleResponse Any(UnscheduleRequest request)
        {
            UnscheduleResponse resp = new UnscheduleResponse();
            Scheduler.UnscheduleJob(new TriggerKey(request.TriggerKey));
            return resp;
        }
        public DeleteJobResponse Any(DeleteJobRequest request)
        {
            DeleteJobResponse resp = new DeleteJobResponse();
            Scheduler.DeleteJob(new JobKey(request.JobKey));
            return resp;
        }

        public async Task TaskExecuter(EbTask _task)
        {
            try
            {
                IJobDetail job = CreateJob(_task);
                ITrigger trigger = CreateTrigger(_task);
                await Scheduler.ScheduleJob(job, trigger);

                MessageProducer3.Publish(new AddSchedulesToSolutionRequest()
                {
                    Task = _task,
                    SolnId = _task.JobArgs.SolnId,
                    UserId = _task.JobArgs.UserId,
                    UserAuthId = _task.JobArgs.UserAuthId,
                    JobKey = job.Key.Name,
                    TriggerKey = trigger.Key.Name,
                    Status = ScheduleStatuses.Active,
                    ObjId = _task.JobArgs.ObjId,
                    Name = _task.Name
                });

                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(job.Key + " Job Scheduled");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception e)
            {
                Console.WriteLine("Task Executer Exception : " + e.Message);
            }
        }

        public ITrigger CreateTrigger(EbTask _task)
        {
            ITrigger trigger = TriggerBuilder.Create()
                   .WithIdentity("T-" + _task.JobArgs.SolnId + "-" + _task.JobArgs.ObjId + "-" + _task.Expression + "-" + DateTime.Now)
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
            _dataMap.Add("args", _task.JobArgs);
            if (_task.JobType == JobTypes.EmailTask)
            {
                jobKey = JobKey.Create("Email_" + DateTime.Now);
                job = JobBuilder.Create<EmailJob>().WithIdentity(jobKey).UsingJobData(_dataMap).Build();
            }
            else if (_task.JobType == JobTypes.SmsTask)
            {
                jobKey = JobKey.Create("Sms" + DateTime.Now);
                job = JobBuilder.Create<SmsJob>().WithIdentity(jobKey).UsingJobData(_dataMap).Build();
            }
            else if (_task.JobType == JobTypes.ReportTask)
            {
                jobKey = JobKey.Create("Report" + DateTime.Now);
                job = JobBuilder.Create<ReportJob>().WithIdentity(jobKey).UsingJobData(_dataMap).Build();
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
