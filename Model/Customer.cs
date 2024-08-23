namespace CustomerStatus.Model
{
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
    }
}
