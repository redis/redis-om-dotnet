using Redis.OM.Full_Text_Search_within_an_Index.Models;
using Redis.OM.Full_Text_Search_within_an_Index.RedisHelper;
using System;

namespace Redis.OM.Full_Text_SearchWithin_an_Index
{
    public class Program
    {

        private static void Main(string[] args)
        {
            // connect
            var provider = new RedisConnectionProvider("redis://localhost:6379");
            var connection = provider.Connection;
            var _movieCollection = provider.RedisCollection<Movie>();

            // Create index
            connection.CreateIndex(typeof(Movie));
            connection.CreateIndex(typeof(Awards));

            var redisHelper = new RedisHelper(provider);
            redisHelper.InitializeCustomers();
            
            //Get the list of movies which year's is eq to 2003
            var getListofMovie=_movieCollection.Where(x => x.Year == 2003).ToList();
            foreach (Movie res in getListofMovie)
            {
                Console.WriteLine(res.Title);
            }
        }
    }
}