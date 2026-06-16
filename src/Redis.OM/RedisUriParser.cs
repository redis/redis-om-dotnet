using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using StackExchange.Redis;

[assembly: InternalsVisibleTo("Redis.OM.Unit.Tests")]

namespace Redis.OM
{
    /// <summary>
    /// URI parsing utility.
    /// </summary>
    internal static class RedisUriParser
    {
        /// <summary>
        /// Parses a Config options for StackExchange Redis from the URI.
        /// </summary>
        /// <param name="uriString">The URI.</param>
        /// <returns>A configuration options result for SE.Redis.</returns>
        internal static ConfigurationOptions ParseConfigFromUri(string uriString)
        {
            var options = new ConfigurationOptions();

            if (string.IsNullOrEmpty(uriString))
            {
                options.EndPoints.Add("localhost:6379");
                ParseProtocol(options, null);
                return options;
            }

            var uri = new Uri(uriString);
            ParseHost(options, uri);
            ParseUserInfo(options, uri);
            ParseQueryArguments(options, uri);
            ParseProtocol(options, uri);
            ParseDefaultDatabase(options, uri);
            options.Ssl = uri.Scheme == "rediss";
            options.AbortOnConnectFail = false;
            return options;
        }

        /// <summary>
        /// Resolves the RESP protocol to negotiate. An explicit <c>protocol</c> query argument
        /// (<c>resp2</c>/<c>resp3</c> or <c>2</c>/<c>3</c>) takes precedence; otherwise the
        /// <c>REDIS_OM_PROTOCOL</c> environment variable is used as a fallback default. When neither is
        /// supplied the StackExchange.Redis default is left untouched.
        /// </summary>
        /// <param name="options">The configuration options to populate.</param>
        /// <param name="uri">The parsed URI, or <c>null</c> when none was supplied.</param>
        private static void ParseProtocol(ConfigurationOptions options, Uri? uri)
        {
            string? requested = null;
            if (uri is not null && !string.IsNullOrEmpty(uri.Query))
            {
                requested = ParseQuery(uri.Query.Substring(1))
                    .Where(x => x.Key.ToLower() == "protocol")
                    .Select(x => x.Value)
                    .FirstOrDefault();
            }

            requested ??= Environment.GetEnvironmentVariable("REDIS_OM_PROTOCOL");

            if (string.IsNullOrEmpty(requested))
            {
                return;
            }

            switch (requested!.Trim().ToLowerInvariant())
            {
                case "2":
                case "resp2":
                    options.Protocol = RedisProtocol.Resp2;
                    break;
                case "3":
                case "resp3":
                    options.Protocol = RedisProtocol.Resp3;
                    break;
            }
        }

        private static void ParseDefaultDatabase(ConfigurationOptions options, Uri uri)
        {
            if (string.IsNullOrEmpty(uri.AbsolutePath))
            {
                return;
            }

            var dbNumStr = Regex.Match(uri.AbsolutePath, "[0-9]+").Value;
            int dbNum;
            if (int.TryParse(dbNumStr, out dbNum))
            {
                options.DefaultDatabase = dbNum;
            }
        }

        private static IList<KeyValuePair<string, string>> ParseQuery(string query) =>
            query.Split('&').Select(x =>
                new KeyValuePair<string, string>(x.Split('=').First(), x.Split('=').Last())).ToList();

        private static void ParseUserInfo(ConfigurationOptions options, Uri uri)
        {
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var userInfo = uri.UserInfo.Split(':');
                if (userInfo.Length > 1)
                {
                    options.User = Uri.UnescapeDataString(userInfo[0]);
                    options.Password = Uri.UnescapeDataString(userInfo[1]);
                }
                else
                {
                    throw new FormatException(
                        "Username and password must be in the form username:password - if there is no username use the format :password");
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
                if (queryArgs.Any(x => x.Key == "timeout"))
                {
                    var timeout = int.Parse(queryArgs.First(x => x.Key == "timeout").Value);
                    options.AsyncTimeout = timeout;
                    options.SyncTimeout = timeout;
                    options.ConnectTimeout = timeout;
                }

                if (queryArgs.Any(x => x.Key.ToLower() == "clientname"))
                {
                    options.ClientName = queryArgs.First(x => x.Key.ToLower() == "clientname").Value;
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
