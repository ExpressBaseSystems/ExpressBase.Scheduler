using ExpressBase.Common.Structures;
using ExpressBase.Objects.ServiceStack_Artifacts;
using Quartz;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpressBase.Scheduler.Jobs
{
    public class SqlTask : EbJob, IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.MergedJobDataMap;
            EbJobArguments jobArgs = dataMap["args"] as EbJobArguments;

            MessageProducer.Publish(new SqlJobInternalRequest
            {
                GlobalParams = jobArgs.Params,
                ObjId =jobArgs.ObjId,
                SolnId = jobArgs.SolnId,
                UserId = jobArgs.UserId,
                UserAuthId = jobArgs.UserAuthId
            });

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Sql Job queued");
            Console.ForegroundColor = ConsoleColor.White;
            return Task.FromResult(0);
        }
    }
}
