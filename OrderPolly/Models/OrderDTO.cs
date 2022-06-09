using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderPolly.Models
{
    public class OrderDTO
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public List<ItemDTO> ItemList { get; set; }
    }
}
