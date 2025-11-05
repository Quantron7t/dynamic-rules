using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public class Program
{
    public static void Main()
    {
        var customers = new List<Customer>
        {
            new Customer { Name="John", Bio={ Age=30 }, City="Mumbai" },
            new Customer { Name="Sara", Bio={ Age=22 }, City="Delhi" },
            new Customer { Name="Aman", Bio={ Age=29 }, City="Mumbai" }
        };

        // Simulate fetching JSON from DB
        var json = "{  \"connector\": \"And\",  \"conditions\": [    { \"field\": \"Bio.Age\", \"operator\": \"GreaterThan\", \"value\": 20 },    { \"field\": \"City\", \"operator\": \"Contains\", \"value\": \"Mumbai\" },    { \"field\": \"Name\", \"operator\": \"Equal\", \"value\": \"John\" }   ]}";
        var ruleSet = Newtonsoft.Json.JsonConvert.DeserializeObject<RuleSet>(json);

        // Build expression and apply
        var predicate = RuleEngine<Customer>.Build(ruleSet);
        var result = customers.AsQueryable().Where(predicate).ToList();

        foreach (var item in result)
            Console.WriteLine(item.Name);
    }
}

public class Customer
{
    public string Name { get; set; } = string.Empty;
    public Bio Bio { get; set; } = new();
    public string City { get; set; } = string.Empty;
}

public class Bio
{     public int Age { get; set; }
}
