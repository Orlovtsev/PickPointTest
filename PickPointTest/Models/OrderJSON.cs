using System.Text.RegularExpressions;

namespace PickPointTest.Models
{
    /// <summary>
    /// ##  OrderBody
    /// 
    /// </summary>
    public class OrderJSON
    {
        #region Properties
        public int number { get; set; }
        public int status { get; set; }
        public string[] composition { get; set; }
        public decimal cost { get; set; }
        public string postautomat { get; set; }
        public string phone { get; set; }
        public string recipient { get; set; }

        #endregion


        #region Methods

        public bool IsValidNumber(out string error)
        {
            error = "Phone number format: +7XXXXXXXXXX";
            var regexp = @"^\+7([0-9]{10})";
            var regex = new Regex(regexp);
            if (string.IsNullOrWhiteSpace(phone)) return false;
            return regex.IsMatch(this.phone) && this.phone.Length == 12;
        }

        #endregion
    }
}