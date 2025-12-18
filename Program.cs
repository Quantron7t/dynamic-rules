using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RulesEngine.Models;

public class Program
{
    public static void Main()
    {
        var customers = new List<Customer>
        {
            new Customer { Name="John", Bio={ Age=30 }, City="Mumbai", FinancialScores = new List<int> { 700, 750 } },
            new Customer { Name="Sara", Bio={ Age=22 }, City="Delhi", FinancialScores = new List<int> { 600 } },
            new Customer { Name="Aman", Bio={ Age=29 }, City="Mumbai", FinancialScores = new List<int> { 800, 850 } }
        };

        // Simulate fetching JSON from DB
        var json = "{  \"connector\": \"And\",  \"conditions\": [    { \"field\": \"Bio.Age\", \"operator\": \"GreaterThan\", \"value\": 20 },    { \"field\": \"City\", \"operator\": \"Contains\", \"value\": \"Mumbai\" },    { \"field\": \"Name\", \"operator\": \"Equal\", \"value\": \"John\" }   ]}";
        var ruleSet = Newtonsoft.Json.JsonConvert.DeserializeObject<RuleSet>(json);

        if (ruleSet == null)
            throw new InvalidOperationException("Failed to deserialize rule set");

        // Build expression and apply
        var predicate = RuleEngine<Customer>.Build(ruleSet);
        var result = customers.AsQueryable().Where(predicate).ToList();

        foreach (var item in result)
            Console.WriteLine(item.Name);


        //POC for using microsofts RulesEngine
        Console.WriteLine("\n--- Microsoft RulesEngine POC (from JSON) ---");
        
        // Define rules in JSON format (simulating fetching from DB)
        var rulesJson = @"[
            {
                ""WorkflowName"": ""CustomerFilterWorkflow"",
                ""Rules"": [
                    {
                        ""RuleName"": ""AgeGreaterThan20"",
                        ""Expression"": ""Bio.Age > 20"",
                        ""RuleExpressionType"": ""LambdaExpression""
                    },
                    {
                        ""RuleName"": ""CityContainsMumbai"",
                        ""Expression"": ""City.Contains(\""Mumbai\"")"",
                        ""RuleExpressionType"": ""LambdaExpression""
                    },
                    {
                        ""RuleName"": ""NameEqualsJohn"",
                        ""Expression"": ""Name == \""John\"""",
                        ""RuleExpressionType"": ""LambdaExpression""
                    }
                ]
            }
        ]";

        // Deserialize JSON to Workflow objects
        var workflows = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Workflow>>(rulesJson);
        
        if (workflows == null)
            throw new InvalidOperationException("Failed to deserialize workflows");

        // Initialize RulesEngine with JSON-loaded workflows
        var rulesEngine = new RulesEngine.RulesEngine(workflows.ToArray());

        // Evaluate rules for each customer
        Console.WriteLine("Customers matching all rules (Age > 20 AND City contains Mumbai AND Name equals John):");
        foreach (var customer in customers)
        {
            var ruleResults = rulesEngine.ExecuteAllRulesAsync("CustomerFilterWorkflow", customer).Result;

            // Check if all rules passed
            bool allRulesPassed = ruleResults.All(r => r.IsSuccess);

            if (allRulesPassed)
            {
                Console.WriteLine($"✓ {customer.Name} - Age: {customer.Bio.Age}, City: {customer.City}");
            }
            else
            {
                Console.WriteLine($"✗ {customer.Name} - Failed rules: {string.Join(", ", ruleResults.Where(r => !r.IsSuccess).Select(r => r.Rule.RuleName))}");
            }
        }
        
        //Show final output by using workflow rule applied to a list of customers
        Console.WriteLine("\n--- Filtered Customer List (Microsoft RulesEngine) ---");
        
        var filteredCustomers = new List<Customer>();
        foreach (var customer in customers)
        {
            var ruleResults = rulesEngine.ExecuteAllRulesAsync("CustomerFilterWorkflow", customer).Result;
            
            // If all rules passed, add to filtered list
            if (ruleResults.All(r => r.IsSuccess))
            {
                filteredCustomers.Add(customer);
            }
        }
        
        Console.WriteLine($"Total customers matching all rules: {filteredCustomers.Count}");
        foreach (var customer in filteredCustomers)
        {
            Console.WriteLine($"  - {customer.Name}");
        }

        //POC for using microsofts RulesEngine with Workflow objects (no JSON)
        Console.WriteLine("\n--- Microsoft RulesEngine (Direct Workflow Objects) ---");
        
        // Create workflow using direct object initialization (no JSON deserialization)
        var workflowsFromObjects = new List<Workflow>
        {
            new Workflow
            {
                WorkflowName = "CustomerFilterWorkflow2",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "AgeGreaterThan20",
                        Expression = "(Bio.Age > 20 & City == \"Mumbai\") | Bio.Age < 30",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    },
                    new Rule
                    {
                        RuleName = "CityContainsMumbai",
                        Expression = "City.Contains(\"Mumbai\")",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            }
        };

        // Initialize RulesEngine with direct Workflow objects
        var rulesEngine2 = new RulesEngine.RulesEngine(workflowsFromObjects.ToArray());

        // Apply rules to filter customer list
        var filteredCustomers2 = new List<Customer>();
        foreach (var customer in customers)
        {
            var ruleResults = rulesEngine2.ExecuteAllRulesAsync("CustomerFilterWorkflow2", customer).Result;
            
            if (ruleResults.All(r => r.IsSuccess))
            {
                filteredCustomers2.Add(customer);
            }
        }

        Console.WriteLine($"Total customers matching all rules: {filteredCustomers2.Count}");
        foreach (var customer in filteredCustomers2)
        {
            Console.WriteLine($"  - {customer.Name}");
        }

    }
}

public class Customer
{
    public string Name { get; set; } = string.Empty;
    public Bio Bio { get; set; } = new();
    public string City { get; set; } = string.Empty;
    public List<int> FinancialScores { get; set; } = new();
}

public class Bio
{     
    public int Age { get; set; }
}
