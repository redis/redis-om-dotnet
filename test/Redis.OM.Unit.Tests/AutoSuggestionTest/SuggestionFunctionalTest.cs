using Redis.OM.Contracts;
using Redis.OM.Searching;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Redis.OM.Unit.Tests.AutoSuggestionTest
{
    [Collection("Redis")]
    public class SuggestionFunctionalTest
    {
        public SuggestionFunctionalTest(RedisSetup setup)
        {
            _connection = setup.Connection;
        }
        private readonly IRedisConnection _connection = null;

        [Fact]
        public void TestAddSuggestion()
        {
            var collection = new RedisCollection<Airport>(_connection);
            var airport1 = new Airport() { Name = "Indira Gandhi International Airport", Code = "DEL", State = "Delhi" };
            var airport2 = new Airport() { Name = "Chennai International Airport", Code = "MAA", State = "Tamil Nadu" };
            var airport3 = new Airport() { Name = "Kempegowda International Airport", Code = "BLR", State = "Karnataka" };
            collection.Insert(airport1);
            collection.Insert(airport2);
            collection.Insert(airport3);
            var type = typeof(Airport);
            _connection.AddSuggestion(type, airport1.Name, 1);
            _connection.AddSuggestion(type, airport1.Code, 1);
            _connection.AddSuggestion(type, airport1.State, 1);
            var listOfSuggestions = _connection.GetSuggestion(type, "De");
            Assert.Equal(2, listOfSuggestions.Count);
            Assert.Contains(listOfSuggestions, x=>x.Contains(airport1.State));
        }

        [Fact]
        public void TestSuggestionWithOptionalParameters()
        {
            var collection = new RedisCollection<Airport>(_connection);
            var airport1 = new Airport() { Name = "Indira Gandhi International Airport", Code = "DEL", State = "Delhi" };
            var airport2 = new Airport() { Name = "Chennai International Airport", Code = "MAA", State = "Tamil Nadu" };
            var airport3 = new Airport() { Name = "Kempegowda International Airport", Code = "BLR", State = "Karnataka" };
            collection.Insert(airport1);
            collection.Insert(airport2);
            collection.Insert(airport3);
            var type = typeof(Airport);
            _connection.AddSuggestion(type, "International Airport1", 1, false, airport1);
            _connection.AddSuggestion(type, "International Airport2", 1, false, airport2);
            _connection.AddSuggestion(type, "International Airport3", 1.0f, false, airport3);
            var listOfSuggestions = _connection.GetSuggestion(type, "International", true, 3, false, true);
            var queries = listOfSuggestions.Where(name => name.Contains('}'));
            var query1 = queries.ToList().First();
            var query2 = queries.ToList().Skip(1).First();
            var query3 = queries.ToList().Skip(2).First();
            var deJson1 = JsonSerializer.Deserialize<Airport>(query1);
            var deJson2 = JsonSerializer.Deserialize<Airport>(query2);
            var deJson3 = JsonSerializer.Deserialize<Airport>(query3);
            Assert.Equal(airport1.Id, deJson1.Id);
            Assert.Equal(airport2.Code, deJson2.Code);
            Assert.Equal(airport3.State, deJson3.State);
        }

        [Fact]
        public void TestDeleteAndLengthOfSuggestion()
        {
            var collection = new RedisCollection<Airport>(_connection);
            var airport1 = new Airport() { Name = "Indira Gandhi International Airport", Code = "DEL", State = "Delhi" };
            var airport2 = new Airport() { Name = "Chennai International Airport", Code = "MAA", State = "Tamil Nadu" };
            var airport3 = new Airport() { Name = "Kempegowda International Airport", Code = "BLR", State = "Karnataka" };
            collection.Insert(airport1);
            collection.Insert(airport2);
            collection.Insert(airport3);
            var type = typeof(Airport);
            _connection.AddSuggestion(type, airport3.Name, 1);
            _connection.AddSuggestion(type, airport3.State, 1);
            _connection.AddSuggestion(type, airport2.Code, 1);
            _connection.DeleteSuggestion(type, "MAA");
            var addedSuggestionCount = _connection.AddSuggestion(type, "BLR", 1);
            var lengthOfAddSuggestioned = _connection.GetSuggestionLength(type);
            Assert.Equal(lengthOfAddSuggestioned, addedSuggestionCount);
        }
    }
}
