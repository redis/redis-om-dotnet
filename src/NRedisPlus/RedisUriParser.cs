using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StackExchange.Redis;
namespace NRedisPlus
{
    public static class RedisUriParser
    {
        public static ConfigurationOptions ParseConfigFromUri(string url)
        {
            var options = new ConfigurationOptions();

            if (string.IsNullOrEmpty(url))
            {
                options.EndPoints.Add("localhost:6379");
                return options;
            }
                
            var uri = new Uri(url);
            ParseHost(options, uri);
            ParseUserInfo(options, uri);
            ParseQueryArguments(options, uri);
            ParseDefaultDatabase(options, uri);
            options.Ssl = uri.Scheme == "rediss";
            options.AbortOnConnectFail = false;
            return options;
        }

        private static void ParseDefaultDatabase(ConfigurationOptions options, Uri uri)
        {
            if (string.IsNullOrEmpty(uri.AbsolutePath)) return;
            var dbNumStr = Regex.Match(uri.AbsolutePath,"[0-9]+").Value;
            int dbNum;
            if (int.TryParse(dbNumStr, out dbNum))
                options.DefaultDatabase = dbNum;
        }
        
        private static IList<KeyValuePair<string, string>> ParseQuery(string query) =>
            query.Split('&').Select(x => 
                new KeyValuePair<string,string>(x.Split('=').First(), x.Split('=').Last())).ToList();

        private static void ParseUserInfo(ConfigurationOptions options, Uri uri)
        {
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var userInfo = uri.UserInfo.Split(':');
                if (userInfo.Length > 1)
                {
                    options.User = userInfo[0];
                    options.Password = userInfo[1];
                }
                else
                {
                    options.Password = userInfo[0];
                }
            }
        }

        private static void ParseHost(ConfigurationOptions options, Uri uri)
        {
            var port = uri.Port >= 0 ? uri.Port : 6379;
            var host = !string.IsNullOrEmpty(uri.Host) ? uri.Host : "localhost";
            options.EndPoints.Add($"{host}:{port}");
        }

        private static void ParseQueryArguments(ConfigurationOptions options, Uri uri)
        {
            if (!string.IsNullOrEmpty(uri.Query))
            {
                var queryArgs = ParseQuery(uri.Query.Substring(1));
                if (queryArgs.Any(x=>x.Key == "timeout"))
                {
                    var timeout = int.Parse(queryArgs.First(x=>x.Key == "timeout").Value);
                    options.AsyncTimeout = timeout;
                    options.SyncTimeout = timeout;
                    options.ConnectTimeout = timeout;
                }

                if (queryArgs.Any(x=>x.Key.ToLower() == "clientname"))
                {
                    options.ClientName = queryArgs.First(x=>x.Key.ToLower() == "clientname").Value;
                }

                if (queryArgs.Any(x => x.Key.ToLower() == "sentinel_primary_name"))
                {
                    options.ServiceName = queryArgs.First(x => x.Key.ToLower() == "sentinel_primary_name").Value;
                }
                    

                foreach (var endpoint in queryArgs.Where(x => x.Key == "endpoint").Select(x => x.Value))
                {
                    options.EndPoints.Add(endpoint);
                }
                
                
            }
        }
    }
}