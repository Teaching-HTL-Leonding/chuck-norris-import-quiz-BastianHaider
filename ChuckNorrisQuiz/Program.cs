using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

var factory = new CookbookContextFactory();
using var dbContext = factory.CreateDbContext(args);
HttpClient client = new();

if ("clear".Equals(args[0]))
{
    await dbContext.Database.ExecuteSqlRawAsync("DELETE from Facts");
    Console.WriteLine("DB was cleared");
}
else
{
    var numOfJokes = Convert.ToInt32(args[0]) >= 1 && Convert.ToInt32(args[0]) <= 10 ? Convert.ToInt32(args[0]) : 5;
    for (int i = 0; i < numOfJokes; i++)
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync("https://api.chucknorris.io/jokes/random");
            response.EnsureSuccessStatusCode();
            string jsonString = await response.Content.ReadAsStringAsync();
            var jsonObject = JsonSerializer.Deserialize<Fact>(jsonString);
            var newJoke = new Fact { ChuckNorrisId = jsonObject.Id.ToString(), Url = jsonObject.Url, Joke = jsonObject.Joke};

            using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                dbContext.Jokes.Add(newJoke);
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);
        }
    }
    Console.WriteLine($"Added {numOfJokes}/{numOfJokes} Jokes to the DB");
}
public class Fact
{
    public int Id { get; set; }
    [JsonPropertyName("id")]
    [MaxLength(40)]
    public string ChuckNorrisId { get; set; }
    [JsonPropertyName("url")]
    [MaxLength(1024)]
    public string Url { get; set; }
    [JsonPropertyName("value")]
    public string Joke { get; set; }
}
class CookbookContext : DbContext
{
    public DbSet<Fact> Jokes { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public CookbookContext(DbContextOptions<CookbookContext> options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        : base(options)
    { }

}

class CookbookContextFactory : IDesignTimeDbContextFactory<CookbookContext>
{
    public CookbookContext CreateDbContext(string[]? args = null)
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        var optionsBuilder = new DbContextOptionsBuilder<CookbookContext>();
        optionsBuilder
            // Uncomment the following line if you want to print generated
            // SQL statements on the console.
            //.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
            .UseSqlServer(configuration["ConnectionStrings:DefaultConnection"]);

        return new CookbookContext(optionsBuilder.Options);
    }

}
