namespace LowCode.Platform.Core.Interfaces;

/// <summary>
/// Repository interface for dynamic form operations
/// </summary>
public interface IFormRepository
{
    Task<FormDefinition?> GetFormByIdAsync(Guid formId);
    Task<FormDefinition?> GetFormByNameAsync(string formName);
    Task<IEnumerable<FormDefinition>> GetAllFormsAsync();
    Task<IEnumerable<FormDefinition>> GetFormsByModuleAsync(Guid moduleId);
    Task<Guid> CreateFormAsync(FormDefinition form);
    Task UpdateFormAsync(FormDefinition form);
    Task DeleteFormAsync(Guid formId);
    Task<IEnumerable<FormControlDefinition>> GetControlsForFormAsync(Guid formId);
    Task AddControlAsync(Guid formId, FormControlDefinition control);
    Task UpdateControlAsync(FormControlDefinition control);
    Task DeleteControlAsync(Guid controlId);
}

/// <summary>
/// Repository interface for dynamic data operations (CRUD on user-created tables)
/// </summary>
public interface IDynamicDataRepository
{
    Task<IEnumerable<dynamic>> GetAllAsync(string tableName);
    Task<dynamic?> GetByIdAsync(string tableName, Guid id);
    Task<Guid> InsertAsync(string tableName, IDictionary<string, object?> data);
    Task UpdateAsync(string tableName, Guid id, IDictionary<string, object?> data);
    Task DeleteAsync(string tableName, Guid id);
    Task<IEnumerable<dynamic>> SearchAsync(string tableName, SearchCriteria criteria);
    Task<int> CountAsync(string tableName, SearchCriteria? criteria = null);
}

/// <summary>
/// Search criteria for dynamic queries
/// </summary>
public class SearchCriteria
{
    public List<SearchCondition> Conditions { get; set; } = new();
    public string? OrderBy { get; set; }
    public bool OrderDescending { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Individual search condition
/// </summary>
public class SearchCondition
{
    public string FieldName { get; set; } = string.Empty;
    public string Operator { get; set; } = "Equals"; // Equals, Contains, GreaterThan, etc.
    public object? Value { get; set; }
    public object? Value2 { get; set; } // For Between operator
    public string LogicalOperator { get; set; } = "AND"; // AND, OR
}

/// <summary>
/// Repository interface for database schema operations
/// </summary>
public interface ISchemaRepository
{
    Task<bool> CreateTableAsync(TableDefinition table);
    Task<bool> AddColumnAsync(string tableName, ColumnDefinition column);
    Task<bool> ModifyColumnAsync(string tableName, ColumnDefinition column);
    Task<bool> DeleteColumnAsync(string tableName, string columnName);
    Task<bool> CreateIndexAsync(string tableName, IndexDefinition index);
    Task<bool> CreateForeignKeyAsync(ForeignKeyDefinition fk, string tableName);
    Task<bool> DropTableAsync(string tableName);
    Task<IEnumerable<ColumnInfo>> GetTableColumnsAsync(string tableName);
    Task<IEnumerable<string>> GetAllTablesAsync();
    string GenerateCreateTableScript(TableDefinition table);
    string GenerateAlterColumnScript(string tableName, ColumnDefinition column);
}

/// <summary>
/// Column information from database
/// </summary>
public class ColumnInfo
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? Length { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Repository interface for workflow operations
/// </summary>
public interface IWorkflowRepository
{
    Task<WorkflowDefinition?> GetWorkflowByIdAsync(Guid workflowId);
    Task<IEnumerable<WorkflowDefinition>> GetAllWorkflowsAsync();
    Task<Guid> CreateWorkflowAsync(WorkflowDefinition workflow);
    Task UpdateWorkflowAsync(WorkflowDefinition workflow);
    Task DeleteWorkflowAsync(Guid workflowId);
    Task<WorkflowInstance?> GetInstanceAsync(Guid instanceId);
    Task<Guid> StartWorkflowAsync(Guid workflowId, string entityType, Guid entityId);
    Task TransitionAsync(Guid instanceId, string action, Guid userId, string? comments);
}

/// <summary>
/// Workflow instance tracking
/// </summary>
public class WorkflowInstance
{
    public Guid InstanceId { get; set; }
    public Guid WorkflowId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid CurrentStateId { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
}

/// <summary>
/// Repository interface for security operations
/// </summary>
public interface ISecurityRepository
{
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<IEnumerable<Role>> GetRolesForUserAsync(Guid userId);
    Task<IEnumerable<string>> GetPermissionsForUserAsync(Guid userId);
    Task<bool> ValidateCredentialsAsync(string username, string password);
    Task<Guid> CreateUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task<IEnumerable<Role>> GetAllRolesAsync();
    Task<IEnumerable<Permission>> GetAllPermissionsAsync();
}

/// <summary>
/// Repository interface for audit logging
/// </summary>
public interface IAuditRepository
{
    Task LogAsync(AuditLog log);
    Task<IEnumerable<AuditLog>> GetLogsForRecordAsync(string tableName, Guid recordId);
    Task<IEnumerable<AuditLog>> GetLogsByUserAsync(Guid userId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100);
}

/// <summary>
/// Service interface for business rule evaluation
/// </summary>
public interface IBusinessRuleEngine
{
    Task<RuleEvaluationResult> EvaluateBeforeSaveAsync(Guid formId, IDictionary<string, object?> data);
    Task<RuleEvaluationResult> EvaluateAfterSaveAsync(Guid formId, IDictionary<string, object?> data, Guid recordId);
    Task<RuleEvaluationResult> EvaluateOnLoadAsync(Guid formId, Guid? recordId = null);
    Task ApplyActionAsync(string actionExpression, IDictionary<string, object?> data);
}

/// <summary>
/// Result of business rule evaluation
/// </summary>
public class RuleEvaluationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> ErrorMessages { get; set; } = new();
    public Dictionary<string, object?> ModifiedValues { get; set; } = new();
    public List<string> ExecutedActions { get; set; } = new();
}

/// <summary>
/// Service interface for dynamic SQL generation
/// </summary>
public interface IDynamicSqlGenerator
{
    string GenerateInsertQuery(string tableName, IEnumerable<string> columns);
    string GenerateUpdateQuery(string tableName, IEnumerable<string> columns, string primaryKeyColumn);
    string GenerateDeleteQuery(string tableName, string primaryKeyColumn);
    string GenerateSelectQuery(string tableName, IEnumerable<string>? columns = null, SearchCriteria? criteria = null);
    string GenerateCreateTableScript(TableDefinition table);
    string GenerateAlterScript(string tableName, ColumnDefinition column, bool isAdd = true);
}

/// <summary>
/// Service interface for report generation
/// </summary>
public interface IReportService
{
    Task<ReportResult> ExecuteReportAsync(Guid reportId, Dictionary<string, object?> parameters);
    Task<byte[]> ExportToExcelAsync(ReportResult result);
    Task<byte[]> ExportToPdfAsync(ReportResult result);
}

/// <summary>
/// Report execution result
/// </summary>
public class ReportResult
{
    public string ReportName { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public List<IDictionary<string, object?>> Data { get; set; } = new();
    public Dictionary<string, object>? Summary { get; set; }
}

/// <summary>
/// Unit of Work pattern for transaction management
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IFormRepository Forms { get; }
    IDynamicDataRepository Data { get; }
    ISchemaRepository Schema { get; }
    IWorkflowRepository Workflows { get; }
    ISecurityRepository Security { get; }
    IAuditRepository Audit { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
