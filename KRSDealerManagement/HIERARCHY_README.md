# Hierarchy — cleaned & wired

## DB (source of truth)

| Keep | Dropped |
|------|---------|
| `Dealerships`, `SubDealers`, `Roles`, `RoleMenus`, `UserOrgRoles` | `Dealers`, `Users.DealerId` |

Duplicate users removed: `karur_admin`, `namakkal_admin`, `salem_admin`, `finance_admin`.

## Logins (password = `{CODE}@123`)

| User | Role | Scope |
|------|------|--------|
| `admin` / `Admin@123` | System Admin | All locations |
| `karur_mgr` / `KARUR@123` | Branch Manager | Karur only |
| `karur_finance` / `KARUR@123` | Finance Admin | Karur only |
| same pattern for namakkal, salem, erode | | |

## Menus by role (per location for branch/finance)

| Role | Menus |
|------|--------|
| Branch Manager | Subdealers, Manage Orders, Return Requests |
| Finance Admin | Balances, Payment Approvals, Reports, Account Statements |
| System Admin | All |

Branch manager **cannot** open finance screens. Finance admin **only** finance screens.
