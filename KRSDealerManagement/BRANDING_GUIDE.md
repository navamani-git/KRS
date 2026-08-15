# KRS Dealer Management System - Branding & Logo Guide

## Logo Integration

### Logo File
- **File:** `krslogo.png`
- **Location:** `d:\KRS\Requirement\krslogo.png`
- **To be copied to:** `wwwroot/images/krslogo.png`

## Logo Usage Locations

### 1. Login Page
**Path:** `Views/Account/Login.cshtml`

**Usage:**
- Display centered at top of login form
- Size: 150px x 150px (or appropriate size)
- Responsive: 100px on mobile, 150px on desktop

```html
<div class="login-box">
  <div class="login-logo text-center mb-4">
    <img src="@Url.Content("~/images/krslogo.png")" 
         alt="KRS Logo" 
         class="img-fluid" 
         style="max-width: 150px; height: auto;" />
  </div>
  <!-- Login form -->
</div>
```

### 2. Main Layout - Navbar
**Path:** `Views/Shared/_Layout.cshtml`

**Usage:**
- Display in navbar brand area
- Size: 40px x 40px with text
- Responsive: Scale appropriately

```html
<nav class="navbar navbar-expand-lg navbar-dark bg-dark">
  <div class="container-fluid">
    <a class="navbar-brand" href="@Url.Action("Index", "Home")">
      <img src="@Url.Content("~/images/krslogo.png")" 
           alt="KRS" 
           class="d-inline-block align-text-top me-2" 
           style="height: 40px; width: auto;" />
      <strong>KRS Dealer Management</strong>
    </a>
  </div>
</nav>
```

### 3. Sidebar - Brand Section
**Path:** `Views/Shared/_Sidebar.cshtml`

**Usage:**
- Display at top of sidebar
- Size: 50px x 50px with text
- Sticky position

```html
<div class="sidebar-brand mb-3 p-3 border-bottom">
  <div class="d-flex align-items-center">
    <img src="@Url.Content("~/images/krslogo.png")" 
         alt="KRS" 
         class="rounded-circle" 
         style="height: 50px; width: 50px; object-fit: cover;" />
    <div class="ms-3">
      <h6 class="mb-0">KRS Dealer</h6>
      <small class="text-muted">Management System</small>
    </div>
  </div>
</div>
```

### 4. Dashboard Header
**Path:** `Views/Home/Index.cshtml` or `Views/Shared/_MainContent.cshtml`

**Usage:**
- Welcome banner with logo
- Size: 100px x 100px

```html
<div class="row mb-4">
  <div class="col-12">
    <div class="card bg-primary text-white">
      <div class="card-body d-flex align-items-center">
        <img src="@Url.Content("~/images/krslogo.png")" 
             alt="KRS" 
             class="rounded-circle me-3" 
             style="height: 80px; width: 80px; object-fit: cover;" />
        <div>
          <h4 class="card-title mb-0">Welcome to KRS Dealer Management</h4>
          <p class="card-text mb-0">Professional Vehicle Dealership Platform</p>
        </div>
      </div>
    </div>
  </div>
</div>
```

### 5. Print/Export Header
**Usage:**
- Include in printed reports
- Size: 80px x 80px

```html
<!-- For PDF/Print reports -->
<div class="print-header text-center mb-4" style="page-break-after: always;">
  <img src="@Url.Content("~/images/krslogo.png")" 
       alt="KRS" 
       style="height: 80px; width: auto;" />
  <h2>KRS Dealer Management System</h2>
  <p>Professional Vehicle Dealership Platform</p>
</div>
```

## Logo Responsive Sizes

| Location | Mobile | Tablet | Desktop | Use Case |
|----------|--------|--------|---------|----------|
| Login Page | 100px | 125px | 150px | Centered login header |
| Navbar | 30px | 35px | 40px | Brand logo in header |
| Sidebar | 45px | 50px | 50px | Brand section top |
| Dashboard | 60px | 70px | 80px | Welcome banner |
| Reports | 60px | 70px | 80px | Print/export header |

## CSS Classes for Logo

### Circular Display (Sidebar/Dashboard)
```css
.logo-circular {
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid rgba(255, 255, 255, 0.2);
}
```

### Square Display (Navbar)
```css
.logo-square {
  object-fit: contain;
  padding: 2px;
}
```

### Responsive Display
```css
.logo-responsive {
  max-width: 100%;
  height: auto;
}

@media (max-width: 576px) {
  .logo-responsive {
    max-width: 100px;
  }
}

@media (min-width: 768px) {
  .logo-responsive {
    max-width: 150px;
  }
}
```

## Favicon Integration

Create favicon from logo:
```html
<!-- In _Layout.cshtml <head> section -->
<link rel="shortcut icon" href="@Url.Content("~/images/krslogo.png")" type="image/png" />
<link rel="apple-touch-icon" href="@Url.Content("~/images/krslogo.png")" />
```

## Logo File Setup Steps

1. **Copy Logo File**
   ```
   From: d:\KRS\Requirement\krslogo.png
   To: d:\KRS\KRSDealerManagement\KRSDealerManagement.Web\wwwroot\images\
   ```

2. **Verify in Project**
   - Right-click wwwroot → Add Existing Item
   - Browse to krslogo.png
   - Include in project

3. **Update Views**
   - Use `@Url.Content("~/images/krslogo.png")` in Razor views
   - Ensures correct path in all environments

4. **Test Responsive**
   - Test on mobile (320px), tablet (768px), desktop (1200px)
   - Verify logo displays correctly at all sizes
   - Check print/export renders correctly

## Color Usage

If logo needs color adjustments:

### Background Colors
- **Dark navbar:** Use white/light logo or invert
- **White background:** Use colored logo
- **Admin sections:** Use primary color theme
- **Subdealer sections:** Use secondary color theme
- **Dealer sections:** Use accent color theme

### Hover Effects
```css
.navbar-brand img:hover {
  opacity: 0.8;
  transition: opacity 0.3s ease;
}

.sidebar-brand img:hover {
  transform: scale(1.05);
  transition: transform 0.3s ease;
}
```

## Alt Text Standards

Always include meaningful alt text:
```html
<!-- ✓ Good -->
<img src="krslogo.png" alt="KRS Dealer Management Logo" />

<!-- ✗ Avoid -->
<img src="krslogo.png" alt="logo" />
<img src="krslogo.png" alt="" />
```

## Accessibility

Logo should be:
- **Decorative in navbar:** `aria-hidden="true"` with meaningful brand text
- **Functional in login:** Include alt text for screen readers
- **Part of branding:** Use semantic HTML with proper contrast

```html
<!-- Good example with accessibility -->
<a class="navbar-brand" href="/">
  <img src="krslogo.png" 
       alt="KRS" 
       class="d-inline-block align-text-top" 
       style="height: 40px;" />
  <span class="ms-2">KRS Dealer Management</span>
</a>
```

## Next Steps

1. Copy krslogo.png to `wwwroot/images/`
2. Update _Layout.cshtml with logo in navbar/sidebar
3. Create Login.cshtml with centered logo
4. Add favicon references
5. Test responsive display on all devices
6. Test print/export functionality

