using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Sovelluskehitys_2025.Models;

namespace Sovelluskehitys_2025.Data
{
    // Sovelluksen palvelukerros: käärii repositoriot ja muuntaa datan käyttöliittymälle.
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

        // Tuotteet.
        public DataTable GetProducts() => _products.GetProducts();
        public DataTable GetProductOptions() => _products.GetProductOptions();
        public void AddProduct(string name, decimal price, int stock, long? categoryId) =>
            _products.AddProduct(name, price, stock, categoryId);
        public void DeleteProduct(long id) => _products.DeleteProduct(id);
        public void UpdateProductCategory(long productId, long? categoryId) =>
            _products.UpdateProductCategory(productId, categoryId);
        public void UpdateProductDetails(long productId, string name, decimal price, long? categoryId) =>
            _products.UpdateProductDetails(productId, name, price, categoryId);
        public void AddStock(long productId, int amount) =>
            _products.AddStock(productId, amount);

        // Asiakkaat.
        public DataTable GetCustomers() => _customers.GetCustomers();
        public DataTable GetCustomerOptions() => _customers.GetCustomerOptions();
        public void AddCustomer(string name, string address, string phone) =>
            _customers.AddCustomer(name, address, phone);

        // Kategoriat.
        public DataTable GetCategories() => _categories.GetCategories();
        public DataTable GetCategoryOptions() => _categories.GetCategoryOptions();
        public void AddCategory(string name, string? description) =>
            _categories.AddCategory(name, description);
        public void DeleteCategory(long id) => _categories.DeleteCategory(id);

        // Tilaukset.
        public DataTable GetOpenOrders() => _orders.GetOpenOrders();
        public DataTable GetDeliveredOrders() => _orders.GetDeliveredOrders();
        public void CreateOrder(long customerId, long productId, int quantity) =>
            _orders.CreateOrder(customerId, productId, quantity);
        public void CreateOrderWithLines(long customerId, IReadOnlyList<(long productId, int quantity)> lines) =>
            _orders.CreateOrderWithLines(customerId, lines);
        public void SetDelivered(long orderId, bool delivered) =>
            _orders.SetDelivered(orderId, delivered);
        public void DeleteOrder(long orderId) => _orders.DeleteOrder(orderId);
        public void UpdateOrderQuantity(long rowId, long productId, int newQty, int oldQty) =>
            _orders.UpdateOrderQuantity(rowId, productId, newQty, oldQty);
        public void UpdateOrderQuantity(long rowId, int newQty) =>
            _orders.UpdateOrderQuantity(rowId, newQty);
        public DataTable GetTopProducts(int limit) => _orders.GetTopProducts(limit);

        // Pää- ja rivinäkymän muodostus avoimille tilauksille.
        public List<TilausNakyma> GetOpenOrdersHierarchical()
        {
            var table = _orders.GetOpenOrders();
            return MapOrders(table, delivered: false);
        }

        // Pää- ja rivinäkymän muodostus toimitetuille tilauksille.
        public List<TilausNakyma> GetDeliveredOrdersHierarchical()
        {
            var table = _orders.GetDeliveredOrders();
            return MapOrders(table, delivered: true);
        }

        // Muuntaa litteät SQL-rivit sisäkkäiseksi tilaus -> rivit -rakenteeksi.
        private static List<TilausNakyma> MapOrders(DataTable table, bool delivered)
        {
            var orders = new Dictionary<long, TilausNakyma>();

            foreach (DataRow row in table.Rows)
            {
                long id = Convert.ToInt64(row["id"]);
                if (!orders.TryGetValue(id, out var order))
                {
                    DateTime? pvm = null;
                    var rawPvm = Convert.ToString(row["tilaus_pvm"]);
                    if (!string.IsNullOrWhiteSpace(rawPvm)
                        && DateTime.TryParse(rawPvm, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
                        pvm = parsed;

                    DateTime? toimitusPvm = null;
                    var rawToimitusPvm = Convert.ToString(row["toimitus_pvm"]);
                    if (!string.IsNullOrWhiteSpace(rawToimitusPvm)
                        && DateTime.TryParse(rawToimitusPvm, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedToimitus))
                        toimitusPvm = parsedToimitus;

                    // Ensimmäinen rivi tälle tilaukselle -> luo otsikkotiedot.
                    order = new TilausNakyma
                    {
                        Id = id,
                        AsiakasNimi = Convert.ToString(row["asiakas"]) ?? "",
                        Osoite = Convert.ToString(row["osoite"]) ?? "",
                        TilausPvm = pvm,
                        ToimitusPvm = toimitusPvm,
                        Toimitettu = delivered
                    };
                    orders.Add(id, order);
                }

                // Jokainen rivi muodostaa tilauksen rivin.
                var line = new TilausRiviNakyma
                {
                    RiviId = Convert.ToInt64(row["rivi_id"]),
                    TuoteId = Convert.ToInt64(row["tuote_id"]),
                    TuoteNimi = Convert.ToString(row["tuote"]) ?? "",
                    Maara = Convert.ToInt32(row["maara"]),
                    Rivihinta = Convert.ToDecimal(row["rivihinta"])
                };
                order.Rivit.Add(line);
            }

            foreach (var order in orders.Values)
            {
                // Laske rivien summat otsikkonäkymään.
                order.Yhteensa = order.Rivit.Sum(r => r.Rivihinta);
            }

            return orders.Values.OrderByDescending(o => o.Id).ToList();
        }
    }
}
