# AdminLTE Integration Guide for KRS Dealer Management System

## Overview
AdminLTE v4 is a responsive Bootstrap 5 admin dashboard template. We'll integrate it into ASP.NET Core MVC with:
- Responsive design (mobile, tablet, desktop)
- Dark/Light theme support
- Bootstrap 5 components
- Bootstrap Icons
- Accessibility-first approach

## Project Structure

```
KRSDealerManagement.Web/
├── wwwroot/                          # Static assets
│   ├── css/
│   │   └── adminlte.css             # AdminLTE main stylesheet
│   ├── js/
│   │   ├── adminlte.js              # AdminLTE main JS
│   │   └── custom.js                # Custom app logic
│   ├── lib/
│   │   ├── bootstrap/
│   │   ├── bootstrap-icons/
│   │   └── overlayscrollbars/
│   └── images/
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml           # Main master layout
│   │   ├── _Sidebar.cshtml          # Sidebar nav component
│   │   ├── _TopNav.cshtml           # Top navigation component
│   │   ├── _UserMenu.cshtml         # User dropdown
│   │   └── _MainContent.cshtml      # Content wrapper
│   ├── Admin/
│   │   ├── VehicleModels/
│   │   │   ├── Index.cshtml         # List view (grid)
│   │   │   ├── Create.cshtml        # Create form
│   │   │   ├── Edit.cshtml          # Edit form
│   │   │   └── Details.cshtml       # Detail view
│   │   ├── VehicleColors/
│   │   ├── VehiclePrices/
│   │   ├── Subdealers/
│   │   ├── SubdealerAccounts/
│   │   ├── CommissionRates/
│   │   └── DealerAccounts/
│   ├── Subdealer/
│   │   ├── Orders/
│   │   ├── Commissions/
│   │   ├── Account/
│   │   └── Payments/
│   ├── Dealer/
│   │   ├── Orders/
│   │   ├── Returns/
│   │   └── Payments/
│   └── Account/
│       ├── Login.cshtml
│       └── Logout.cshtml
├── Controllers/
│   ├── HomeController.cs
│   ├── AccountController.cs
│   ├── AdminController.cs
│   ├── SubdealerController.cs
│   └── DealerController.cs
└── Program.cs

```

## Key AdminLTE Components

### 1. Main Layout (_Layout.cshtml)
```html
<!-- Navbar (Top) -->
<!-- Sidebar (Left) -->
<!-- Main Content -->
<!-- Footer -->
```

### 2. Sidebar Navigation
- Collapsible menu items
- Active state tracking
- Icons from Bootstrap Icons
- Role-based visibility

### 3. Forms
- Bootstrap 5 form classes
- Input validation feedback
- Responsive input groups
- Date pickers, Select2, etc.

### 4. Data Tables
- Responsive table classes
- Bootstrap table variations
- DataTables integration for sorting/pagination
- Action buttons (Edit, Delete, Details)

### 5. Cards & Panels
- Info cards with metrics
- Card headers and footers
- Card with icons and colors

### 6. Modals
- Forms in modals
- Confirmation dialogs
- Toast notifications

## CSS Classes Reference

### Grid System (Bootstrap 5)
```html
<div class="row">
  <div class="col-md-6"><!-- Half width on medium+ --></div>
  <div class="col-lg-3"><!-- Quarter width on large+ --></div>
</div>
```

### Cards
```html
<div class="card">
  <div class="card-header">
    <h3 class="card-title">Title</h3>
  </div>
  <div class="card-body">Content</div>
  <div class="card-footer">Footer</div>
</div>
```

### Forms
```html
<form class="form-horizontal">
  <div class="form-group row">
    <label class="col-sm-2 col-form-label">Label</label>
    <div class="col-sm-10">
      <input type="text" class="form-control" />
    </div>
  </div>
</form>
```

### Tables
```html
<table class="table table-bordered table-striped">
  <thead>
    <tr>
      <th>Column</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>Data</td>
    </tr>
  </tbody>
</table>
```

### Buttons & States
```html
<button class="btn btn-primary">Primary</button>
<button class="btn btn-danger">Danger</button>
<button class="btn btn-success">Success</button>
<button class="btn btn-warning">Warning</button>
<button class="btn btn-info">Info</button>

<!-- Sizes -->
<button class="btn btn-sm">Small</button>
<button class="btn btn-lg">Large</button>
```

### Alerts & Badges
```html
<div class="alert alert-info">Info message</div>
<div class="alert alert-danger">Error message</div>
<span class="badge badge-primary">Badge</span>
```

### Icons (Bootstrap Icons)
```html
<i class="bi bi-search"></i>
<i class="bi bi-pencil"></i>
<i class="bi bi-trash"></i>
<i class="bi bi-eye"></i>
<i class="bi bi-plus"></i>
```

## Colors & Theming

### Primary Colors
- Primary (Blue): #007bff
- Success (Green): #28a745
- Danger (Red): #dc3545
- Warning (Orange): #ffc107
- Info (Light Blue): #17a2b8

### Text/Background Classes
```html
<p class="text-danger">Red text</p>
<div class="bg-light">Light background</div>
<span class="text-muted">Muted text</span>
```

## Responsive Design

### Breakpoints
- `xs`: < 576px (mobile)
- `sm`: ≥ 576px (small devices)
- `md`: ≥ 768px (tablets)
- `lg`: ≥ 992px (desktops)
- `xl`: ≥ 1200px (large desktops)
- `xxl`: ≥ 1400px (extra large)

### Responsive Classes
```html
<div class="d-none d-md-block">Hidden on mobile, visible on tablet+</div>
<div class="col-12 col-md-6 col-lg-4">Full width mobile, half on tablet, quarter on desktop</div>
```

## Useful Utilities

### Spacing (Margin & Padding)
```html
<div class="m-3">Margin on all sides</div>
<div class="px-2 py-4">Padding horizontal 2, vertical 4</div>
<div class="mt-5">Margin-top 5</div>
```

### Flexbox
```html
<div class="d-flex justify-content-between align-items-center">
  <span>Left</span>
  <span>Right</span>
</div>
```

### Display
```html
<div class="d-block">Display block</div>
<div class="d-flex">Display flex</div>
<div class="d-none">Hidden</div>
<div class="d-print-none">Hidden on print</div>
```

## Form Examples

### Horizontal Form
```html
<form>
  <div class="form-group row">
    <label class="col-sm-2 col-form-label">Email</label>
    <div class="col-sm-10">
      <input type="email" class="form-control" placeholder="Email" />
    </div>
  </div>
</form>
```

### Inline Form
```html
<form class="form-inline">
  <input class="form-control mr-2" type="text" placeholder="Search" />
  <button class="btn btn-outline-success" type="submit">Search</button>
</form>
```

### Form with Validation
```html
<input type="text" class="form-control is-invalid" />
<div class="invalid-feedback">This field is required</div>

<input type="text" class="form-control is-valid" />
<div class="valid-feedback">Looks good!</div>
```

## Data Table Integration

AdminLTE works well with DataTables for sorting/filtering:
```html
<table id="example" class="table table-bordered">
  <thead>
    <tr>
      <th>Column 1</th>
      <th>Column 2</th>
      <th>Actions</th>
    </tr>
  </thead>
  <tbody>
    <!-- Server-side rendering from Razor -->
  </tbody>
</table>

<script>
$(document).ready(function() {
    $('#example').DataTable({
        responsive: true,
        autoWidth: false
    });
});
</script>
```

## Pagination
```html
<nav>
  <ul class="pagination">
    <li class="page-item"><a class="page-link" href="#">Previous</a></li>
    <li class="page-item active"><a class="page-link" href="#">1</a></li>
    <li class="page-item"><a class="page-link" href="#">2</a></li>
    <li class="page-item"><a class="page-link" href="#">Next</a></li>
  </ul>
</nav>
```

## Modals
```html
<button type="button" class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#exampleModal">
  Launch Modal
</button>

<div class="modal fade" id="exampleModal">
  <div class="modal-dialog">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title">Modal Title</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        Modal content here
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
        <button type="button" class="btn btn-primary">Save</button>
      </div>
    </div>
  </div>
</div>
```

## Toast Notifications
```html
<div class="toast" role="alert">
  <div class="toast-header">
    <strong class="me-auto">Bootstrap</strong>
    <button type="button" class="btn-close" data-bs-dismiss="toast"></button>
  </div>
  <div class="toast-body">
    Hello, world! This is a toast message.
  </div>
</div>

<script>
const toast = new bootstrap.Toast(element);
toast.show();
</script>
```

## Role-Based Sidebar

The sidebar should show/hide menu items based on user role:
```html
@if (User.IsInRole("Admin"))
{
    <li class="nav-item">
        <a href="@Url.Action("Index", "VehicleModels", new { area = "Admin" })" 
           class="nav-link @(ViewContext.RouteData.Values["action"].ToString() == "Index" ? "active" : "")">
            <i class="bi bi-car-front"></i>
            <p>Vehicle Models</p>
        </a>
    </li>
}
```

## Responsive Mobile Menu

AdminLTE automatically collapses sidebar on mobile:
```html
<button class="btn btn-navbar" type="button" data-lte-toggle="sidebar-mini">
    <i class="bi bi-list"></i>
</button>
```

## Next Steps

1. Copy AdminLTE CSS/JS to `wwwroot/`
2. Create `_Layout.cshtml` master template
3. Create `_Sidebar.cshtml` with role-based navigation
4. Create form templates for CRUD operations
5. Create grid/table templates for list views
6. Create modal templates for confirmations
7. Implement responsive design testing

