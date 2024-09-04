namespace CustomerStatus.Model
{

    public class CombinedDataModel
    {
        public List<Customer> customerData { get; set; }
        public List<BarData> ApplicationData { get; set; }

    }
    public class Customer
    {
        public int CustomerId { get; set; }
        public List<Details> Details { get; set; }
    }
    public class Details
    {
        public int CustomerStatusCode { get; set; }
        public int ApplicationStatusCode { get; set; }
        public DateTime InsertedDate { get; set; }
        public string Color { get; set; }
    }
    public class BarData
    {
        public int ApplicationStatusCode { get; set; }
        public int Count { get; set; }
    }
}
