using System.Data;
using Dapper;
using LowCode.Platform.Core.Domain;
using LowCode.Platform.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace LowCode.Platform.Infrastructure.Repositories;

/// <summary>
/// SQL Server implementation of form repository
/// </summary>
public class FormRepository : IFormRepository
{
    private readonly string _connectionString;

    public FormRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<FormDefinition?> GetFormByIdAsync(Guid formId)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var formSql = "SELECT * FROM Forms WHERE FormId = @FormId";
        var form = await connection.QueryFirstOrDefaultAsync<FormDefinition>(formSql, new { FormId = formId });
        
        if (form != null)
        {
            form.Controls = (await GetControlsForFormAsync(formId)).ToList();
            form.BusinessRules = await GetBusinessRulesAsync(formId);
        }
        
        return form;
    }

    public async Task<FormDefinition?> GetFormByNameAsync(string formName)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = "SELECT * FROM Forms WHERE FormName = @FormName";
        var form = await connection.QueryFirstOrDefaultAsync<FormDefinition>(sql, new { FormName = formName });
        
        if (form != null)
        {
            form.Controls = (await GetControlsForFormAsync(form.FormId)).ToList();
        }
        
        return form;
    }

    public async Task<IEnumerable<FormDefinition>> GetAllFormsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Forms ORDER BY FormName";
        return await connection.QueryAsync<FormDefinition>(sql);
    }

    public async Task<IEnumerable<FormDefinition>> GetFormsByModuleAsync(Guid moduleId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Forms WHERE ModuleId = @ModuleId ORDER BY FormName";
        return await connection.QueryAsync<FormDefinition>(sql, new { ModuleId = moduleId });
    }

    public async Task<Guid> CreateFormAsync(FormDefinition form)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            INSERT INTO Forms (FormId, FormName, TableName, ModuleId, IsMasterDetail, ParentFormId, 
                              LayoutConfig, WorkflowId, PrimaryKeyColumn, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy)
            VALUES (@FormId, @FormName, @TableName, @ModuleId, @IsMasterDetail, @ParentFormId,
                    @LayoutConfig, @WorkflowId, @PrimaryKeyColumn, GETDATE(), GETDATE(), @CreatedBy, @ModifiedBy)";
        
        form.FormId = Guid.NewGuid();
        form.CreatedDate = DateTime.Now;
        form.ModifiedDate = DateTime.Now;
        
        await connection.ExecuteAsync(sql, form);
        return form.FormId;
    }

    public async Task UpdateFormAsync(FormDefinition form)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            UPDATE Forms SET FormName = @FormName, TableName = @TableName, ModuleId = @ModuleId,
                            IsMasterDetail = @IsMasterDetail, ParentFormId = @ParentFormId,
                            LayoutConfig = @LayoutConfig, WorkflowId = @WorkflowId,
                            ModifiedDate = GETDATE(), ModifiedBy = @ModifiedBy
            WHERE FormId = @FormId";
        
        form.ModifiedDate = DateTime.Now;
        await connection.ExecuteAsync(sql, form);
    }

    public async Task DeleteFormAsync(Guid formId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "DELETE FROM Forms WHERE FormId = @FormId";
        await connection.ExecuteAsync(sql, new { FormId = formId });
    }

    public async Task<IEnumerable<FormControlDefinition>> GetControlsForFormAsync(Guid formId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM FormControls WHERE FormId = @FormId ORDER BY OrderIndex";
        return await connection.QueryAsync<FormControlDefinition>(sql, new { FormId = formId });
    }

    public async Task AddControlAsync(Guid formId, FormControlDefinition control)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            INSERT INTO FormControls (ControlId, FormId, ControlType, FieldName, Label, OrderIndex,
                                     RowIndex, ColIndex, Width, Height, IsRequired, IsReadOnly, IsVisible,
                                     DefaultValue, ValidationRule, DataSourceQuery, DisplayMember, ValueMember,
                                     PropertiesJson, TabIndex)
            VALUES (@ControlId, @FormId, @ControlType, @FieldName, @Label, @OrderIndex,
                    @RowIndex, @ColIndex, @Width, @Height, @IsRequired, @IsReadOnly, @IsVisible,
                    @DefaultValue, @ValidationRule, @DataSourceQuery, @DisplayMember, @ValueMember,
                    @PropertiesJson, @TabIndex)";
        
        control.ControlId = Guid.NewGuid();
        control.FormId = formId;
        
        await connection.ExecuteAsync(sql, control);
    }

    public async Task UpdateControlAsync(FormControlDefinition control)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            UPDATE FormControls SET ControlType = @ControlType, FieldName = @FieldName, Label = @Label,
                                   OrderIndex = @OrderIndex, RowIndex = @RowIndex, ColIndex = @ColIndex,
                                   Width = @Width, Height = @Height, IsRequired = @IsRequired,
                                   IsReadOnly = @IsReadOnly, IsVisible = @IsVisible, DefaultValue = @DefaultValue,
                                   ValidationRule = @ValidationRule, DataSourceQuery = @DataSourceQuery,
                                   DisplayMember = @DisplayMember, ValueMember = @ValueMember,
                                   PropertiesJson = @PropertiesJson, TabIndex = @TabIndex
            WHERE ControlId = @ControlId";
        
        await connection.ExecuteAsync(sql, control);
    }

    public async Task DeleteControlAsync(Guid controlId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "DELETE FROM FormControls WHERE ControlId = @ControlId";
        await connection.ExecuteAsync(sql, new { ControlId = controlId });
    }

    private async Task<IEnumerable<BusinessRule>> GetBusinessRulesAsync(Guid formId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM BusinessRules WHERE FormId = @FormId AND IsActive = 1 ORDER BY OrderIndex";
        return await connection.QueryAsync<BusinessRule>(sql, new { FormId = formId });
    }
}

/// <summary>
/// SQL Server implementation of dynamic data repository
/// Uses Dapper for dynamic query execution
/// </summary>
public class DynamicDataRepository : IDynamicDataRepository
{
    private readonly string _connectionString;
    private readonly IDynamicSqlGenerator _sqlGenerator;

    public DynamicDataRepository(IConfiguration configuration, IDynamicSqlGenerator sqlGenerator)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _sqlGenerator = sqlGenerator;
    }

    public async Task<IEnumerable<dynamic>> GetAllAsync(string tableName)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = $"SELECT * FROM [{tableName}]";
        return await connection.QueryAsync(sql);
    }

    public async Task<dynamic?> GetByIdAsync(string tableName, Guid id)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = $"SELECT * FROM [{tableName}] WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync(sql, new { Id = id });
    }

    public async Task<Guid> InsertAsync(string tableName, IDictionary<string, object?> data)
    {
        using var connection = new SqlConnection(_connectionString);
        
        // Ensure Id field exists
        if (!data.ContainsKey("Id"))
        {
            data["Id"] = Guid.NewGuid();
        }
        
        var columns = data.Keys.ToList();
        var sql = _sqlGenerator.GenerateInsertQuery(tableName, columns);
        
        var parameters = columns.Select((c, i) => new SqlParameter($"@p{i}", data[c] ?? DBNull.Value)).ToArray();
        
        await connection.ExecuteAsync(sql, parameters);
        
        return (Guid)data["Id"]!;
    }

    public async Task UpdateAsync(string tableName, Guid id, IDictionary<string, object?> data)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var columns = data.Keys.ToList();
        var primaryKeyColumn = "Id"; // Configurable per form
        var sql = _sqlGenerator.GenerateUpdateQuery(tableName, columns, primaryKeyColumn);
        
        var parameters = columns.Select((c, i) => new SqlParameter($"@p{i}", data[c] ?? DBNull.Value)).ToList();
        parameters.Add(new SqlParameter($"@p{columns.Count}", id));
        
        await connection.ExecuteAsync(sql, parameters.ToArray());
    }

    public async Task DeleteAsync(string tableName, Guid id)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = $"DELETE FROM [{tableName}] WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<IEnumerable<dynamic>> SearchAsync(string tableName, SearchCriteria criteria)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = _sqlGenerator.GenerateSelectQuery(tableName, null, criteria);
        
        // Build parameters from conditions
        var parameters = new List<SqlParameter>();
        int paramIndex = 0;
        
        foreach (var condition in criteria.Conditions)
        {
            if (condition.Operator == "Between")
            {
                parameters.Add(new SqlParameter($"@p{paramIndex++}", condition.Value ?? DBNull.Value));
                parameters.Add(new SqlParameter($"@p{paramIndex++}", condition.Value2 ?? DBNull.Value));
            }
            else if (condition.Operator == "In" || condition.Operator == "Not In")
            {
                // Handle IN clause - convert to comma-separated for SQL
                parameters.Add(new SqlParameter($"@p{paramIndex++}", condition.Value?.ToString() ?? ""));
            }
            else
            {
                // Handle LIKE operators
                if (condition.Operator.ToLower().Contains("contains"))
                {
                    condition.Value = $"%{condition.Value}%";
                }
                else if (condition.Operator.ToLower().Contains("starts with"))
                {
                    condition.Value = $"{condition.Value}%";
                }
                else if (condition.Operator.ToLower().Contains("ends with"))
                {
                    condition.Value = $"%{condition.Value}";
                }
                
                parameters.Add(new SqlParameter($"@p{paramIndex++}", condition.Value ?? DBNull.Value));
            }
        }
        
        return await connection.QueryAsync(sql, parameters.ToArray());
    }

    public async Task<int> CountAsync(string tableName, SearchCriteria? criteria = null)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var whereClause = "";
        if (criteria != null && criteria.Conditions.Any())
        {
            // Simplified - in production use proper parameterization
            whereClause = " WHERE 1=1"; // Would need to build proper WHERE clause
        }
        
        var sql = $"SELECT COUNT(*) FROM [{tableName}]{whereClause}";
        return await connection.ExecuteScalarAsync<int>(sql);
    }
}

/// <summary>
/// Unit of Work implementation
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly IConfiguration _configuration;
    private readonly IDynamicSqlGenerator _sqlGenerator;
    private SqlConnection? _connection;
    private SqlTransaction? _transaction;
    
    private IFormRepository? _forms;
    private IDynamicDataRepository? _data;
    private ISchemaRepository? _schema;
    private IWorkflowRepository? _workflows;
    private ISecurityRepository? _security;
    private IAuditRepository? _audit;

    public UnitOfWork(IConfiguration configuration, IDynamicSqlGenerator sqlGenerator)
    {
        _configuration = configuration;
        _sqlGenerator = sqlGenerator;
    }

    public IFormRepository Forms => _forms ??= new FormRepository(_configuration);
    public IDynamicDataRepository Data => _data ??= new DynamicDataRepository(_configuration, _sqlGenerator);
    public ISchemaRepository Schema => _schema ??= new SchemaRepository(_configuration, _sqlGenerator);
    public IWorkflowRepository Workflows => _workflows ??= new WorkflowRepository(_configuration);
    public ISecurityRepository Security => _security ??= new SecurityRepository(_configuration);
    public IAuditRepository Audit => _audit ??= new AuditRepository(_configuration);

    public async Task BeginTransactionAsync()
    {
        _connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await _connection.OpenAsync();
        _transaction = _connection.BeginTransaction();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            _transaction?.Commit();
        }
        finally
        {
            Dispose();
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            _transaction?.Rollback();
        }
        finally
        {
            Dispose();
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        // For repositories using the same connection/transaction
        return await Task.FromResult(1);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Placeholder implementations for other repositories
public class SchemaRepository : ISchemaRepository
{
    private readonly string _connectionString;
    private readonly IDynamicSqlGenerator _sqlGenerator;

    public SchemaRepository(IConfiguration configuration, IDynamicSqlGenerator sqlGenerator)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not found.");
        _sqlGenerator = sqlGenerator;
    }

    public async Task<bool> CreateTableAsync(TableDefinition table)
    {
        using var connection = new SqlConnection(_connectionString);
        var script = _sqlGenerator.GenerateCreateTableScript(table);
        
        try
        {
            await connection.ExecuteAsync(script);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> AddColumnAsync(string tableName, ColumnDefinition column)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ModifyColumnAsync(string tableName, ColumnDefinition column)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteColumnAsync(string tableName, string columnName)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateIndexAsync(string tableName, IndexDefinition index)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateForeignKeyAsync(ForeignKeyDefinition fk, string tableName)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DropTableAsync(string tableName)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ColumnInfo>> GetTableColumnsAsync(string tableName)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<string>> GetAllTablesAsync()
    {
        throw new NotImplementedException();
    }

    public string GenerateCreateTableScript(TableDefinition table)
    {
        return _sqlGenerator.GenerateCreateTableScript(table);
    }

    public string GenerateAlterColumnScript(string tableName, ColumnDefinition column)
    {
        return _sqlGenerator.GenerateAlterScript(tableName, column, true);
    }
}

public class WorkflowRepository : IWorkflowRepository
{
    private readonly string _connectionString;

    public WorkflowRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not found.");
    }

    public Task<WorkflowDefinition?> GetWorkflowByIdAsync(Guid workflowId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<WorkflowDefinition>> GetAllWorkflowsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Guid> CreateWorkflowAsync(WorkflowDefinition workflow)
    {
        throw new NotImplementedException();
    }

    public Task UpdateWorkflowAsync(WorkflowDefinition workflow)
    {
        throw new NotImplementedException();
    }

    public Task DeleteWorkflowAsync(Guid workflowId)
    {
        throw new NotImplementedException();
    }

    public Task<WorkflowInstance?> GetInstanceAsync(Guid instanceId)
    {
        throw new NotImplementedException();
    }

    public Task<Guid> StartWorkflowAsync(Guid workflowId, string entityType, Guid entityId)
    {
        throw new NotImplementedException();
    }

    public Task TransitionAsync(Guid instanceId, string action, Guid userId, string? comments)
    {
        throw new NotImplementedException();
    }
}

public class SecurityRepository : ISecurityRepository
{
    private readonly string _connectionString;

    public SecurityRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not found.");
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Users WHERE Username = @Username AND IsActive = 1";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }

    public Task<User?> GetUserByIdAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Role>> GetRolesForUserAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<string>> GetPermissionsForUserAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        throw new NotImplementedException();
    }

    public Task<Guid> CreateUserAsync(User user)
    {
        throw new NotImplementedException();
    }

    public Task UpdateUserAsync(User user)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Role>> GetAllRolesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Permission>> GetAllPermissionsAsync()
    {
        throw new NotImplementedException();
    }
}

public class AuditRepository : IAuditRepository
{
    private readonly string _connectionString;

    public AuditRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not found.");
    }

    public async Task LogAsync(AuditLog log)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            INSERT INTO AuditLogs (TableName, RecordId, Action, UserId, UserName, MachineName, IpAddress, Timestamp, OldData, NewData, AffectedColumns)
            VALUES (@TableName, @RecordId, @Action, @UserId, @UserName, @MachineName, @IpAddress, @Timestamp, @OldData, @NewData, @AffectedColumns)";
        
        await connection.ExecuteAsync(sql, log);
    }

    public Task<IEnumerable<AuditLog>> GetLogsForRecordAsync(string tableName, Guid recordId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<AuditLog>> GetLogsByUserAsync(Guid userId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100)
    {
        throw new NotImplementedException();
    }
}
