-- =============================================
-- Low-Code Platform Database Schema
-- Author: Senior Solution Architect
-- Target: SQL Server 2019+
-- =============================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- 1. SECURITY & USERS
-- =============================================

CREATE TABLE [dbo].[Users] (
    [UserId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Username] NVARCHAR(50) NOT NULL UNIQUE,
    [PasswordHash] NVARCHAR(256) NOT NULL,
    [FullName] NVARCHAR(100),
    [Email] NVARCHAR(100),
    [IsActive] BIT DEFAULT 1,
    [CreatedDate] DATETIME2 DEFAULT GETDATE(),
    [LastLoginDate] DATETIME2 NULL
);

CREATE TABLE [dbo].[Roles] (
    [RoleId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [RoleName] NVARCHAR(50) NOT NULL UNIQUE,
    [Description] NVARCHAR(255),
    [IsSystem] BIT DEFAULT 0
);

CREATE TABLE [dbo].[UserRoles] (
    [UserId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Users]([UserId]) ON DELETE CASCADE,
    [RoleId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Roles]([RoleId]) ON DELETE CASCADE,
    PRIMARY KEY ([UserId], [RoleId])
);

CREATE TABLE [dbo].[Permissions] (
    [PermissionId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [PermissionName] NVARCHAR(100) NOT NULL UNIQUE, -- e.g., "Form.Create", "Form.Edit"
    [Description] NVARCHAR(255)
);

CREATE TABLE [dbo].[RolePermissions] (
    [RoleId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Roles]([RoleId]) ON DELETE CASCADE,
    [PermissionId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Permissions]([PermissionId]) ON DELETE CASCADE,
    PRIMARY KEY ([RoleId], [PermissionId])
);

-- =============================================
-- 2. MODULES & MENUS
-- =============================================

CREATE TABLE [dbo].[Modules] (
    [ModuleId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ModuleName] NVARCHAR(100) NOT NULL,
    [Icon] NVARCHAR(50),
    [OrderIndex] INT DEFAULT 0,
    [IsActive] BIT DEFAULT 1
);

CREATE TABLE [dbo].[MenuItems] (
    [MenuItemId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ModuleId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Modules]([ModuleId]),
    [FormId] UNIQUEIDENTIFIER NULL, -- Link to form if it's a leaf node
    [Caption] NVARCHAR(100) NOT NULL,
    [ParentMenuItemId] UNIQUEIDENTIFIER NULL FOREIGN KEY REFERENCES [dbo].[MenuItems]([MenuItemId]),
    [OrderIndex] INT DEFAULT 0,
    [Icon] NVARCHAR(50),
    [IsActive] BIT DEFAULT 1
);

-- =============================================
-- 3. DYNAMIC FORM DEFINITION (METADATA)
-- =============================================

CREATE TABLE [dbo].[Forms] (
    [FormId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [FormName] NVARCHAR(100) NOT NULL,
    [TableName] NVARCHAR(100) NOT NULL, -- Physical DB table name
    [ModuleId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Modules]([ModuleId]),
    [IsMasterDetail] BIT DEFAULT 0,
    [ParentFormId] UNIQUEIDENTIFIER NULL FOREIGN KEY REFERENCES [dbo].[Forms]([FormId]), -- For Child Forms
    [LayoutConfig] NVARCHAR(MAX) NULL, -- JSON: Grid settings, tabs, groups
    [WorkflowId] UNIQUEIDENTIFIER NULL,
    [PrimaryKeyColumn] NVARCHAR(100) DEFAULT 'Id',
    [CreatedDate] DATETIME2 DEFAULT GETDATE(),
    [ModifiedDate] DATETIME2 DEFAULT GETDATE(),
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    [ModifiedBy] UNIQUEIDENTIFIER NULL
);

CREATE TABLE [dbo].[FormControls] (
    [ControlId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [FormId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Forms]([FormId]) ON DELETE CASCADE,
    [ControlType] NVARCHAR(50) NOT NULL, -- TextBox, ComboBox, Grid, DateTimePicker, etc.
    [FieldName] NVARCHAR(100) NOT NULL, -- Database Column Name
    [Label] NVARCHAR(100),
    [OrderIndex] INT DEFAULT 0,
    [RowIndex] INT DEFAULT 0, -- For grid layout
    [ColIndex] INT DEFAULT 0,
    [Width] INT DEFAULT 200,
    [Height] INT DEFAULT 30,
    [IsRequired] BIT DEFAULT 0,
    [IsReadOnly] BIT DEFAULT 0,
    [IsVisible] BIT DEFAULT 1,
    [DefaultValue] NVARCHAR(MAX),
    [ValidationRule] NVARCHAR(MAX), -- Regex or Expression
    [DataSourceQuery] NVARCHAR(MAX), -- For Lookups/Combos (SQL or API endpoint)
    [DisplayMember] NVARCHAR(100), -- For ComboBox/ListBox
    [ValueMember] NVARCHAR(100),   -- For ComboBox/ListBox
    [PropertiesJson] NVARCHAR(MAX), -- Extra UI properties (Colors, Fonts, MaxLength, etc.)
    [TabIndex] INT DEFAULT 0
);

CREATE TABLE [dbo].[FormEvents] (
    [EventId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [FormId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Forms]([FormId]) ON DELETE CASCADE,
    [EventType] NVARCHAR(50) NOT NULL, -- OnLoad, BeforeSave, AfterSave, BeforeDelete, AfterDelete
    [ScriptType] NVARCHAR(20) DEFAULT 'CSharp', -- CSharp, SQL, JavaScript
    [ScriptContent] NVARCHAR(MAX)
);

-- =============================================
-- 4. BUSINESS RULES ENGINE
-- =============================================

CREATE TABLE [dbo].[BusinessRules] (
    [RuleId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [FormId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Forms]([FormId]) ON DELETE CASCADE,
    [RuleName] NVARCHAR(100),
    [RuleGroup] NVARCHAR(50), -- Validation, Automation, Workflow
    [EventType] NVARCHAR(50), -- BeforeSave, AfterSave, OnLoad, OnFieldChange
    [ConditionExpression] NVARCHAR(MAX), -- e.g., "Amount > 1000 && Status == 'Open'"
    [ActionExpression] NVARCHAR(MAX), -- e.g., "SetField('Status', 'ApprovalPending')"
    [ErrorMessage] NVARCHAR(500),
    [IsActive] BIT DEFAULT 1,
    [OrderIndex] INT DEFAULT 0
);

-- =============================================
-- 5. WORKFLOW ENGINE
-- =============================================

CREATE TABLE [dbo].[Workflows] (
    [WorkflowId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [WorkflowName] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(255),
    [DefinitionJson] NVARCHAR(MAX), -- JSON State Machine definition
    [IsActive] BIT DEFAULT 1,
    [CreatedDate] DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE [dbo].[WorkflowStates] (
    [StateId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [WorkflowId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Workflows]([WorkflowId]) ON DELETE CASCADE,
    [StateName] NVARCHAR(50) NOT NULL, -- Draft, Submitted, Approved, Rejected
    [IsInitial] BIT DEFAULT 0,
    [IsFinal] BIT DEFAULT 0,
    [Color] NVARCHAR(20) DEFAULT '#CCCCCC'
);

CREATE TABLE [dbo].[WorkflowTransitions] (
    [TransitionId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [WorkflowId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Workflows]([WorkflowId]) ON DELETE CASCADE,
    [FromStateId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[WorkflowStates]([StateId]),
    [ToStateId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[WorkflowStates]([StateId]),
    [ActionName] NVARCHAR(50) NOT NULL, -- Submit, Approve, Reject
    [RequiredRoleId] UNIQUEIDENTIFIER NULL, -- Role required to perform this action
    [AllowRecall] BIT DEFAULT 0
);

CREATE TABLE [dbo].[WorkflowInstances] (
    [InstanceId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [WorkflowId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Workflows]([WorkflowId]),
    [EntityType] NVARCHAR(100), -- Table name
    [EntityId] UNIQUEIDENTIFIER, -- Record ID
    [CurrentStateId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[WorkflowStates]([StateId]),
    [Status] NVARCHAR(50) DEFAULT 'Active',
    [StartedDate] DATETIME2 DEFAULT GETDATE(),
    [CompletedDate] DATETIME2 NULL
);

CREATE TABLE [dbo].[WorkflowHistory] (
    [HistoryId] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [InstanceId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[WorkflowInstances]([InstanceId]),
    [FromStateId] UNIQUEIDENTIFIER NULL,
    [ToStateId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[WorkflowStates]([StateId]),
    [ActionTaken] NVARCHAR(50),
    [Comments] NVARCHAR(MAX),
    [UserId] UNIQUEIDENTIFIER,
    [Timestamp] DATETIME2 DEFAULT GETDATE()
);

-- =============================================
-- 6. AUDIT TRAIL
-- =============================================

CREATE TABLE [dbo].[AuditLogs] (
    [AuditId] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [TableName] NVARCHAR(100) NOT NULL,
    [RecordId] UNIQUEIDENTIFIER,
    [Action] NVARCHAR(20) NOT NULL, -- INSERT, UPDATE, DELETE, LOGIN, LOGOUT
    [UserId] UNIQUEIDENTIFIER,
    [UserName] NVARCHAR(100),
    [MachineName] NVARCHAR(100),
    [IpAddress] NVARCHAR(50),
    [Timestamp] DATETIME2 DEFAULT GETDATE(),
    [OldData] NVARCHAR(MAX) NULL, -- JSON
    [NewData] NVARCHAR(MAX) NULL,  -- JSON
    [AffectedColumns] NVARCHAR(MAX) NULL -- Comma-separated list of changed columns
);

-- =============================================
-- 7. SEARCH CONFIGURATION
-- =============================================

CREATE TABLE [dbo].[SavedSearches] (
    [SearchId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [FormId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Forms]([FormId]),
    [SearchName] NVARCHAR(100) NOT NULL,
    [UserId] UNIQUEIDENTIFIER,
    [IsPublic] BIT DEFAULT 0,
    [FilterDefinition] NVARCHAR(MAX), -- JSON: Conditions, Operators, Values
    [CreatedDate] DATETIME2 DEFAULT GETDATE()
);

-- =============================================
-- 8. REPORTS & DASHBOARDS
-- =============================================

CREATE TABLE [dbo].[Reports] (
    [ReportId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ReportName] NVARCHAR(100) NOT NULL,
    [ReportType] NVARCHAR(50) DEFAULT 'Tabular', -- Tabular, MasterDetail, Chart
    [DefinitionJson] NVARCHAR(MAX), -- Query, Columns, Grouping, Chart config
    [FormId] UNIQUEIDENTIFIER NULL,
    [IsActive] BIT DEFAULT 1
);

CREATE TABLE [dbo].[Dashboards] (
    [DashboardId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [DashboardName] NVARCHAR(100) NOT NULL,
    [LayoutJson] NVARCHAR(MAX), -- Widget positions and sizes
    [UserId] UNIQUEIDENTIFIER,
    [IsDefault] BIT DEFAULT 0
);

CREATE TABLE [dbo].[DashboardWidgets] (
    [WidgetId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [DashboardId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Dashboards]([DashboardId]) ON DELETE CASCADE,
    [WidgetType] NVARCHAR(50), -- KPI, Chart, Grid, Counter
    [Title] NVARCHAR(100),
    [ConfigurationJson] NVARCHAR(MAX),
    [RowPosition] INT DEFAULT 0,
    [ColPosition] INT DEFAULT 0,
    [Width] INT DEFAULT 2,
    [Height] INT DEFAULT 1
);

-- =============================================
-- 9. API CONFIGURATION
-- =============================================

CREATE TABLE [dbo].[ApiEndpoints] (
    [EndpointId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [FormId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Forms]([FormId]),
    [Route] NVARCHAR(200),
    [HttpMethod] NVARCHAR(20), -- GET, POST, PUT, DELETE
    [IsRequiredAuth] BIT DEFAULT 1,
    [AllowedRoles] NVARCHAR(MAX), -- Comma-separated role names
    [IsActive] BIT DEFAULT 1
);

-- =============================================
-- 10. IMPORT/EXPORT TEMPLATES
-- =============================================

CREATE TABLE [dbo].[ImportTemplates] (
    [TemplateId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [FormId] UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [dbo].[Forms]([FormId]),
    [TemplateName] NVARCHAR(100),
    [FileType] NVARCHAR(20), -- Excel, CSV, JSON
    [ColumnMappingJson] NVARCHAR(MAX), -- Maps file columns to DB columns
    [CreatedDate] DATETIME2 DEFAULT GETDATE()
);

-- =============================================
-- INDEXES FOR PERFORMANCE
-- =============================================

CREATE NONCLUSTERED INDEX IX_FormControls_FormId ON [dbo].[FormControls]([FormId]);
CREATE NONCLUSTERED INDEX IX_BusinessRules_FormId ON [dbo].[BusinessRules]([FormId]);
CREATE NONCLUSTERED INDEX IX_AuditLogs_Timestamp ON [dbo].[AuditLogs]([Timestamp]);
CREATE NONCLUSTERED INDEX IX_AuditLogs_TableName_RecordId ON [dbo].[AuditLogs]([TableName], [RecordId]);
CREATE NONCLUSTERED INDEX IX_MenuItems_ModuleId ON [dbo].[MenuItems]([ModuleId]);
CREATE NONCLUSTERED INDEX IX_WorkflowInstances_Entity ON [dbo].[WorkflowInstances]([EntityType], [EntityId]);

GO

-- =============================================
-- SEED DATA
-- =============================================

-- Default Roles
INSERT INTO [dbo].[Roles] ([RoleId], [RoleName], [Description], [IsSystem]) VALUES 
('11111111-1111-1111-1111-111111111111', 'Admin', 'System Administrator', 1),
('22222222-2222-2222-2222-222222222222', 'Developer', 'Application Developer', 1),
('33333333-3333-3333-3333-333333333333', 'User', 'Standard User', 1);

-- Default Permissions
INSERT INTO [dbo].[Permissions] ([PermissionId], [PermissionName], [Description]) VALUES 
(NEWID(), 'Form.Create', 'Create new forms'),
(NEWID(), 'Form.Edit', 'Edit existing forms'),
(NEWID(), 'Form.Delete', 'Delete forms'),
(NEWID(), 'Form.View', 'View forms'),
(NEWID(), 'Data.Create', 'Create data records'),
(NEWID(), 'Data.Edit', 'Edit data records'),
(NEWID(), 'Data.Delete', 'Delete data records'),
(NEWID(), 'Data.View', 'View data records');

-- Assign all permissions to Admin
DECLARE @AdminRole UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId])
SELECT @AdminRole, [PermissionId] FROM [dbo].[Permissions];

-- Default Module
INSERT INTO [dbo].[Modules] ([ModuleId], [ModuleName], [Icon], [OrderIndex]) VALUES 
('AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA', 'General', 'fa-home', 0);

-- Default Admin User (password: admin123 - hash should be computed in real app)
INSERT INTO [dbo].[Users] ([UserId], [Username], [PasswordHash], [FullName], [Email], [IsActive]) VALUES 
('DEMOUSER-DEMO-DEMO-DEMO-DEMODEMOUSER', 'admin', '$2a$11$wH.3yJvZqKxN9L8mR5tF.eOQpXzY7VxWqKjHgFdSaCbNmPoLiGkRe', 'System Administrator', 'admin@localhost', 1);

-- Assign Admin role to admin user
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES 
('DEMOUSER-DEMO-DEMO-DEMO-DEMODEMOUSER', '11111111-1111-1111-1111-111111111111');

GO

PRINT 'Database schema created successfully!';
GO
