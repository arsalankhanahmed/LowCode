namespace LowCode.Platform.Core.Domain;

/// <summary>
/// Represents a dynamic form definition stored in metadata
/// </summary>
public class FormDefinition
{
    public Guid FormId { get; set; }
    public string FormName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public Guid ModuleId { get; set; }
    public bool IsMasterDetail { get; set; }
    public Guid? ParentFormId { get; set; }
    public string? LayoutConfig { get; set; } // JSON
    public Guid? WorkflowId { get; set; }
    public string PrimaryKeyColumn { get; set; } = "Id";
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? ModifiedBy { get; set; }
    
    public virtual ICollection<FormControlDefinition> Controls { get; set; } = new List<FormControlDefinition>();
    public virtual ICollection<BusinessRule> BusinessRules { get; set; } = new List<BusinessRule>();
}

/// <summary>
/// Represents a control on a dynamic form
/// </summary>
public class FormControlDefinition
{
    public Guid ControlId { get; set; }
    public Guid FormId { get; set; }
    public string ControlType { get; set; } = string.Empty; // TextBox, ComboBox, Grid, etc.
    public string FieldName { get; set; } = string.Empty;
    public string? Label { get; set; }
    public int OrderIndex { get; set; }
    public int RowIndex { get; set; }
    public int ColIndex { get; set; }
    public int Width { get; set; } = 200;
    public int Height { get; set; } = 30;
    public bool IsRequired { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? DefaultValue { get; set; }
    public string? ValidationRule { get; set; }
    public string? DataSourceQuery { get; set; }
    public string? DisplayMember { get; set; }
    public string? ValueMember { get; set; }
    public string? PropertiesJson { get; set; }
    public int TabIndex { get; set; }
}

/// <summary>
/// Represents a business rule for validation or automation
/// </summary>
public class BusinessRule
{
    public Guid RuleId { get; set; }
    public Guid FormId { get; set; }
    public string? RuleName { get; set; }
    public string? RuleGroup { get; set; } // Validation, Automation, Workflow
    public string EventType { get; set; } = string.Empty; // BeforeSave, AfterSave, OnLoad
    public string? ConditionExpression { get; set; }
    public string? ActionExpression { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsActive { get; set; } = true;
    public int OrderIndex { get; set; }
}

/// <summary>
/// Represents a workflow definition
/// </summary>
public class WorkflowDefinition
{
    public Guid WorkflowId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefinitionJson { get; set; } // State machine JSON
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    
    public virtual ICollection<WorkflowState> States { get; set; } = new List<WorkflowState>();
    public virtual ICollection<WorkflowTransition> Transitions { get; set; } = new List<WorkflowTransition>();
}

/// <summary>
/// Represents a state in a workflow
/// </summary>
public class WorkflowState
{
    public Guid StateId { get; set; }
    public Guid WorkflowId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public bool IsInitial { get; set; }
    public bool IsFinal { get; set; }
    public string Color { get; set; } = "#CCCCCC";
}

/// <summary>
/// Represents a transition between workflow states
/// </summary>
public class WorkflowTransition
{
    public Guid TransitionId { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid FromStateId { get; set; }
    public Guid ToStateId { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public Guid? RequiredRoleId { get; set; }
    public bool AllowRecall { get; set; }
}

/// <summary>
/// Represents a user in the system
/// </summary>
public class User
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
}

/// <summary>
/// Represents a role in the system
/// </summary>
public class Role
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
}

/// <summary>
/// Represents a permission
/// </summary>
public class Permission
{
    public Guid PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Represents an audit log entry
/// </summary>
public class AuditLog
{
    public long AuditId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public Guid RecordId { get; set; }
    public string Action { get; set; } = string.Empty; // INSERT, UPDATE, DELETE
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? MachineName { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
    public string? OldData { get; set; } // JSON
    public string? NewData { get; set; } // JSON
    public string? AffectedColumns { get; set; }
}

/// <summary>
/// Represents a saved search configuration
/// </summary>
public class SavedSearch
{
    public Guid SearchId { get; set; }
    public Guid FormId { get; set; }
    public string SearchName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public bool IsPublic { get; set; }
    public string? FilterDefinition { get; set; } // JSON
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// Represents a report definition
/// </summary>
public class ReportDefinition
{
    public Guid ReportId { get; set; }
    public string ReportName { get; set; } = string.Empty;
    public string ReportType { get; set; } = "Tabular";
    public string? DefinitionJson { get; set; }
    public Guid? FormId { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Represents a dashboard
/// </summary>
public class Dashboard
{
    public Guid DashboardId { get; set; }
    public string DashboardName { get; set; } = string.Empty;
    public string? LayoutJson { get; set; }
    public Guid UserId { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>
/// Column definition for dynamic table creation
/// </summary>
public class ColumnDefinition
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty; // int, varchar, datetime, etc.
    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsNullable { get; set; } = true;
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsUnique { get; set; }
}

/// <summary>
/// Table definition for dynamic creation
/// </summary>
public class TableDefinition
{
    public string TableName { get; set; } = string.Empty;
    public List<ColumnDefinition> Columns { get; set; } = new();
    public List<ForeignKeyDefinition> ForeignKeys { get; set; } = new();
    public List<IndexDefinition> Indexes { get; set; } = new();
}

/// <summary>
/// Foreign key relationship definition
/// </summary>
public class ForeignKeyDefinition
{
    public string ConstraintName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string ReferencedTable { get; set; } = string.Empty;
    public string ReferencedColumn { get; set; } = string.Empty;
    public bool CascadeDelete { get; set; }
    public bool CascadeUpdate { get; set; }
}

/// <summary>
/// Index definition
/// </summary>
public class IndexDefinition
{
    public string IndexName { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public bool IsUnique { get; set; }
    public bool IsClustered { get; set; }
}
