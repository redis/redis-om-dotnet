using Redis.OM.Contracts;
using Redis.OM.Searching;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Redis.OM.Unit.Tests.AutoSuggestionTest
{
    [Collection("Redis")]
    public class SuggestionFunctionalTest
    {
        private readonly Type type = typeof(Airport);
        public SuggestionFunctionalTest(RedisSetup setup)
        {
            _connection = setup.Connection;
        }
        private readonly IRedisConnection _connection = null;

        [Fact]
        public void TestAddAndGetSuggestion()
        {
            var collection = new RedisCollection<Airport>(_connection);
            var airport1 = new Airport() { Name = "Indira Gandhi International Airport", Code = "DEL", State = "Delhi" };
            var airport2 = new Airport() { Name = "Chennai International Airport", Code = "MAA", State = "Tamil Nadu" };
            var airport3 = new Airport() { Name = "Kempegowda International Airport", Code = "BLR", State = "Karnataka" };
            collection.Insert(airport1);
            collection.Insert(airport2);
            collection.Insert(airport3);
            _connection.AddSuggestion(type, airport1.Name, 1);
            _connection.AddSuggestion(type, airport1.Code, 1);
            _connection.AddSuggestion(type, airport1.State, 1);
            var listOfSuggestions = _connection.GetSuggestion(type, "De");
            Assert.Equal(2, listOfSuggestions.Length);
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
            _connection.AddSuggestion(type, "International Airport1", 1, false, airport1);
            _connection.AddSuggestion(type, "International Airport2", 1, false, airport2);
            _connection.AddSuggestion(type, "International Airport3", 1.0f, false, airport3);
            var listOfSuggestions = _connection.GetSuggestion(type, "International", true, 3, false, true);
            var query1 = listOfSuggestions.Skip(1).First();
            var query2 = listOfSuggestions.Skip(3).First();
            var query3 = listOfSuggestions.Skip(5).First();
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
            _connection.AddSuggestion(type, airport3.Name, 1);
            _connection.AddSuggestion(type, airport3.State, 1);
            _connection.AddSuggestion(type, airport2.Code, 1);
            _connection.DeleteSuggestion(type, "MAA");
            var addedSuggestionCount = _connection.AddSuggestion(type, "BLR", 1);
            var lengthOfAddSuggestioned = _connection.GetSuggestionLength(type);
            Assert.Equal(lengthOfAddSuggestioned, addedSuggestionCount);
        }

        [Fact]
        public async Task TestAddAndGetSuggestionAsync()
        {
            var collection = new RedisCollection<Airport>(_connection);
            var airport1 = new Airport() { Name = "Indira Gandhi International Airport", Code = "DEL", State = "Delhi" };
            var airport2 = new Airport() { Name = "Chennai International Airport", Code = "MAA", State = "Tamil Nadu" };
            var airport3 = new Airport() { Name = "Kempegowda International Airport", Code = "BLR", State = "Karnataka" };
            await collection.InsertAsync(airport1);
            await collection.InsertAsync(airport2);
            await collection.InsertAsync(airport3);
            await _connection.AddSuggestionAsync(type, airport1.Name, 1);
            await _connection.AddSuggestionAsync(type, airport1.Code, 1);
            await _connection.AddSuggestionAsync(type, airport1.State, 1);
            var listOfSuggestions = await _connection.GetSuggestionAsync(type, "De");
            Assert.Equal(2, listOfSuggestions.Length);
        }


        [Fact]
        public async Task TestSuggestionWithOptionalParametersAsync()
        {
            var collection = new RedisCollection<Airport>(_connection);
            var airport1 = new Airport() { Name = "Indira Gandhi International Airport", Code = "DEL", State = "Delhi" };
            var airport2 = new Airport() { Name = "Chennai International Airport", Code = "MAA", State = "Tamil Nadu" };
            var airport3 = new Airport() { Name = "Kempegowda International Airport", Code = "BLR", State = "Karnataka" };
            await collection.InsertAsync(airport1);
            await collection.InsertAsync(airport2);
            await collection.InsertAsync(airport3);
            await _connection.AddSuggestionAsync(type, "International Airport1", 1, false, airport1);
            await _connection.AddSuggestionAsync(type, "International Airport2", 1, false, airport2);
            await _connection.AddSuggestionAsync(type, "International Airport3", 1.0f, false, airport3);
            var listOfSuggestions = await _connection.GetSuggestionAsync(type, "International", true, 3, false, true);
            var query1 = listOfSuggestions.Skip(1).First();
            var query2 = listOfSuggestions.Skip(3).First();
            var query3 = listOfSuggestions.Skip(5).First();
            var deJson1 = JsonSerializer.Deserialize<Airport>(query1.ToString());
            var deJson2 = JsonSerializer.Deserialize<Airport>(query2.ToString());
            var deJson3 = JsonSerializer.Deserialize<Airport>(query3.ToString());
            Assert.Equal(airport1.Id, deJson1.Id);
            Assert.Equal(airport2.Code, deJson2.Code);
            Assert.Equal(airport3.State, deJson3.State);
        }

        [Fact]
        public async Task TestDeleteAndLengthOfSuggestionAsync()
        {
            var collection = new RedisCollection<Airport>(_connection);
            var airport1 = new Airport() { Name = "Indira Gandhi International Airport", Code = "DEL", State = "Delhi" };
            var airport2 = new Airport() { Name = "Chennai International Airport", Code = "MAA", State = "Tamil Nadu" };
            var airport3 = new Airport() { Name = "Kempegowda International Airport", Code = "BLR", State = "Karnataka" };
            await collection.InsertAsync(airport1);
            await collection.InsertAsync(airport2);
            await collection.InsertAsync(airport3);
            await _connection.AddSuggestionAsync(type, airport3.Name, 1);
            await _connection.AddSuggestionAsync(type, airport3.State, 1);
            await _connection.AddSuggestionAsync(type, airport2.Code, 1);
            await _connection.DeleteSuggestionAsync(type, "MAA");
            var addedSuggestionCount = await _connection.AddSuggestionAsync(type, "BLR", 1);
            var lengthOfAddSuggestioned = await _connection.GetSuggestionLengthAsync(type);
            Assert.Equal(lengthOfAddSuggestioned, addedSuggestionCount);
        }
    }
}