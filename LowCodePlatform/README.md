# Low-Code Development Platform

A comprehensive enterprise-grade low-code platform built with .NET 8, WPF, and SQL Server that enables business users and developers to dynamically create forms, database structures, workflows, and search screens without writing code.

## Architecture Overview

### Technology Stack
- **Frontend**: WPF (.NET 8) with MahApps.Metro for modern UI
- **Backend**: .NET 8 Class Libraries
- **Database**: SQL Server 2019+
- **ORM**: Dapper for dynamic queries
- **Pattern**: MVVM, Repository Pattern, Unit of Work

### Solution Structure

```
LowCodePlatform/
├── Database/
│   └── Schema.sql              # Complete database schema
├── src/
│   ├── LowCode.Platform.Core/          # Domain entities & interfaces
│   │   ├── Domain/
│   │   │   └── Entities.cs            # FormDefinition, BusinessRule, etc.
│   │   ├── Interfaces/
│   │   │   └── IRepositories.cs       # Repository contracts
│   │   └── Services/
│   │       └── DynamicServices.cs     # SQL Generator, Rule Engine
│   ├── LowCode.Platform.Infrastructure/ # Data access layer
│   │   └── Data/
│   │       └── Repositories.cs        # SQL implementations
│   └── LowCode.Platform.UI/           # WPF Application
│       ├── Views/
│       │   └── MainWindow.xaml        # Main application window
│       ├── ViewModels/
│       ├── Controls/                   # Custom controls
│       └── Themes/
│           └── CustomStyles.xaml      # Modern UI styles
└── tests/
```

## Core Features

### 1. Dynamic Form Designer
- Create, edit, clone, delete forms at runtime
- Organize forms into modules
- Supported controls: TextBox, ComboBox, Grid, DatePicker, RichTextBox, etc.
- Configurable properties: Name, Caption, Validation, Data Binding, Events

### 2. Dynamic Database Designer
- Create tables and columns dynamically
- Support for all SQL Server data types
- Indexes (clustered, non-clustered, unique, composite)
- Foreign key relationships with cascade options
- Automatic SQL script generation

### 3. Master-Detail Forms
- Parent-child form relationships
- Nested detail grids
- Auto-save parent-child records

### 4. Dynamic CRUD Generator
- Automatic Create, Read, Update, Delete operations
- No coding required

### 5. Search Wizard Builder
- Filter builder with multiple conditions
- Advanced search capabilities
- Saved searches and templates
- Operators: Equals, Contains, Between, Greater Than, etc.

### 6. Business Rules Engine
- Define logic without coding
- Event-based rules (BeforeSave, AfterSave, OnLoad)
- Condition expressions and action chaining
- Validation rules with error messages

### 7. Workflow Engine
- Visual workflow designer
- States: Draft, Submitted, Approved, Rejected
- Actions: Approve, Reject, Return, Recall
- Multi-level and role-based approvals
- Email notifications support

### 8. Security Framework
- Authentication and Authorization
- Role-based access control (RBAC)
- Permissions and claims
- Row-level and field-level security

### 9. Audit Trail
- Track all data changes
- Log Insert, Update, Delete operations
- Store old and new values in JSON format
- User, machine, timestamp tracking

### 10. Reporting Module
- Tabular and Master-Detail reports
- Charts and visualizations
- Export to Excel and PDF
- Print support

### 11. Dashboard Builder
- Drag-and-drop widgets
- KPI cards, charts, grids, counters
- Customizable layouts

### 12. Import/Export Framework
- Excel, CSV, JSON support
- Column mapping configuration

### 13. API Layer
- RESTful endpoints auto-generated
- GET, POST, PUT, DELETE operations
- Role-based endpoint security

## Database Schema

The platform uses a metadata-driven architecture. All forms, controls, rules, and configurations are stored in the database:

### Key Tables
- **Users/Roles/Permissions**: Security framework
- **Modules/MenuItems**: Navigation structure
- **Forms/FormControls**: Form definitions and layout
- **BusinessRules**: Rule engine configuration
- **Workflows/WorkflowStates/WorkflowTransitions**: Workflow definitions
- **AuditLogs**: Change tracking
- **SavedSearches**: User-defined searches
- **Reports/Dashboards**: Reporting configuration

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server 2019 or later
- Visual Studio 2022 (recommended)

### Installation Steps

1. **Create Database**
```sql
-- Execute the schema script
sqlcmd -S localhost -d master -i Database/Schema.sql
```

2. **Configure Connection String**
Edit `src/LowCode.Platform.UI/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LowCodePlatform;Trusted_Connection=True;"
  }
}
```

3. **Build and Run**
```bash
cd src/LowCode.Platform.UI
dotnet restore
dotnet build
dotnet run
```

## Metadata-Driven Architecture

The entire platform is metadata-driven. The runtime engine reads form definitions from the database and renders UI dynamically:

```csharp
// Example: Loading a form definition
var form = await formRepository.GetFormByIdAsync(formId);
foreach (var control in form.Controls)
{
    // Dynamically create UI controls based on metadata
    var uiControl = CreateControl(control.ControlType);
    uiControl.SetValue(control.FieldName);
    uiControl.SetLabel(control.Label);
}
```

## Extensibility

### Custom Controls
Extend the platform by creating custom controls that implement the base control interface.

### Custom Scripts
Add C#, SQL, or JavaScript snippets for custom business logic:
- Before Save
- After Save
- Before Delete
- After Delete

### API Extensions
Expose custom endpoints through the ApiEndpoints table configuration.

## Security Considerations

- Password hashing using bcrypt
- Parameterized queries to prevent SQL injection
- Role-based authorization on all operations
- Audit logging for compliance
- Input validation on all user inputs

## Performance Optimization

- Indexed metadata tables
- Cached form definitions
- Efficient dynamic SQL generation
- Pagination for large datasets
- Async/await throughout the codebase

## License

Enterprise License - Contact for commercial use.

## Support

For documentation, tutorials, and support, please refer to the project wiki.
