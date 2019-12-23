using ExpressBase.Common.Structures;
using Quartz;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpressBase.Scheduler.Jobs
{
    public class SqlTask:IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.MergedJobDataMap;
            EbJobArguments jobArgs = dataMap["args"] as EbJobArguments;
         
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Sql Job queued");
            Console.ForegroundColor = ConsoleColor.White;
            return Task.FromResult(0);
        }
    }
}
