using ExpressBase.Common.Structures;
using ExpressBase.Objects.ServiceStack_Artifacts;
using Quartz;
using System;
using System.Threading.Tasks;

namespace ExpressBase.Scheduler.Jobs
{
    public class ReportJob : EbJob, IJob
    {
        public ReportJob()
        {

        }
        public Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.MergedJobDataMap;
            EbJobArguments jobArgs = dataMap["args"] as EbJobArguments;
             MessageProducer.Publish(new ReportInternalRequest()
            {
                JobArgs = jobArgs
            });

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Report Job queued. Job: " + context.JobDetail.Key +" trigger: "+context.Trigger.Key);
            Console.ForegroundColor = ConsoleColor.White;

            return Task.FromResult(0);
        }
    }
}
