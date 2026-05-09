# Database Schema

## DbSets Dictionary
`AppDbContext` actively manages the following 11 domain entities:
1. `SiteSettings`
2. `AdminUsers`
3. `Branches`
4. `MenuCategories`
5. `MenuItems`
6. `MenuItemImages`
7. `Reservations`
8. `Posts`
9. `Reviews`
10. `ContactMessages`
11. `MediaAssets`

## Entity Details

### AdminUser
| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key |
| `Username` | `string` | Login username |
| `PasswordHash` | `string` | Secured hash via PasswordHasher |
| `Role` | `string` | Authorization role (default: SuperAdmin) |
| `IsActive` | `bool` | Account status flag |
| `CreatedAt` | `DateTimeOffset` | Auto-managed timestamp |
| `UpdatedAt` | `DateTimeOffset` | Auto-managed timestamp |

### Branch
| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key |
| `Name` | `string` | Branch display name |
| `Address` | `string` | Physical location |
| `Hotline` | `string` | Contact number |

### Reservation
| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key |
| `CustomerName` | `string` | Booker's name |
| `PhoneNumber` | `string` | Contact phone |
| `ReservationDate` | `DateOnly` | Scheduled date |

### MenuCategory
| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key |
| `Name` | `string` | Category title |
| `Slug` | `string` | URL-friendly identifier |

### MenuItem
| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key |
| `CategoryId` | `Guid` | Foreign Key to MenuCategory |
| `Name` | `string` | Item name |
| `Price` | `decimal` | Sale price |

## Active Migrations
Entity Framework Core Migrations currently tracked:
1. `20260508045623_FirstDBMigration` (Initial domain schema)
2. `20260508052050_AddAdminUserTable` (Admin authentication extension)
