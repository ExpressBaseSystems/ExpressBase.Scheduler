using ExpressBase.Common;
using ExpressBase.Common.Structures;
using ExpressBase.Objects.ServiceStack_Artifacts;
using Quartz;
using ServiceStack;
using ServiceStack.RabbitMq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpressBase.Scheduler.Jobs
{
    public class EmailJob : EbJob, IJob
    {      
        public EmailJob()
        {

        }
        public Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.MergedJobDataMap;
            EbJobArguments jobArgs = dataMap["args"] as EbJobArguments;
            MessageProducer.Publish(new EmailAttachmenRequest()
            {
                ObjId = jobArgs.ObjId,
                Params = jobArgs.Params,
                SolnId = jobArgs.SolnId,
                UserId = jobArgs.UserId,
                UserAuthId = jobArgs.UserAuthId
            });
            Console.WriteLine("Email Job queued");
            return Task.FromResult(0);
        }
    }
}
