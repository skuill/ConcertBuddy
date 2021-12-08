using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda;
using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ConcertBuddy.Bot.AWSServerless
{
    public class Functions
    {
        private readonly AmazonLambdaClient _lambdaClient;
        private readonly Connect _connect;

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
            _lambdaClient = new AmazonLambdaClient();
            _connect = new Connect();
        }

        public async Task<string> FunctionHandler(JObject request, ILambdaContext context)
        {
            LambdaLogger.Log("REQUEST: " + JsonConvert.SerializeObject(request));
            try
            {
                var updateEvent = request.ToObject<Update>();
                await _connect.RespFromTelegram(updateEvent);
            }
            catch (Exception e)
            {
                LambdaLogger.Log("exception: " + e.Message);
            }

            return "Hello from AWS Lambda" + DateTimeOffset.UtcNow.ToString();
        }

        /// <summary>
        /// A Lambda function to respond to HTTP Get methods from API Gateway
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The API Gateway response.</returns>
        public APIGatewayProxyResponse Get(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("Get Request\n");

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = "Hello AWS Serverless",
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };

            return response;
        }
    }
}
