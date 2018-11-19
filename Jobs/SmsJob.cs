using ExpressBase.Common.Structures;
using ExpressBase.Objects.ServiceStack_Artifacts;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpressBase.Scheduler.Jobs
{
    public class SmsJob : EbJob, IJob
    {    
        public SmsJob()
        {

        }
        public Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.MergedJobDataMap;
            EbJobArguments jobArgs = dataMap["args"] as EbJobArguments;
            MessageProducer.Publish(new SMSCreateRequest
            {
                ObjId = jobArgs.ObjId,
                Params = jobArgs.Params,
                SolnId = jobArgs.SolnId,
                UserId = jobArgs.UserId,
                UserAuthId = jobArgs.UserAuthId,
                //MediaUrl = request.MediaUrl
            });
            Console.WriteLine("Sms Job queued");
            return Task.FromResult(0);
        }
    }
}
