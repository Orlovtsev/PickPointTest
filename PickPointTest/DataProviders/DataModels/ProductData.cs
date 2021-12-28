using System.ComponentModel.DataAnnotations;

namespace PickPointTest.DataProviders.DataModels
{
    public class ProductData
    {
        [Key] public long ID { get; set; }
        public string Name { get; set; }
        public int Amount { get; set; }
        public decimal Cost { get; set; } //TODO: не тот тип?
    }
}