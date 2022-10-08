using Redis.OM.Full_Text_Search_within_an_Index.Models;
using Redis.OM.Full_Text_Search_within_an_Index.RedisHelper;
using System;

namespace Redis.OM.Full_Text_SearchWithin_an_Index
{
    public class Program
    {
        static void ShowMovies(string type, List<Movie> movies)
        {
            Console.WriteLine($"Movies {type}: {string.Join(", ", movies.Select(x => $"{x.Title}  Year: {x.Year}"))}, total: {movies.Count}.");
        }
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

            //BasicFirstOrDefaultQuery
            // get first or default null value from movies redis limit set to 0 to 1
            var movieWithFirstorDefault = _movieCollection.FirstOrDefault<Movie>(x => x.ImdbRating >5.5);
            Console.WriteLine($"Title of the movie:{movieWithFirstorDefault.Title} and ImdbRating of the movie is {movieWithFirstorDefault.ImdbRating}");

            //BasicQueryWithExactNumericMatch
            //Get the list of movies which year's is eq to 2003
            var getListofMovie = _movieCollection.Where(x => x.Year == 2003).ToList();
            ShowMovies("Year is 2003", getListofMovie);

            //QueryWithNegation
            var negationMovieList = _movieCollection.Where( x => x.Runtime >= 178 && x.ImdbRating!=8.0).ToList();
            ShowMovies("Negation in MovieList", negationMovieList);

            //FirstOrDefaultWithMixedLocals
            var rating = 8.8;
            var searchmixedLocals = new Movie();
            var runtimeList = new List<int> { 178, 200 };
            foreach (var runtime in runtimeList)
            {
                searchmixedLocals = _movieCollection.FirstOrDefault<Movie>(x => x.ImdbRating == rating && x.Runtime == runtime);
            }
            Console.WriteLine($"MixedLocals with Movie: {searchmixedLocals.Title} and Runtime: {searchmixedLocals.Runtime}");

            //QueryWithContains
            var containsMovieTitle = _movieCollection.Where(x => x.Title.Contains("The Lord of")).ToList();
            ShowMovies("Contains title for The Lord of", containsMovieTitle);

            //QueryWithContainsNegation
            var containsMovieTitleWithNegation = _movieCollection.Where(x => !x.Title.Contains("The Lord of")).ToList();
            ShowMovies("Contains title with Negation for The Lord of ", containsMovieTitleWithNegation);

            //NestedObjectStringSearch
            var nestedAwardsSearch = _movieCollection.Where(x => x.Awards.Text=="1 win").ToList();
            ShowMovies("with only one win award",nestedAwardsSearch);

            //Prefix Matching of the movie title
            var prefixMatchTitle = _movieCollection.Where(x => x.Title.Contains("Jur*")).ToList();
            ShowMovies("with PrefixMatch of Jur* ", prefixMatchTitle);

            //search with contains
            var oscarsWinningMovies = _movieCollection.Where(x => x.Awards.Text.Contains("Oscars")).ToList();
            ShowMovies("won Oscars", oscarsWinningMovies);

            //ListContains 
            var searchForListOfCastInMovie = _movieCollection.Where(x => x.Cast.Contains("Sean Astin")).ToList();
            ShowMovies("Cast By Sean Astin", searchForListOfCastInMovie);

            //Sort the search result ascending use OrdreByDecending for DESC 
            var sortByASC = _movieCollection.OrderBy(x => x.Year).ToList();
            ShowMovies("sort by year", sortByASC);

            //embeddedobject with multiple 
            var embeddedMovieSearch = _movieCollection.Where(x => x.Awards.Wins == 175 && x.Writers.Contains("J.R.R. Tolkien (novel)")).ToList();
            ShowMovies("search by Writer with Awards won count is 175",embeddedMovieSearch);

            //drop index
            connection.DropIndex(typeof(Movie));
            connection.DropIndex(typeof(Awards));
        }
    }
}
