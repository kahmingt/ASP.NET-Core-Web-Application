using System.ComponentModel.DataAnnotations;


namespace WebApp.Models
{
    public class OrderDetailsListIndex
    {
        public string CustomerName { get; set; }

        public int OrderCount { get; set; }

        public DateTime OrderDate { get; set; }

        public int OrderID { get; set; }

        public decimal TotalCost { get; set; }
    }

    public class OrderModel
    {
        public IEnumerable<OrderDetailsModel> OrderDetailList { get; set; }

        public string CustomerAddress { get; set; }

        public string CustomerContactNumber { get; set; }

        [Required(ErrorMessage = "Please select Customer Name.")]
        public string CustomerID { get; set; }

        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Please select Employee Name.")]
        public int EmployeeID { get; set; }

        public string EmployeeName { get; set; }

        public int? OrderID { get; set; }

        [Required(ErrorMessage = "Please select Order Date.")]
        public DateTime OrderDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:c}")]
        public decimal OrderGrandTotal { get; set; }
    }

    public class OrderDetailsModel
    {
        [Required(ErrorMessage = "Please select Product Category.")]
        public int CategoryID { get; set; }

        public string CategoryName { get; set; }

        public decimal OrderTotalCost { get; set; }

        public int? OrderID { get; set; }

        public int OrderQuantity { get; set; }

        [Required(ErrorMessage = "Please select Product.")]
        public int ProductID { get; set; }

        public string ProductName { get; set; }

        public decimal ProductUnitPrice { get; set; }
    }

    public class NestedOrderModel
    {
        public OrderModel OrderModel { get; set; }

        public OrderDetailsModel OrderDetailsModel { get; set; }
    }


}
