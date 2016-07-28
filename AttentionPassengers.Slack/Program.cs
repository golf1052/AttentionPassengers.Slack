using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using AttentionPassengers.Slack.Constants;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Conventions;
using AttentionPassengers.Slack.Dto;
using MongoDB.Bson;

namespace AttentionPassengers.Slack
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppConstants.Mongo = new MongoClient(Secrets.MongoConnectionString);
            AppConstants.Database = AppConstants.Mongo.GetDatabase("mbta");
            AppConstants.UserCollection = AppConstants.Database.GetCollection<User>("users");
            ConventionPack pack = new ConventionPack();
            pack.Add(new EnumRepresentationConvention(BsonType.String));
            ConventionRegistry.Register("EnumStringConvention", pack, t => true);

            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .Build();
            
            var host = new WebHostBuilder()
                .UseConfiguration(config)
                .UseKestrel()
                .UseUrls("http://127.0.0.1:8891")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            AppConstants.LoadContent();
            host.Run();
        }
    }
}
