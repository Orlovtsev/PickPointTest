using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickPointTest.DataProviders.DataModels
{
    public class OrderProductData
    {
        [Key] public int Id { get; set; }
        public long ProductId { get; set; }
        [ForeignKey(nameof(ProductId))][Required] public ProductData Product { get; set; }
        public int OrderId { get; set; }
        [ForeignKey(nameof(OrderId))][Required] public OrderData Order { get; set; }
    }
}