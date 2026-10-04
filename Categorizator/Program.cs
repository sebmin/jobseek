using Categorizator.Models.Source;
using Categorizator.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Categorizator;

internal static class Program
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static int Main(string[] args)
    {
        if (!CategorizatorOptions.TryParse(args, out var options, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine();
            Console.Error.WriteLine(CategorizatorOptions.Usage);
            return 1;
        }

        if (options.Help)
        {
            Console.WriteLine(CategorizatorOptions.Usage);
            return 0;
        }

        if (!File.Exists(options.OffersFile))
        {
            Console.Error.WriteLine($"Offers file does not exist: {options.OffersFile}");
            return 1;
        }

        var json = File.ReadAllText(options.OffersFile);

        var sourceOffers = JsonSerializer.Deserialize<List<SourceOffer>>(json, ReadOptions) ?? [];
        var categoryService = CategoryOfferService.CreateService();
        var offers = categoryService.CreateCategoryOfferDtos(sourceOffers);


        foreach (var offer in offers.Take(10))
        {
            Console.WriteLine($"Id: {offer.Id}");
            Console.WriteLine($"Slug: {offer.Slug}");
            Console.WriteLine($"RequiredSkills: {string.Join(", ", offer.RequiredSkills)}");
            Console.WriteLine($"NiceToHaveSkills: {string.Join(", ", offer.NiceToHaveSkills)}");
            Console.WriteLine();
        }

        return 0;
    }
}
