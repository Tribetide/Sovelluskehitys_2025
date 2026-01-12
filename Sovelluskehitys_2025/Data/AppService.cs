using Microsoft.Data.Sqlite;
using System.Data;

namespace Sovelluskehitys_2025.Data
{
    public class AppService
    {
        private readonly ProductRepository _products;
        private readonly CustomerRepository _customers;
        private readonly CategoryRepository _categories;
        private readonly OrderRepository _orders;

        public AppService(SqliteConnection connection)
        {
            _products = new ProductRepository(connection);
            _customers = new CustomerRepository(connection);
            _categories = new CategoryRepository(connection);
            _orders = new OrderRepository(connection);
        }

        public DataTable GetProducts() => _products.GetProducts();
        public DataTable GetProductOptions() => _products.GetProductOptions();
        public void AddProduct(string name, decimal price, int stock, long? categoryId) =>
            _products.AddProduct(name, price, stock, categoryId);
        public void DeleteProduct(long id) => _products.DeleteProduct(id);
        public void UpdateProductCategory(long productId, long? categoryId) =>
            _products.UpdateProductCategory(productId, categoryId);

        public DataTable GetCustomers() => _customers.GetCustomers();
        public DataTable GetCustomerOptions() => _customers.GetCustomerOptions();
        public void AddCustomer(string name, string address, string phone) =>
            _customers.AddCustomer(name, address, phone);

        public DataTable GetCategories() => _categories.GetCategories();
        public DataTable GetCategoryOptions() => _categories.GetCategoryOptions();
        public void AddCategory(string name, string? description) =>
            _categories.AddCategory(name, description);
        public void DeleteCategory(long id) => _categories.DeleteCategory(id);

        public DataTable GetOpenOrders() => _orders.GetOpenOrders();
        public DataTable GetDeliveredOrders() => _orders.GetDeliveredOrders();
        public void CreateOrder(long customerId, long productId, int quantity) =>
            _orders.CreateOrder(customerId, productId, quantity);
        public void SetDelivered(long orderId, bool delivered) =>
            _orders.SetDelivered(orderId, delivered);
        public void DeleteOrder(long orderId) => _orders.DeleteOrder(orderId);
        public void UpdateOrderQuantity(long rowId, long productId, int newQty, int oldQty) =>
            _orders.UpdateOrderQuantity(rowId, productId, newQty, oldQty);
    }
}
