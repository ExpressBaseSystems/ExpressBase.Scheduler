using ExpressBase.Common;
using ExpressBase.Common.Data;
using ExpressBase.Common.Extensions;
using ExpressBase.Common.Structures;
using ExpressBase.Objects.ServiceStack_Artifacts;
using Newtonsoft.Json;
using Quartz;
using ServiceStack;
using ServiceStack.Messaging;
using ServiceStack.RabbitMq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExpressBase.Scheduler.Jobs
{
    public class MyJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Inside Job");
            return context.AsTaskResult();
        }
    }

    public abstract class EbJob
    {
        private RabbitMqProducer _msqProducer = null;

        protected RabbitMqProducer MessageProducer
        {
            get
            {
                if (_msqProducer == null)
                {
                    RabbitMqMessageFactory rabitFactory = new RabbitMqMessageFactory();
                    rabitFactory.ConnectionFactory.UserName = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_USER);
                    rabitFactory.ConnectionFactory.Password = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_PASSWORD);
                    rabitFactory.ConnectionFactory.HostName = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_HOST);
                    rabitFactory.ConnectionFactory.Port = Convert.ToInt32(Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_PORT));
                    rabitFactory.ConnectionFactory.VirtualHost = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_VHOST);
                    var mqserver = new RabbitMqServer(rabitFactory);
                    _msqProducer = mqserver.CreateMessageProducer() as RabbitMqProducer;
                }
                return _msqProducer;
            }
        }
    }
}