using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PipelineTestProject.Database;

public class MyContext(DbContextOptions<MyContext> options) : DbContext(options)
{
    public DbSet<Article> Articles { get; set; }
    public DbSet<Category> Categories { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Category>().HasData([
            new ()
            {
                Id = 1,
                Name = "table"
            },
            new ()
            {
                Id = 2,
                Name = "Shirts"
            },
            new ()
            {
                Id = 3,
                Name = "Clothing > Outdoor"
            },
            new ()
            {
                Id = 4,
                Name = "shirt"
            },
        ]);
        modelBuilder.Entity<Article>().HasData([
            new ()
            {
                Id = 1,
                Ean = "1111",
                Name = "Bamboo table",
                Description = "Elegant modern bamboo table. Perfect for gatherings in a quiet round.",
            },
            new ()
            {
                Id = 2,
                Ean = "2222",
                Name = "Green sweatshirt M",
                Description = "Sweatshirt in the color navy green. Beatable and perfect for outdoors activity.",
                CategoryId = 2
            },
            new ()
            {
                Id = 3,
                Ean = "3333",
                Name = "Blue sweatshirt L",
            },
        ]);
    }
}

public record Article
{
    [Key] public int Id { get; init; }
    public required string Ean { get; init; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    [ForeignKey(nameof(Category))] public int? CategoryId { get; set; } 
    public Category? Category { get; init; } 
}

public record Category
{
    [Key] public int Id { get; init; }
    public required string Name { get; init; }
}
