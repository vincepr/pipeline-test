using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using PipelineTestProject.Database;

namespace PipelineTestProject;

public static class ExamplesPgVector
{
    public static async Task Run()
    {
        
        var conString = "Server=localhost;Database=mydb;Port=5432;User Id=myuser;Password=mypassword;";
        IServiceCollection services = new ServiceCollection();
        services.AddDbContextFactory<MyContext>(o =>
        {
            o.UseNpgsql(conString, builder =>
            {
                builder.UseVector();
            });
        });
        var provider = services.BuildServiceProvider();
        
        await using var ctx = provider.GetRequiredService<MyContext>();
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.EnsureCreatedAsync();
        
        // insert an embedding
        var categories = ctx.Categories.ToList();
        Console.WriteLine(categories.Count);
        categories.First(c => c.Id == 1).Embedding = new Vector(new ReadOnlyMemory<float>([1,2,3]));
        categories.First(c => c.Id == 2).Embedding = new Vector(new ReadOnlyMemory<float>([2,2,3]));
        categories.First(c => c.Id == 3).Embedding = new Vector(new ReadOnlyMemory<float>([3,3,3]));
        categories.First(c => c.Id == 4).Embedding = new Vector(new ReadOnlyMemory<float>([99,99,99]));
        await ctx.SaveChangesAsync();


        // read an embedding
        var readCtx = provider.GetRequiredService<MyContext>();
        var x = readCtx.Categories.First(c => c.Id == 1);
        Console.WriteLine(x.Id);
        Console.WriteLine(x.Embedding);

        // nearest neighbor embeddings
        var embedding = new Vector(new ReadOnlyMemory<float>([30,30,30]));
        var neighbors = await readCtx.Categories
            .Where(c => c.Embedding != null)
            .OrderBy(x => x.Embedding!.L2Distance(embedding))
            .Take(2)
            .ToListAsync();
        
        foreach (var tuple in neighbors.Select((e,i) => (e, i)))
        {
            Console.WriteLine($"{tuple.i}th nearest neighbor was: {tuple.e.Name} with: {tuple.e.Embedding!}");
        }
        
        
        // // get the distance
        // var embedding2 = new Vector(new ReadOnlyMemory<float>([0,0,0]));
        // var neighborsWithDistance = await readCtx.Categories
        //     .Select(c => new
        //     {
        //         Category = c,
        //         L2Distance = x.Embedding!.L2Distance(embedding2)
        //     })
        //     .ToListAsync();
        //
        // foreach (var c in neighborsWithDistance)
        // {
        //     Console.WriteLine($"{c.Category.Name}  with L1distance: {c.L1Distance} with L2distance: {c.L2Distance}");
        // }
        
    }
    
}