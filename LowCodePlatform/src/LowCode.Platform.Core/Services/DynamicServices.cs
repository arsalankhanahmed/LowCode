using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using LowCode.Platform.Core.Domain;
using LowCode.Platform.Core.Interfaces;
using Newtonsoft.Json;

namespace LowCode.Platform.Core.Services;

/// <summary>
/// Dynamic SQL Generator for metadata-driven queries
/// </summary>
public class DynamicSqlGenerator : IDynamicSqlGenerator
{
    public string GenerateInsertQuery(string tableName, IEnumerable<string> columns)
    {
        var columnList = columns.ToList();
        var columnNames = string.Join(", ", columnList.Select(c => $"[{c}]"));
        var parameters = string.Join(", ", columnList.Select((c, i) => $"@p{i}"));
        
        return $"INSERT INTO [{tableName}] ({columnNames}) VALUES ({parameters})";
    }

    public string GenerateUpdateQuery(string tableName, IEnumerable<string> columns, string primaryKeyColumn)
    {
        var columnList = columns.Where(c => c != primaryKeyColumn).ToList();
        var setClause = string.Join(", ", columnList.Select((c, i) => $"[{c}] = @p{i}"));
        
        return $"UPDATE [{tableName}] SET {setClause} WHERE [{primaryKeyColumn}] = @p{columnList.Count}";
    }

    public string GenerateDeleteQuery(string tableName, string primaryKeyColumn)
    {
        return $"DELETE FROM [{tableName}] WHERE [{primaryKeyColumn}] = @p0";
    }

    public string GenerateSelectQuery(string tableName, IEnumerable<string>? columns = null, SearchCriteria? criteria = null)
    {
        var selectColumns = columns?.Any() == true 
            ? string.Join(", ", columns.Select(c => $"[{c}]"))
            : "*";
        
        var query = $"SELECT {selectColumns} FROM [{tableName}]";
        
        if (criteria != null && criteria.Conditions.Any())
        {
            var whereClause = BuildWhereClause(criteria.Conditions);
            query += $" WHERE {whereClause}";
        }
        
        if (!string.IsNullOrEmpty(criteria?.OrderBy))
        {
            query += $" ORDER BY [{criteria.OrderBy}] {(criteria.OrderDescending ? "DESC" : "ASC")}";
        }
        
        // Pagination
        if (criteria != null && criteria.PageSize > 0)
        {
            var offset = (criteria.PageNumber - 1) * criteria.PageSize;
            query += $" OFFSET {offset} ROWS FETCH NEXT {criteria.PageSize} ROWS ONLY";
        }
        
        return query;
    }

    private string BuildWhereClause(List<SearchCondition> conditions)
    {
        if (!conditions.Any()) return "1=1";
        
        var whereParts = new List<string>();
        for (int i = 0; i < conditions.Count; i++)
        {
            var cond = conditions[i];
            var operatorSql = ConvertOperator(cond.Operator);
            
            string conditionSql;
            if (cond.Operator == "Between")
            {
                conditionSql = $"([{cond.FieldName}] {operatorSql} @p{i * 2} AND @p{i * 2 + 1})";
            }
            else if (cond.Operator == "In" || cond.Operator == "Not In")
            {
                conditionSql = $"([{cond.FieldName}] {operatorSql} (@p{i}))";
            }
            else
            {
                conditionSql = $"([{cond.FieldName}] {operatorSql} @p{i})";
            }
            
            if (i > 0)
            {
                conditionSql = $"{cond.LogicalOperator} {conditionSql}";
            }
            
            whereParts.Add(conditionSql);
        }
        
        return string.Join(" ", whereParts);
    }

    private string ConvertOperator(string op)
    {
        return op.ToLower() switch
        {
            "equals" => "=",
            "not equals" => "<>",
            "contains" => "LIKE",
            "starts with" => "LIKE",
            "ends with" => "LIKE",
            "greater than" => ">",
            "less than" => "<",
            "greater than or equal" => ">=",
            "less than or equal" => "<=",
            "between" => "BETWEEN",
            "in" => "IN",
            "not in" => "NOT IN",
            "is null" => "IS NULL",
            "is not null" => "IS NOT NULL",
            _ => "="
        };
    }

    public string GenerateCreateTableScript(TableDefinition table)
    {
        var columnsSql = new List<string>();
        var primaryKeys = new List<string>();
        
        foreach (var col in table.Columns)
        {
            var sql = $"    [{col.ColumnName}] {GetSqlDataType(col)}";
            
            if (!col.IsNullable && !col.IsIdentity)
                sql += " NOT NULL";
            
            if (!string.IsNullOrEmpty(col.DefaultValue))
                sql += $" DEFAULT {col.DefaultValue}";
            
            if (col.IsIdentity)
                sql += " IDENTITY(1,1)";
            
            if (col.IsPrimaryKey)
                primaryKeys.Add($"[{col.ColumnName}]");
            
            columnsSql.Add(sql);
        }
        
        if (primaryKeys.Any())
        {
            columnsSql.Add($"    CONSTRAINT [PK_{table.TableName}] PRIMARY KEY CLUSTERED ({string.Join(", ", primaryKeys)})");
        }
        
        var script = $"CREATE TABLE [{table.TableName}] (\n{string.Join(",\n", columnsSql)}\n);";
        
        // Add indexes
        foreach (var index in table.Indexes)
        {
            script += "\n" + GenerateIndexScript(table.TableName, index);
        }
        
        return script;
    }

    private string GetSqlDataType(ColumnDefinition col)
    {
        var dataType = col.DataType.ToLower();
        
        return dataType switch
        {
            "varchar" or "nvarchar" or "char" or "nchar" => $"{dataType.ToUpper()}({(col.Length ?? 50)})",
            "decimal" or "numeric" => $"DECIMAL({(col.Precision ?? 18)}, {(col.Scale ?? 2)})",
            "int" => "INT",
            "bigint" => "BIGINT",
            "smallint" => "SMALLINT",
            "tinyint" => "TINYINT",
            "bit" => "BIT",
            "datetime" or "datetime2" => "DATETIME2",
            "date" => "DATE",
            "time" => "TIME",
            "uniqueidentifier" => "UNIQUEIDENTIFIER",
            "text" or "ntext" => "NVARCHAR(MAX)",
            "image" => "VARBINARY(MAX)",
            "json" => "NVARCHAR(MAX)",
            _ => "NVARCHAR(255)"
        };
    }

    private string GenerateIndexScript(string tableName, IndexDefinition index)
    {
        var unique = index.IsUnique ? "UNIQUE " : "";
        var clustered = index.IsClustered ? "CLUSTERED" : "NONCLUSTERED";
        var columns = string.Join(", ", index.Columns.Select(c => $"[{c}]"));
        
        return $"CREATE {unique}{clustered} INDEX [{index.IndexName}] ON [{tableName}] ({columns});";
    }

    public string GenerateAlterScript(string tableName, ColumnDefinition column, bool isAdd = true)
    {
        if (isAdd)
        {
            return $"ALTER TABLE [{tableName}] ADD [{column.ColumnName}] {GetSqlDataType(column)}{(column.IsNullable ? "" : " NOT NULL")};";
        }
        else
        {
            return $"ALTER TABLE [{tableName}] ALTER COLUMN [{column.ColumnName}] {GetSqlDataType(column)}{(column.IsNullable ? "" : " NOT NULL")};";
        }
    }
}

/// <summary>
/// Business Rule Engine using dynamic expressions
/// </summary>
public class BusinessRuleEngine : IBusinessRuleEngine
{
    private readonly IFormRepository _formRepository;
    
    public BusinessRuleEngine(IFormRepository formRepository)
    {
        _formRepository = formRepository;
    }

    public async Task<RuleEvaluationResult> EvaluateBeforeSaveAsync(Guid formId, IDictionary<string, object?> data)
    {
        var result = new RuleEvaluationResult();
        var rules = await GetRulesForEventAsync(formId, "BeforeSave");
        
        foreach (var rule in rules)
        {
            if (!string.IsNullOrEmpty(rule.ConditionExpression))
            {
                try
                {
                    var conditionMet = EvaluateCondition(rule.ConditionExpression, data);
                    
                    if (conditionMet)
                    {
                        if (!string.IsNullOrEmpty(rule.ActionExpression))
                        {
                            await ApplyActionAsync(rule.ActionExpression, data);
                            result.ExecutedActions.Add(rule.RuleName ?? "Unnamed Rule");
                        }
                        
                        if (!string.IsNullOrEmpty(rule.ErrorMessage))
                        {
                            result.IsValid = false;
                            result.ErrorMessages.Add(rule.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorMessages.Add($"Rule evaluation error: {ex.Message}");
                }
            }
        }
        
        return result;
    }

    public async Task<RuleEvaluationResult> EvaluateAfterSaveAsync(Guid formId, IDictionary<string, object?> data, Guid recordId)
    {
        var result = new RuleEvaluationResult();
        var rules = await GetRulesForEventAsync(formId, "AfterSave");
        
        // After save rules typically trigger notifications, workflows, etc.
        foreach (var rule in rules)
        {
            if (!string.IsNullOrEmpty(rule.ConditionExpression))
            {
                try
                {
                    var conditionMet = EvaluateCondition(rule.ConditionExpression, data);
                    
                    if (conditionMet && !string.IsNullOrEmpty(rule.ActionExpression))
                    {
                        await ApplyActionAsync(rule.ActionExpression, data);
                        result.ExecutedActions.Add(rule.RuleName ?? "Unnamed Rule");
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorMessages.Add($"After-save rule error: {ex.Message}");
                }
            }
        }
        
        return result;
    }

    public async Task<RuleEvaluationResult> EvaluateOnLoadAsync(Guid formId, Guid? recordId = null)
    {
        var result = new RuleEvaluationResult();
        var rules = await GetRulesForEventAsync(formId, "OnLoad");
        
        // OnLoad rules can set default values or modify visibility
        foreach (var rule in rules)
        {
            if (!string.IsNullOrEmpty(rule.ActionExpression))
            {
                try
                {
                    await ApplyActionAsync(rule.ActionExpression, result.ModifiedValues);
                    result.ExecutedActions.Add(rule.RuleName ?? "Unnamed Rule");
                }
                catch (Exception ex)
                {
                    result.ErrorMessages.Add($"On-load rule error: {ex.Message}");
                }
            }
        }
        
        return result;
    }

    public Task ApplyActionAsync(string actionExpression, IDictionary<string, object?> data)
    {
        // Parse and execute action expressions like "SetField('Status', 'Approved')"
        // This is a simplified implementation
        if (actionExpression.StartsWith("SetField("))
        {
            var parts = actionExpression.Substring(9, actionExpression.Length - 10).Split(',');
            if (parts.Length == 2)
            {
                var fieldName = parts[0].Trim().Trim('\'');
                var value = parts[1].Trim().Trim('\'');
                data[fieldName] = value;
            }
        }
        
        return Task.CompletedTask;
    }

    private async Task<IEnumerable<BusinessRule>> GetRulesForEventAsync(Guid formId, string eventType)
    {
        var form = await _formRepository.GetFormByIdAsync(formId);
        return form?.BusinessRules
            .Where(r => r.EventType == eventType && r.IsActive)
            .OrderBy(r => r.OrderIndex) ?? Enumerable.Empty<BusinessRule>();
    }

    private bool EvaluateCondition(string expression, IDictionary<string, object?> data)
    {
        // Simplified expression evaluation
        // In production, use a proper expression parser like NCalc or DynamicExpresso
        
        try
        {
            // Replace field names with values
            var evalExpression = expression;
            foreach (var kvp in data)
            {
                evalExpression = evalExpression.Replace(kvp.Key, kvp.Value?.ToString() ?? "null");
            }
            
            // Very basic evaluation - production should use proper expression engine
            if (evalExpression.Contains("==") || evalExpression.Contains("="))
            {
                return true; // Placeholder
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}
