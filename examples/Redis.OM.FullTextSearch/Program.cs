using Redis.OM.FullTextSearch.Models;
using Redis.OM.FullTextSearch.RedisHelper;
using System;
using Redis.OM;

static void ShowMovies(string type, List<Movie> movies)
{
    Console.WriteLine($"Movies {type}: {string.Join(", ", movies.Select(x => $"{x.Title}  Year: {x.Year}"))}, total: {movies.Count}.");
}


// connect
var provider = new RedisConnectionProvider("redis://localhost:6379");
var connection = provider.Connection;
var movieCollection = provider.RedisCollection<Movie>();

// Create index
connection.CreateIndex(typeof(Movie));
connection.CreateIndex(typeof(Awards));

var redisHelper = new RedisHelper(provider);
redisHelper.InitializeCustomers();

//BasicFirstOrDefaultQuery
// get first or default null value from movies redis limit set to 0 to 1
var movieWithFirstOrDefault = movieCollection.FirstOrDefault<Movie>(x => x.ImdbRating >5.5);
Console.WriteLine($"Title of the movie:{movieWithFirstOrDefault.Title} and ImdbRating of the movie is {movieWithFirstOrDefault.ImdbRating}");

//BasicQueryWithExactNumericMatch
//Get the list of movies which year's is eq to 2003
var getListOfMovie = movieCollection.Where(x => x.Year == 2003).ToList();
ShowMovies("Year is 2003", getListOfMovie);

//QueryWithNegation
var negationMovieList = movieCollection.Where( x => x.Runtime >= 178 && x.ImdbRating!=8.0).ToList();
ShowMovies("Negation in MovieList", negationMovieList);

//FirstOrDefaultWithMixedLocals
var rating = 8.8;
var searchMixedLocals = new Movie();
var runtimeList = new List<int> { 178, 200 };
foreach (var runtime in runtimeList)
{
    searchMixedLocals = movieCollection.FirstOrDefault<Movie>(x => x.ImdbRating == rating && x.Runtime == runtime);
}
Console.WriteLine($"MixedLocals with Movie: {searchMixedLocals.Title} and Runtime: {searchMixedLocals.Runtime}");

//QueryWithContains
var containsMovieTitle = movieCollection.Where(x => x.Title.Contains("The Lord of")).ToList();
ShowMovies("Contains title for The Lord of", containsMovieTitle);

//QueryWithContainsNegation
var containsMovieTitleWithNegation = movieCollection.Where(x => !x.Title.Contains("The Lord of")).ToList();
ShowMovies("Contains title with Negation for The Lord of ", containsMovieTitleWithNegation);

//NestedObjectStringSearch
var nestedAwardsSearch = movieCollection.Where(x => x.Awards.Text=="1 win").ToList();
ShowMovies("with only one win award",nestedAwardsSearch);

//Prefix Matching of the movie title
var prefixMatchTitle = movieCollection.Where(x => x.Title.Contains("Jur*")).ToList();
ShowMovies("with PrefixMatch of Jur* ", prefixMatchTitle);

//search with contains
var oscarsWinningMovies = movieCollection.Where(x => x.Awards.Text.Contains("Oscars")).ToList();
ShowMovies("won Oscars", oscarsWinningMovies);

//ListContains 
var searchForListOfCastInMovie = movieCollection.Where(x => x.Cast.Contains("Sean Astin")).ToList();
ShowMovies("Cast By Sean Astin", searchForListOfCastInMovie);

//Sort the search result ascending and use OrdreByDecending for DESC 
var sortByASC = movieCollection.OrderBy(x => x.Year).ToList();
ShowMovies("sort by year", sortByASC);

//embeddedobject with multiple 
var embeddedMovieSearch = movieCollection.Where(x => x.Awards.Wins == 175 && x.Writers.Contains("J.R.R. Tolkien (novel)")).ToList();
ShowMovies("search by Writer with Awards won count is 175",embeddedMovieSearch);



//drop index
connection.DropIndex(typeof(Movie));
connection.DropIndex(typeof(Awards));