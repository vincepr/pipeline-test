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
    static void Main(string[] args)
    {
        
        //Func<string, List<PipelineResult>> result = null;
        
        IServiceCollection services = new ServiceCollection();
        var dbFactory = FileBasedContextFactory<MyContext>.New(o => new MyContext(o));
        using var ctx = dbFactory.CreateDbContext();
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
            results.Add(new {Article= a.Ean, Result= finalPipeline.Run(a.Ean) });
        }
        Console.WriteLine(JsonSerializer.Serialize(results, JsonSerializerOptions.Web));
        
        // make a pipeline Step that runs multiple steps or combines them or whatever
        var finalPipelineTaketwo = app.GetService<FinalPipelineStep>();
        results = [];
        foreach (var a in articles)
        {
            results.Add(new {Article= a.Ean, Result= finalPipelineTaketwo.Run(a.Ean) });
        }
        Console.WriteLine(JsonSerializer.Serialize(results, JsonSerializerOptions.Web));
    }
}

/*
 * ** Interfaces and Types **
 */
public class PipelineResult : List<PipelineResultItem>
{
}

public interface IPipelineStep
{
    public PipelineResult Run(string ean, PipelineResult? previousResult = null);
}

/*
 * ** Example Steps **
 */

public class TryFromCategoryNamePipelineStep : IPipelineStep
{
    private readonly MyContext _context;
    public TryFromCategoryNamePipelineStep(MyContext context)
    {
        _context = context;
    }
    public PipelineResult Run(string ean, PipelineResult? previousResult = null)
    {
        var article = _context.Articles.Single(a => a.Ean == ean);
        if (article?.Description is null || article.Description.Length == 0) return [];
        var categories = _context.Categories
                .Where(c => article.Description.ToLower().Contains(c.Name.ToLower()))
                .ToList();
        if (categories.Any() is false) return [];
        var result = new PipelineResult();
        var x =  categories.Select(c => new PipelineResultItem
        {
            CategoryId = c.Id,
            Certainty = 1.0 / categories.Count,
        });
        result.AddRange(x);
        return result;
    }
}

public class FinalPipelineStep : IPipelineStep
{
    private readonly IEnumerable<IPipelineStep> _allSteps;

    public FinalPipelineStep(IEnumerable<IPipelineStep> allSteps)
    {
        _allSteps = allSteps;
    }

    public PipelineResult Run(string ean, PipelineResult? previousResult = null)
    {
        if (HighEnoughMatch(previousResult))
        {
            Console.WriteLine($"Previous PipelineStep reached final result with high enough certainty");
            return previousResult!;
        }
        Console.WriteLine($"Found {_allSteps.Count()} pipeline steps");
        foreach (var s in _allSteps)
        {
            Console.WriteLine($"Running PipelineStep {nameof(s)}");
            previousResult = s.Run(ean, previousResult);
            if (previousResult?.Any(r => r.Certainty > 0.8) is true)
            {
                Console.WriteLine($"{nameof(s)} PipelineStep reached final result with high enough certainty");
                return previousResult;
            }
        }

        return [];
    }

    private static bool HighEnoughMatch(PipelineResult? previousResult)
    {
        return previousResult?.Any(r => r.Certainty > 0.8) is true;
    }
}

public class FinalPipelineStep_ThatKeepsOld : IPipelineStep
{
    private readonly IEnumerable<IPipelineStep> _allSteps;

    public FinalPipelineStep_ThatKeepsOld(IEnumerable<IPipelineStep> allSteps)
    {
        _allSteps = allSteps;
    }

    public PipelineResult Run(string ean, PipelineResult? previousResult = null)
    {
        if (previousResult is null) previousResult = new PipelineResult();
        foreach (var s in _allSteps)
        {
            var newResults = s.Run(ean, previousResult);
            previousResult.AddRange(newResults);
        }

        return previousResult;
    }

    private static bool HighEnoughMatch(PipelineResult? previousResult)
    {
        return previousResult?.Any(r => r.Certainty > 0.8) is true;
    }
}

public record PipelineResultItem
{
    public required int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public required double Certainty { get; set; }
}

public class AlwaysCorrectPercentPipelineStep : IPipelineStep
{
    public PipelineResult Run(string ean, PipelineResult? previousResult = null)
    {
        return ean switch
        {
            "1111" =>
            [
                new()
                {
                    CategoryId = 1,
                    Certainty = 1.0,
                },
            ],
            "2222" =>
            [
                new()
                {
                    CategoryId = 2,
                    Certainty = 1.0,
                },
            ],
            "3333" => [new ()
                {
                    CategoryId = 2,
                    Certainty = 1.0,
                },],
        };
    }
}

public class EmptyPipelineStep : IPipelineStep
{
    public PipelineResult Run(string ean, PipelineResult? previousResult = null)
    {
        return [];
    }
}

public class RandomPipelineStep : IPipelineStep
{
    public PipelineResult Run(string ean, PipelineResult? previousResult = null)
        => new Random().Next(1, 3) switch
        {
            1 => [new () { CategoryId = 1, Certainty = 0.5 }],
            2 => [new() { CategoryId = 2, Certainty = 0.3 }],
            3 => [new() { CategoryId = 3, Certainty = 0.9 }],
            _ => throw new ArgumentOutOfRangeException(nameof(ean), ean, null)
        };
}
