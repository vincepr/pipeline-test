using System.Collections.Specialized;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework.Internal;
using PipelineTestProject.Database;
using TestingFixtures;

namespace PipelineTestProject;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting");
        await ExamplesPgVector.Run();
        Console.WriteLine("Finished");
    }


    public async Task ExamplePipelineSteps()
    {
        IServiceCollection services = new ServiceCollection();
        var dbFactory = await PostgresDockerContextFactory<MyContext>.NewAsync(o => new MyContext(o));
        await using var ctx = await dbFactory.CreateDbContextAsync();
        Console.WriteLine($"Seeded-File-BasedDB: Articles {ctx.Articles.Count()}, Categories {ctx.Categories.Count()}");
        var articles = ctx.Articles.Include(a => a.Category).ToList();
        Console.WriteLine($"Articles with Category{articles.Where(a => a.Category is not null).Count()}");
        services.AddTransient<MyContext>(_ => dbFactory.CreateDbContext());
        services.AddTransient<IPipelineStep, TryFromCategoryNamePipelineStep>();
        services.AddTransient<FinalPipelineStep>();
        services.AddTransient<FinalPipelineStep_ThatKeepsOld>();
        
        // manually run all steps:
        var app = services.BuildServiceProvider();
        var step = app.GetService<IPipelineStep>();
        foreach (var a in articles)
        {
            var result = step.Run(a.Ean);
            Console.WriteLine(string.Join(", ", result));
        }
        
        // make a pipeline Step that runs multiple steps or combines them or whatever
        var finalPipeline = app.GetService<FinalPipelineStep>();
        List<object> results = [];
        foreach (var a in articles)
        {
            results.Add(new {Article= a.Ean, Result = finalPipeline.Run(a.Ean) });
        }
        Console.WriteLine(JsonSerializer.Serialize(results, JsonSerializerOptions.Web));
        
        // make a pipeline Step that runs multiple steps or combines them or whatever
        var finalPipelineTaketwo = app.GetService<FinalPipelineStep>();
        results = [];
        foreach (var a in articles)
        {
            results.Add(new {Article= a.Ean, Result = finalPipelineTaketwo.Run(a.Ean) });
        }
        Console.WriteLine(JsonSerializer.Serialize(results, JsonSerializerOptions.Web));
    }
}

