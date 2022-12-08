using Redis.OM.FullTextSearch.Models;
using Redis.OM.Searching;


namespace Redis.OM.FullTextSearch.RedisHelper
{
    public class RedisHelper
    {
        private readonly IRedisCollection<Movie> movieCollection;

        public RedisHelper(RedisConnectionProvider provider)
        {
            movieCollection = provider.RedisCollection<Movie>();
        }

        public void InitializeMovies()
        {
            var count = movieCollection.Count();
            if (count > 0)
            {
                // not re-add when already initialize
                return;
            }

            Console.WriteLine("Initialize Movie Data...");

            movieCollection.Insert(new Movie()
            {
                Title = "The Manson Family",
                Year = 2003,
                Plot = "If you think you know the story of the Manson Family, you are dead wro…",
                Languages = (new List<string> { "English" }),
                Released = new DateTime(2003,05,23),
                Cast = new List<string> { "Marcelo Games", "Marc Pitman", "Leslie Orr", "Maureen Allisse" },
                Directors = new List<string> { "Jim Van Bebber" },
                Writers = new List<string> { "Jim Van Bebber" },
                Runtime = 95,
                Genres = new List<Genres> { Genres.Crime,Genres.History,Genres.Drama},
                Awards = new Awards()
                {
                    Wins = 1,
                    Nominations = 1,
                    Text = "1 win"

                },
                ImdbRating = 5.5,
                Country = Country.USA


            }) ;
            movieCollection.Insert(new Movie()
            {
                Title = "Jurassic World",
                Year = 2015,
                Plot = "A new theme park is built on the original site of Jurassic Park. Every…",
                Languages = (new List<string> { "English", "Tamil" }),
                Released = new DateTime(2015, 06, 12),
                Cast = new List<string> {"Chris Pratt","Bryce Dallas Howard","Irrfan Khan","Vincent D'Onofrio" },
                Directors = new List<string> { "Colin Trevorrow" },
                Writers = new List<string> {"Rick Jaffa (screenplay)","Amanda Silver (screenplay)","Colin Trevorrow (screenplay)","Derek Connolly (screenplay)","Rick Jaffa (story)","Amanda Silver (story)","Michael Crichton (characters)"},
                Runtime = 124,
                Genres = new List<Genres> { Genres.Action, Genres.Adventure, Genres.Scifi },
                Awards = new Awards()
                {
                    Wins = 0,
                    Nominations = 5,
                    Text = "5 nominations."

                },
                ImdbRating = 7.3,
                Country = Country.USA

            });
            movieCollection.Insert(new Movie()
            {
                Title = "The Lord of the Rings: The Return of the King",
                Year = 2003,
                Plot = "Gandalf and Aragorn lead the World of Men against Sauron's army to dra…",               
                Languages = (new List<string> { "English", "Sindarin", "Quenya", "Old English" }),
                Released = new DateTime(2003, 12, 17),
                Cast = new List<string> {"Noel Appleby","Ali Astin","Sean Astin","David Aston" },
                Directors = new List<string> { "Peter Jackson" },
                Writers = new List<string> {"J.R.R. Tolkien (novel)","Fran Walsh (screenplay)","Philippa Boyens (screenplay)","Peter Jackson (screenplay)" },
                Runtime = 201,
                Genres = new List<Genres> { Genres.Adventure,Genres.Fantasy },
                Awards = new Awards()
                {
                    Wins = 175,
                    Nominations = 87,
                    Text = "Won 11 Oscars. Another 164 wins & 87 nominations."

                },
                ImdbRating = 8.9,
                Country = Country.USA

            });
            movieCollection.Insert(new Movie()
            {
                Title = "The Lord of the Rings: The Fellowship of the Ring",
                Year = 2001,
                Plot = "An ancient Ring thought lost for centuries has been found, and through…",
                Languages = (new List<string> { "English", "Sindarin" }),
                Released = new DateTime(2001, 12, 19),
                Cast = new List<string> {"Alan Howard","Noel Appleby","Sean Astin","Sala Baker" },
                Directors = new List<string> { "Peter Jackson" },
                Writers = new List<string> { "J.R.R. Tolkien (novel)", "Fran Walsh (screenplay)", "Philippa Boyens (screenplay)", "Peter Jackson (screenplay)" },
                Runtime = 178,
                Genres = new List<Genres> { Genres.Adventure, Genres.Fantasy },
                Awards = new Awards()
                {
                    Wins = 114,
                    Nominations = 100,
                    Text = "Won 4 Oscars. Another 110 wins & 100 nominations."

                },
                ImdbRating = 8.8,
                Country = Country.USA
            });
        }
        

    }
}
