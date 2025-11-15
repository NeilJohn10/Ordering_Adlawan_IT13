using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Ordering_Adlawan_IT13.Services
{
    /// <summary>
    /// Database Initialization Service
    /// Ensures database and tables exist before application runs
    /// </summary>
    public class DatabaseInitializationService
    {
        private readonly string _masterConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=DB_Ordering_Adlawan_IT13;Integrated Security=True;Trust Server Certificate=True";
        
        private readonly string _connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=DB_Ordering_Adlawan_IT13;Integrated Security=True;Trust Server Certificate=True";

     /// <summary>
      /// Initialize database with tables and sample data
        /// </summary>
        public async Task InitializeDatabaseAsync()
  {
    try
    {
          // Step 1: Create database if it doesn't exist
   await CreateDatabaseIfNotExistsAsync();
            Console.WriteLine("✓ Database checked/created");

    // Step 2: Create tables
 await CreateTablesAsync();
 Console.WriteLine("✓ Tables checked/created");

    // Step 3: Insert sample data
           await InsertSampleDataAsync();
  Console.WriteLine("✓ Sample data verified");

     Console.WriteLine("\n✅ Database initialization complete!");
       }
      catch (Exception ex)
 {
  Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
    throw;
        }
        }

        /// <summary>
     /// Create database if it doesn't exist
      /// </summary>
        private async Task CreateDatabaseIfNotExistsAsync()
        {
      using var conn = new SqlConnection(_masterConnectionString);
     await conn.OpenAsync();

  string checkDbSql = @"
   IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'DB_Ordering_Adlawan_IT13')
            BEGIN
 CREATE DATABASE [DB_Ordering_Adlawan_IT13];
           END";

            using var cmd = new SqlCommand(checkDbSql, conn);
       cmd.CommandTimeout = 60;
            await cmd.ExecuteNonQueryAsync();
    }

        /// <summary>
 /// Create tables if they don't exist
        /// </summary>
        private async Task CreateTablesAsync()
        {
            using var conn = new SqlConnection(_connectionString);
     await conn.OpenAsync();

        // Create FoodItems table
            string foodItemsTableSql = @"
       IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FoodItems' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
         CREATE TABLE [dbo].[FoodItems]
     (
               [ItemId] INT PRIMARY KEY IDENTITY(1,1),
  [ItemName] NVARCHAR(100) NOT NULL,
  [Description] NVARCHAR(MAX) NULL,
     [Price] DECIMAL(10, 2) NOT NULL,
     [Quantity] INT NOT NULL,
        [Category] NVARCHAR(50) NULL,
       [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE()
     );
        
           CREATE INDEX idx_ItemName ON [dbo].[FoodItems]([ItemName]);
   END";

       using var cmd1 = new SqlCommand(foodItemsTableSql, conn);
            cmd1.CommandTimeout = 60;
         await cmd1.ExecuteNonQueryAsync();

          // Create Orders table
            string ordersTableSql = @"
          IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
        CREATE TABLE [dbo].[Orders]
      (
   [OrderId] INT PRIMARY KEY IDENTITY(1,1),
     [CustomerName] NVARCHAR(100) NOT NULL,
               [ItemId] INT NOT NULL,
        [ItemName] NVARCHAR(100) NOT NULL,
        [OrderQuantity] INT NOT NULL,
       [UnitPrice] DECIMAL(10, 2) NOT NULL,
           [TotalPrice] DECIMAL(10, 2) NOT NULL,
             [Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending',
            [OrderDate] DATETIME NOT NULL DEFAULT GETDATE(),
           [Notes] NVARCHAR(MAX) NULL,
      CONSTRAINT FK_Orders_FoodItems FOREIGN KEY ([ItemId]) 
      REFERENCES [dbo].[FoodItems]([ItemId])
      );
   
       CREATE INDEX idx_Status ON [dbo].[Orders]([Status]);
   CREATE INDEX idx_OrderDate ON [dbo].[Orders]([OrderDate]);
  END";

  using var cmd2 = new SqlCommand(ordersTableSql, conn);
            cmd2.CommandTimeout = 60;
    await cmd2.ExecuteNonQueryAsync();
     }

        /// <summary>
        /// Insert sample data if tables are empty
        /// </summary>
        private async Task InsertSampleDataAsync()
        {
   using var conn = new SqlConnection(_connectionString);
         await conn.OpenAsync();

    // Check if data already exists
     string checkSql = "SELECT COUNT(*) FROM [dbo].[FoodItems]";
        using var checkCmd = new SqlCommand(checkSql, conn);
    int itemCount = (int)await checkCmd.ExecuteScalarAsync();

        if (itemCount > 0)
    {
          return; // Data already exists
            }

    // Insert sample data
            string insertSql = @"
     INSERT INTO [dbo].[FoodItems] ([ItemName], [Description], [Price], [Quantity], [Category], [CreatedDate])
 VALUES 
       (N'Fried Chicken', N'Crispy fried chicken breast', 150.00, 50, N'Main Course', GETDATE()),
              (N'Spaghetti', N'Creamy spaghetti carbonara', 120.00, 30, N'Pasta', GETDATE()),
(N'Pizza Margherita', N'Classic pizza with fresh tomatoes and basil', 250.00, 20, N'Pizza', GETDATE()),
(N'Burger', N'Juicy beef burger with cheese', 100.00, 40, N'Sandwich', GETDATE()),
      (N'Caesar Salad', N'Fresh Caesar salad with croutons', 80.00, 25, N'Salad', GETDATE())";

            using var insertCmd = new SqlCommand(insertSql, conn);
     insertCmd.CommandTimeout = 60;
            await insertCmd.ExecuteNonQueryAsync();
   }

      /// <summary>
        /// Test database connection
     /// </summary>
public async Task<bool> TestConnectionAsync()
        {
    try
    {
       using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
  
      using var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo')", conn);
           int tableCount = (int)await cmd.ExecuteScalarAsync();
    
            Console.WriteLine($"✓ Connection successful! Found {tableCount} tables in dbo schema");
         return true;
          }
 catch (Exception ex)
   {
       Console.WriteLine($"❌ Connection failed: {ex.Message}");
     return false;
         }
        }
    }
}
