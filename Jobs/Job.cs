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
    public class SmsJob : IJob
    {
        private RabbitMqProducer _rabbitMqProducer = null;

        RabbitMqProducer RabbitMqProducer
        {
            get
            {
                if (_rabbitMqProducer == null)
                {
                    RabbitMqMessageFactory rabitFactory = new RabbitMqMessageFactory();
                    rabitFactory.ConnectionFactory.UserName = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_USER);
                    rabitFactory.ConnectionFactory.Password = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_PASSWORD);
                    rabitFactory.ConnectionFactory.HostName = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_HOST);
                    rabitFactory.ConnectionFactory.Port = Convert.ToInt32(Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_PORT));
                    rabitFactory.ConnectionFactory.VirtualHost = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_VHOST);
                    var mqserver = new RabbitMqServer(rabitFactory);
                    _rabbitMqProducer = mqserver.CreateMessageProducer() as RabbitMqProducer;
                }
                return _rabbitMqProducer;
            }
        }

        public SmsJob()
        {

        }
        public Task Execute(IJobExecutionContext context)
        {
            using (var client = new HttpClient())
            {
                Authenticate content = new Authenticate
                {
                    provider = "Credentials",
                    UserName = "binivarghese@gmail.com",
                    Password = ("123123123binivarghese@gmail.com").ToMD5Hash(),
                    Meta = new Dictionary<string, string> { { "wc", "uc" }, { "cid", "al_arz_sales" } },
                };
                var httpContent = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
                var response = client.PostAsync("http://localhost:41600/authenticate", httpContent).Result;

                if (response.IsSuccessStatusCode)
                {
                    int val = 1332423;
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    MyAuthenticateResponse r = JsonConvert.DeserializeObject<MyAuthenticateResponse>(responseContent);
                    List<Param> _param = new List<Param> { new Param { Name = "ids", Type = ((int)EbDbTypes.Int32).ToString(), Value = val.ToString() } };
                    JsonServiceClient ServiceClient = new JsonServiceClient("http://localhost:41600");
                    ServiceClient.BearerToken = r.BearerToken;
                    ServiceClient.RefreshToken = r.RefreshToken;

                    ServiceClient.Post(new SMSSentRequest
                    {
                        ObjId = 2236,
                        Params = _param
                    });


                }
            }
            Console.WriteLine("Sms Job Executed");
            return Task.FromResult(0);
        }
    }
    public class EmailJob : IJob
    {
        private RabbitMqProducer _msqProducer = null;

        RabbitMqProducer MessageProducer
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

        public EmailJob()
        {

        }
        public Task Execute(IJobExecutionContext context)
        {
            using (var client = new HttpClient())
            {
                Authenticate content = new Authenticate
                {
                    provider = "Credentials",
                    UserName = "binivarghese@gmail.com",
                    Password = ("123123123binivarghese@gmail.com").ToMD5Hash(),
                    Meta = new Dictionary<string, string> { { "wc", "uc" }, { "cid", "al_arz_sales" } },
                };
                var httpContent = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
                var response = client.PostAsync("http://localhost:41600/authenticate", httpContent).Result;

                if (response.IsSuccessStatusCode)
                {
                    int val = 1332423;
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    MyAuthenticateResponse r = JsonConvert.DeserializeObject<MyAuthenticateResponse>(responseContent);
                    List<Param> _param = new List<Param> { new Param { Name = "ids", Type = ((int)EbDbTypes.Int32).ToString(), Value = val.ToString() } };
                    JsonServiceClient ServiceClient = new JsonServiceClient("http://localhost:41600")
                    {
                        BearerToken = r.BearerToken,
                        RefreshToken = r.RefreshToken
                    };
                    //ServiceClient.Post(new PdfCreateServiceMqRequest
                    //{
                    //    ObjId = 2174,
                    //    Params = _param
                    //});
                    MessageProducer.Publish(new PdfCreateServiceRequest()
                    {
                        Id = /*request.ObjId*/2174,
                        Params = _param,
                        UserId = 1,
                        //UserAuthId = request.UserAuthId,
                        SolnId = /*request.SolnId*/ "al_arz_sales"
                    });

                }
            }
            Console.WriteLine("Email Job Executed");
            return Task.FromResult(0);
        }
    }
}