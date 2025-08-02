# AgroProduct E-commerce Application

A modern ASP.NET Core e-commerce application for agricultural products with user authentication, shopping cart functionality, and admin management.

## Features

- **User Authentication & Authorization**: Register, login, and profile management
- **Product Catalog**: Browse products by category with search functionality
- **Shopping Cart**: Add/remove items with quantity management
- **Order Management**: Complete checkout process with order confirmation
- **Admin Panel**: Product management (CRUD operations)
- **Responsive Design**: Modern UI with Bootstrap

## Prerequisites

- **.NET 9.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **SQL Server LocalDB** (included with Visual Studio or SQL Server Express)
- **Visual Studio 2022** or **Visual Studio Code**

## Getting Started

### 1. Clone or Download the Project

```bash
git clone <repository-url>
cd AgroProductEcommerce
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Update Database

The application uses Entity Framework Core with SQL Server LocalDB. Run the following commands to set up the database:

```bash
# Navigate to the project directory
cd AgroProductEcommerce

# Create and apply migrations
dotnet ef database update
```

### 4. Run the Application

```bash
dotnet run
```

The application will be available at:
- **Main Site**: https://localhost:7001 (or http://localhost:5001)
- **Admin Access**: Login with admin@clothingbrand.com / Admin@123

## Default Admin Account

- **Email**: admin@clothingbrand.com
- **Password**: Admin@123

## Project Structure

```
AgroProductEcommerce/
├── Controllers/          # MVC Controllers
├── Models/              # Data Models and DbContext
├── Views/               # Razor Views
├── Data/                # Database Initialization
├── Migrations/          # Entity Framework Migrations
├── wwwroot/            # Static Files (CSS, JS, Images)
└── Program.cs          # Application Entry Point
```

## Database Schema

The application includes the following main entities:
- **Users**: Customer accounts with authentication
- **Products**: Product catalog with categories
- **Orders**: Customer orders and order items
- **Cart Items**: Shopping cart functionality

## Configuration

### Connection String

The application is configured to use SQL Server LocalDB by default. You can modify the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AgroProductEcommerceDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### Environment Variables

For production deployment, consider using environment variables for sensitive configuration.

## Troubleshooting

### Common Issues

1. **Database Connection Error**
   - Ensure SQL Server LocalDB is installed
   - Run `dotnet ef database update` to create the database

2. **Port Already in Use**
   - The application uses ports 5001 (HTTP) and 7001 (HTTPS)
   - Modify `Properties/launchSettings.json` to change ports

3. **Build Errors**
   - Ensure .NET 9.0 SDK is installed
   - Run `dotnet restore` to restore packages

### Development Tips

- Use `dotnet watch run` for hot reload during development
- Check the console output for detailed error messages
- Use Visual Studio's debugging tools for step-through debugging

## Technologies Used

- **ASP.NET Core 9.0** - Web framework
- **Entity Framework Core** - ORM for database access
- **ASP.NET Core Identity** - Authentication and authorization
- **Bootstrap 5** - CSS framework for responsive design
- **jQuery** - JavaScript library for client-side functionality
- **SQL Server LocalDB** - Local database for development

## License

This project is for educational and demonstration purposes.

## Support

For issues or questions, please check the troubleshooting section above or create an issue in the repository. 