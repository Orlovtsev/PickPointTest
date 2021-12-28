using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PickPointTest.Models
{
    public class PostautomatJSON
    {
        public string number { get; set; }
        public string address { get; set; }
        public bool status { get; set; }

        public static bool IsValidNumber(string number)
        {
            var regexp = "^[0-9]{4}-[0-9]{4}";
            //TODO: Уточнить, в номере должны использоваться только цифры?
            var reg = new Regex(regexp);
            return number.Length == 9 && reg.IsMatch(number);
        }
    }
}