using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PickPointTest.DataProviders.DataModels
{
    public class PostautomatData
    {
        [Key] public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public bool IsOpen { get; set; }
        public List<OrderData> Orders { get; set; }
    }
}