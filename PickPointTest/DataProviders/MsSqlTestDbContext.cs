#define TEST

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PickPointTest.DataProviders.DataModels;

namespace PickPointTest.DataProviders
{
    public class MsSqlTestDbContext : DbContext
    {
        #region ContextData

        private DbSet<OrderData> _orderSet { get; set; }
        private DbSet<PostautomatData> _postautomatSet { get; set; }
        private DbSet<OrderStatusData> _statusSet { get; set; }
        private DbSet<OrderProductData> _orderProductSet { get; set; }
        private DbSet<ProductData> _productSet { get; set; }

        public MsSqlTestDbContext(DbContextOptions<MsSqlTestDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(@"Server=(localdb)\mssqllocaldb;Database=Test");
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderData>()
                .HasOne<PostautomatData>()
                .WithMany(p => p.Orders)
                .HasForeignKey(o => o.PostautomatId);


            modelBuilder.Entity<OrderProductData>()
                .HasOne<OrderData>()
                .WithMany(order => order.Products)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        #endregion


        public int Insert(OrderData data)
        {
            _orderSet.AddAsync(data);
            Entry(data).State = EntityState.Added;
            return SaveChanges(acceptAllChangesOnSuccess: true);
        }

        public int Update(OrderData data)
        {
            _orderSet.Update(data);
            Entry(data).State = EntityState.Modified;
            return SaveChanges(acceptAllChangesOnSuccess: true);
        }

        public int Delete(OrderData data)
        {
            var orderEntity = Entry(data).Entity;
            _orderSet.Remove(orderEntity);
            return SaveChanges(acceptAllChangesOnSuccess: true);
        }

        public int Delete(ICollection<OrderProductData> collection)
        {
            var entities = collection.Select(x => Entry(x).Entity);
            _orderProductSet.RemoveRange(entities);
            return SaveChanges(acceptAllChangesOnSuccess: true);
        }


        public async Task<OrderData> FindOrder(int number)
        {
            var order = await Task.Run(() => _orderSet
                .Include(o => o.OrderStatus)
                .Include(x => x.Postautomat)
                .FirstOrDefaultAsync(o => o.Id == number));
            if (order == null) return null;
            var taskProducts = await Task.Run(() => _orderProductSet
                .Include(p => p.Product));
            order.Products = taskProducts.AsEnumerable().Where(x => x.OrderId == number).ToList();

            return order;
        }

        public async Task<OrderStatusData> FindStatus(int id)
        {
            return await Task.Run(() => _statusSet.FirstOrDefaultAsync(status => status.Id == id));
        }

        public async Task<int> ChangeStatus(int id, int statusId)
        {
            var order = await FindOrder(id);
            if (order == null) throw new NotFoundDataException($"Object {nameof(OrderData)} not found");
            var status = await FindStatus(statusId);
            if (status == null) throw new NotFoundDataException($"Object {nameof(OrderStatusData)} not fount");
            order.OrderStatus = status;
            _orderSet.Update(order);
            return await Task.Run(() => SaveChangesAsync(acceptAllChangesOnSuccess: true));
        }

        public async Task<int> ChangeProducts(int id, string[] composition, decimal cost)
        {
            var order = await FindOrder(id);
            if (order == null) throw new NotFoundDataException($"Object {nameof(OrderData)} not found");
            var newComposition = GetProducts(composition);
            var toDeletingProducts = order.Products.Where(x => newComposition.All(y => x.ProductId != y.ID));
            if (toDeletingProducts.Any())
            {
                foreach (var orderProductData in toDeletingProducts)
                {
                    order.Products.Remove(orderProductData);
                }
                
            }
            var toAddProducts = newComposition.Where(x => order.Products.All(y => x.ID != y.ProductId));
            if (toAddProducts.Any())
            {
                var addedOrderProducts = toAddProducts.Select(x => new OrderProductData() {Order = order, Product = x})
                    .ToList();
                order.Products.AddRange(addedOrderProducts);
            }

            order.Cost = cost;
            return Update(order);
        }


        public IEnumerable<ProductData> GetProducts(string[] productComposition)
        {
            var products = _productSet.Where(p => productComposition.Contains(p.Name)).ToList();
            return products;
        }

        public async IAsyncEnumerable<PostautomatData> GetOpenedPostautomats()
        {
            try
            {
                var collection = _postautomatSet.Where(x => x.IsOpen).OrderBy(x => x.Name).AsAsyncEnumerable();
                await foreach (var item in collection)
                {
                    yield return item;
                }
            }
            finally
            {
            }
        }

        public async Task<PostautomatData> FindPostautomat(string number)
        {
            return await Task.Run(
                () => _postautomatSet.FirstOrDefaultAsync(p => p.Name == number));
        }


#if TEST
        public void _addTestData()
        {
            var orderStatusCollection = new List<OrderStatusData>()
            {
                new OrderStatusData() {Id = 1, Description = "Зарегистрирован"},
                new OrderStatusData() {Id = 2, Description = "Принят на складе"},
                new OrderStatusData() {Id = 3, Description = "Выдан курьеру"},
                new OrderStatusData() {Id = 4, Description = "Доставлен в постамат"},
                new OrderStatusData() {Id = 5, Description = "Доставлен получателю"},
                new OrderStatusData() {Id = 6, Description = "Отменен"},
            };

            _statusSet.AddRange(orderStatusCollection);
            var products = new List<ProductData>()
            {
                new ProductData() {ID = 1, Name = "prod1", Amount = 10, Cost = 100},
                new ProductData() {ID = 2, Name = "prod2", Amount = 10, Cost = 100},
                new ProductData() {ID = 3, Name = "prod3", Amount = 10, Cost = 100},
                new ProductData() {ID = 4, Name = "prod4", Amount = 10, Cost = 100},
            };
            _productSet.AddRange(products);


            var collection = new PostautomatData[5]
            {
                new PostautomatData() {Id = 1, Name = "1111-1111", Address = "address", IsOpen = true},
                new PostautomatData() {Id = 2, Name = "1121-1111", Address = "address", IsOpen = false},
                new PostautomatData() {Id = 3, Name = "1111-1121", Address = "address", IsOpen = true},
                new PostautomatData() {Id = 4, Name = "1131-1111", Address = "address", IsOpen = false},
                new PostautomatData() {Id = 5, Name = "1141-1111", Address = "address", IsOpen = true},
            };
            _postautomatSet.AddRange(collection);

            SaveChanges();
            var orderProductDatas = new List<OrderProductData>()
            {
                new OrderProductData() {Id = 1, OrderId = 1, Product = products[0]},
            };
            _orderProductSet.AddRange(orderProductDatas);
            var order = new OrderData()
            {
                Id = 1,
                OrderStatus = orderStatusCollection[1],
                Postautomat = collection[0],
                Products = orderProductDatas,
                Cost = 100,
            };
            _orderSet.Add(order);
            SaveChanges();
        }
#endif
    }
}