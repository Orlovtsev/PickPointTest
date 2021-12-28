using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickPointTest.DataProviders.DataModels
{
    public class OrderData
    {
        [Key] public int Id { get; set; }
        public int StatusId { get; set; }
        [ForeignKey(nameof(StatusId))] public OrderStatusData OrderStatus { get; set; }
        public decimal Cost { get; set; }
        public int PostautomatId { get; set; }
        [ForeignKey(nameof(PostautomatId))] public PostautomatData Postautomat { get; set; }
        [MinLength(12)] [MaxLength(12)] public string RecipientPhone { get; set; }
        [MaxLength(100)] public string RecipientName { get; set; }
        [MaxLength(10)] public List<OrderProductData> Products { get; set; }
    }
}