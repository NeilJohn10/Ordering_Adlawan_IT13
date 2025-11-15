using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Ordering_Adlawan_IT13.Models;

namespace Ordering_Adlawan_IT13.Services
{
    public class OrderingService
    {
        private readonly string _connectionString =
    @"Server=(localdb)\mssqllocaldb;Database=DB_Ordering_Adlawan_IT13;Trusted_Connection=True;TrustServerCertificate=True;";

        // -------------- DASHBOARD (nauna na ito) --------------
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var stats = new DashboardStats();

            using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    string query = @"
       SELECT
        (SELECT COUNT(*) FROM dbo.FoodItems) AS TotalItems,
                    (SELECT COUNT(*) FROM dbo.Orders) AS TotalOrders,
          (SELECT COUNT(*) FROM dbo.Orders WHERE Status = 'Pending') AS PendingOrders,
         (SELECT ISNULL(SUM(TotalPrice), 0)
            FROM dbo.Orders
 WHERE Status = 'Completed') AS TotalRevenue;";

            using var cmd = new SqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
   {
 stats.TotalItems = reader.GetInt32(0);
                stats.TotalOrders = reader.GetInt32(1);
     stats.PendingOrders = reader.GetInt32(2);
   stats.TotalRevenue = reader.GetDecimal(3);
            }
            return stats;
        }

        // -------------- FOOD ITEMS CRUD --------------

    public async Task<List<FoodItem>> GetFoodItemsAsync()
      {
       var list = new List<FoodItem>();

         using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"SELECT ItemId, ItemName, Description, Price, Quantity, Category, CreatedDate
           FROM dbo.FoodItems
      ORDER BY ItemName";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
          while (await reader.ReadAsync())
      {
    list.Add(new FoodItem
                {
              ItemId = reader.GetInt32(0),
                ItemName = reader.GetString(1),
    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
   Price = reader.GetDecimal(3),
     Quantity = reader.GetInt32(4),
    Category = reader.IsDBNull(5) ? null : reader.GetString(5),
     CreatedDate = reader.GetDateTime(6)
      });
            }
      return list;
     }

    public async Task AddFoodItemAsync(FoodItem item)
     {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

     string sql = @"
 INSERT INTO dbo.FoodItems (ItemName, Description, Price, Quantity, Category, CreatedDate)
           VALUES (@ItemName, @Description, @Price, @Quantity, @Category, GETDATE());";

      using var cmd = new SqlCommand(sql, conn);
      cmd.Parameters.AddWithValue("@ItemName", item.ItemName);
    cmd.Parameters.AddWithValue("@Description", (object?)item.Description ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@Price", item.Price);
  cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
  cmd.Parameters.AddWithValue("@Category", (object?)item.Category ?? DBNull.Value);

    try
    {
  int rowsAffected = await cmd.ExecuteNonQueryAsync();
   Console.WriteLine($"✓ Food item '{item.ItemName}' added successfully. Rows affected: {rowsAffected}");
    }
      catch (Exception ex)
  {
   Console.WriteLine($"❌ Error adding food item: {ex.Message}");
          throw;
        }
        }

     public async Task UpdateFoodItemAsync(FoodItem item)
  {
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

            string sql = @"
        UPDATE dbo.FoodItems
     SET ItemName = @ItemName,
     Description = @Description,
   Price = @Price,
         Quantity = @Quantity,
         Category = @Category
            WHERE ItemId = @ItemId;";

         using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ItemId", item.ItemId);
     cmd.Parameters.AddWithValue("@ItemName", item.ItemName);
         cmd.Parameters.AddWithValue("@Description", (object?)item.Description ?? DBNull.Value);
         cmd.Parameters.AddWithValue("@Price", item.Price);
     cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
            cmd.Parameters.AddWithValue("@Category", (object?)item.Category ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
   }

   public async Task DeleteFoodItemAsync(int itemId)
   {
      using var conn = new SqlConnection(_connectionString);
       await conn.OpenAsync();

            string sql = "DELETE FROM dbo.FoodItems WHERE ItemId = @ItemId;";
            using var cmd = new SqlCommand(sql, conn);
     cmd.Parameters.AddWithValue("@ItemId", itemId);

        await cmd.ExecuteNonQueryAsync();
        }

      // -------------- ORDERS CRUD WITH TRANSACTIONS --------------

    public async Task<List<Order>> GetOrdersAsync()
        {
            var list = new List<Order>();

            using var conn = new SqlConnection(_connectionString);
  await conn.OpenAsync();

    string sql = @"SELECT OrderId, CustomerName, ItemId, ItemName,
       OrderQuantity, UnitPrice, TotalPrice,
        Status, OrderDate, Notes
      FROM dbo.Orders
               ORDER BY OrderDate DESC;";

    using var cmd = new SqlCommand(sql, conn);
using var reader = await cmd.ExecuteReaderAsync();
 while (await reader.ReadAsync())
        {
             list.Add(new Order
{
   OrderId = reader.GetInt32(0),
      CustomerName = reader.GetString(1),
        ItemId = reader.GetInt32(2),
         ItemName = reader.GetString(3),
               OrderQuantity = reader.GetInt32(4),
 UnitPrice = reader.GetDecimal(5),
           TotalPrice = reader.GetDecimal(6),
       Status = reader.GetString(7),
      OrderDate = reader.GetDateTime(8),
          Notes = reader.IsDBNull(9) ? null : reader.GetString(9)
    });
            }

            return list;
   }

        public async Task AddOrderAsync(Order order)
   {
        using var conn = new SqlConnection(_connectionString);
      await conn.OpenAsync();

  using var transaction = conn.BeginTransaction();
   try
       {
       // Step 1: Check if the food item exists and has sufficient quantity
         string checkItemSql = @"SELECT Quantity FROM dbo.FoodItems WHERE ItemId = @ItemId;";
           using var checkCmd = new SqlCommand(checkItemSql, conn, transaction);
    checkCmd.Parameters.AddWithValue("@ItemId", order.ItemId);
     
            var result = await checkCmd.ExecuteScalarAsync();
          if (result == null)
    {
   throw new InvalidOperationException($"Food item with ID {order.ItemId} does not exist.");
       }

           int currentQuantity = (int)result;
  if (currentQuantity < order.OrderQuantity)
                {
   throw new InvalidOperationException($"Insufficient quantity. Available: {currentQuantity}, Requested: {order.OrderQuantity}");
          }

 // Step 2: Insert the order
                string insertOrderSql = @"
  INSERT INTO dbo.Orders
             (CustomerName, ItemId, ItemName, OrderQuantity,
   UnitPrice, TotalPrice, Status, OrderDate, Notes)
   VALUES
      (@CustomerName, @ItemId, @ItemName, @OrderQuantity,
       @UnitPrice, @TotalPrice, @Status, @OrderDate, @Notes);";

   using var insertCmd = new SqlCommand(insertOrderSql, conn, transaction);
      insertCmd.Parameters.AddWithValue("@CustomerName", order.CustomerName);
  insertCmd.Parameters.AddWithValue("@ItemId", order.ItemId);
        insertCmd.Parameters.AddWithValue("@ItemName", order.ItemName);
                insertCmd.Parameters.AddWithValue("@OrderQuantity", order.OrderQuantity);
   insertCmd.Parameters.AddWithValue("@UnitPrice", order.UnitPrice);
             insertCmd.Parameters.AddWithValue("@TotalPrice", order.TotalPrice);
       insertCmd.Parameters.AddWithValue("@Status", order.Status);
          insertCmd.Parameters.AddWithValue("@OrderDate", order.OrderDate);
            insertCmd.Parameters.AddWithValue("@Notes", (object?)order.Notes ?? DBNull.Value);

      await insertCmd.ExecuteNonQueryAsync();

     // Step 3: Update the food item quantity
          string updateItemSql = @"
               UPDATE dbo.FoodItems 
 SET Quantity = Quantity - @OrderQuantity 
 WHERE ItemId = @ItemId;";

   using var updateCmd = new SqlCommand(updateItemSql, conn, transaction);
   updateCmd.Parameters.AddWithValue("@OrderQuantity", order.OrderQuantity);
 updateCmd.Parameters.AddWithValue("@ItemId", order.ItemId);

    await updateCmd.ExecuteNonQueryAsync();

           // Commit the transaction
    await transaction.CommitAsync();
   
    Console.WriteLine($"✓ Order created successfully for customer '{order.CustomerName}'. Total: ₱{order.TotalPrice}");
 }
       catch (Exception ex)
  {
 await transaction.RollbackAsync();
    Console.WriteLine($"❌ Order creation failed: {ex.Message}");
                throw new Exception($"Transaction failed: {ex.Message}", ex);
      }
        }

     public async Task UpdateOrderStatusAsync(int orderId, string status)
        {
  using var conn = new SqlConnection(_connectionString);
   await conn.OpenAsync();

          string sql = @"UPDATE dbo.Orders SET Status = @Status WHERE OrderId = @OrderId;";
            using var cmd = new SqlCommand(sql, conn);
          cmd.Parameters.AddWithValue("@Status", status);
   cmd.Parameters.AddWithValue("@OrderId", orderId);

            await cmd.ExecuteNonQueryAsync();
        }

public async Task DeleteOrderAsync(int orderId)
      {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();
       try
     {
     // Get order details first
    string selectSql = @"SELECT ItemId, OrderQuantity FROM dbo.Orders WHERE OrderId = @OrderId;";
                using var selectCmd = new SqlCommand(selectSql, conn, transaction);
  selectCmd.Parameters.AddWithValue("@OrderId", orderId);
     
                using var reader = await selectCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
             {
            throw new InvalidOperationException($"Order with ID {orderId} not found.");
       }

                int itemId = reader.GetInt32(0);
  int orderQuantity = reader.GetInt32(1);
         await reader.CloseAsync();

             // Delete the order
                string deleteSql = @"DELETE FROM dbo.Orders WHERE OrderId = @OrderId;";
     using var deleteCmd = new SqlCommand(deleteSql, conn, transaction);
deleteCmd.Parameters.AddWithValue("@OrderId", orderId);
                await deleteCmd.ExecuteNonQueryAsync();

        // Restore the food item quantity
    string restoreSql = @"
       UPDATE dbo.FoodItems 
         SET Quantity = Quantity + @OrderQuantity 
      WHERE ItemId = @ItemId;";

       using var restoreCmd = new SqlCommand(restoreSql, conn, transaction);
           restoreCmd.Parameters.AddWithValue("@OrderQuantity", orderQuantity);
  restoreCmd.Parameters.AddWithValue("@ItemId", itemId);
await restoreCmd.ExecuteNonQueryAsync();

        await transaction.CommitAsync();
  }
            catch (Exception ex)
    {
    await transaction.RollbackAsync();
      throw new Exception($"Transaction failed: {ex.Message}", ex);
 }
        }
    }
}
