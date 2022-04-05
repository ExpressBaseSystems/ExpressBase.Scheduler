using ExpressBase.Common.Structures;
using ExpressBase.Objects.ServiceStack_Artifacts;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Threading.Tasks;

namespace ExpressBase.Scheduler.Jobs
{
    public class ApiJob : EbJob, IJob
    {
        public ApiJob()
        {

        }

        public Task Execute(IJobExecutionContext context)
        {
            EbJobArguments jobArgs = null;
            try
            {
                JobDataMap dataMap = context.MergedJobDataMap;
                jobArgs = JsonConvert.DeserializeObject<EbJobArguments>(dataMap["args"].ToString());

                MessageProducer.Publish(new ApiMqRequest()
                {
                    JobArgs = jobArgs
                });

                Console.WriteLine(jobArgs?.RefId + " Executed at " + DateTime.Now);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in Api Job Execution , Refid: " + jobArgs?.RefId + e.Message);
            }
            return Task.FromResult(0);
        }
    }
}
