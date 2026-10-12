namespace BatchRenamerAT.Enums;

// Comparison a trigger condition performs. The order matches the operator ComboBox items.
public enum ConditionOperator
{
    Contains,
    StartsWith,
    EndsWith,
    Equals,
    MatchesRegularExpression
}
