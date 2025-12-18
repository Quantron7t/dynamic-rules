using System.Linq.Expressions;
using System.Reflection;

public static class RuleEngine<T>
{
    public static Expression<Func<T, bool>> Build(RuleSet ruleSet)
    {
        if (ruleSet == null)
            throw new ArgumentNullException(nameof(ruleSet), "RuleSet cannot be null");

        if (ruleSet.Conditions == null || ruleSet.Conditions.Count == 0)
            throw new ArgumentException("RuleSet must contain at least one condition", nameof(ruleSet));

        ParameterExpression param = Expression.Parameter(typeof(T), "x");
        Expression? finalExpr = null;

        foreach (var cond in ruleSet.Conditions)
        {
            if (string.IsNullOrWhiteSpace(cond.Field))
                throw new ArgumentException("Condition Field cannot be null or empty");

            if (cond.Value == null)
                throw new ArgumentException($"Condition Value cannot be null for field '{cond.Field}'");

            // Get nested property (Bio.Age)
            Expression member = GetNestedProperty(param, cond.Field);

            var constant = Expression.Constant(Convert.ChangeType(cond.Value, member.Type));

            Expression comparison = cond.Operator switch
            {
                RuleOperator.Equal => Expression.Equal(member, constant),
                RuleOperator.NotEqual => Expression.NotEqual(member, constant),
                RuleOperator.GreaterThan => Expression.GreaterThan(member, constant),
                RuleOperator.LessThan => Expression.LessThan(member, constant),
                RuleOperator.Contains => Expression.Call(
                    member,
                    typeof(string).GetMethod("Contains", new[] { typeof(string) }) 
                        ?? throw new InvalidOperationException("Contains method not found"),
                    constant),
                _ => throw new NotSupportedException()
            };

            finalExpr = finalExpr == null
                ? comparison
                : ruleSet.Connector == RuleConnector.And
                    ? Expression.AndAlso(finalExpr, comparison)
                    : Expression.OrElse(finalExpr, comparison);
        }

        if (finalExpr == null)
            throw new InvalidOperationException("No conditions provided");

        return Expression.Lambda<Func<T, bool>>(finalExpr, param);
    }

    // ✅ NEW PART — handles "Bio.Age" or deeper: "Account.Address.Zip"
    private static Expression GetNestedProperty(Expression param, string propertyPath)
    {
        Expression result = param;
        foreach (var part in propertyPath.Split('.'))
        {
            result = Expression.Property(result, part);
        }
        return result;
    }
}


public enum RuleOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    LessThan,
    Contains
}

public enum RuleConnector
{
    And,
    Or
}

public class RuleCondition
{
    public required string Field { get; set; }
    public RuleOperator Operator { get; set; }
    public required object Value { get; set; }
}

public class RuleSet
{
    public RuleConnector Connector { get; set; }
    public required List<RuleCondition> Conditions { get; set; }
}
