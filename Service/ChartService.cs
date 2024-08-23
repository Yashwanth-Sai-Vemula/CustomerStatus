using CustomerStatus.Model;
using System.Data.SqlClient;

namespace CustomerStatus.Service
{
    public class ChartService
    {
        private readonly string _connectionString;
        public ChartService(string connectionString)
        {
            _connectionString = connectionString;
        }
        public async Task<List<Customer>> getFirstandLastIDs()
        {
            var Ids = new List<int>();
            string query1 = @"SELECT
                (SELECT MIN([CustomerHistoryID])
                 FROM CustomerHistory
                 WHERE InsertedDate BETWEEN DATEADD(HOUR, -1, GETDATE()) AND GETDATE()) AS EarliestCustomerHistoryID,
                (SELECT MAX([CustomerHistoryID])
                 FROM CustomerHistory
                 WHERE InsertedDate BETWEEN DATEADD(HOUR, -1, GETDATE()) AND GETDATE()) AS LatestCustomerHistoryID;
            ";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand(query1,connection);
                connection.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Read the earliest CustomerHistoryID
                        if (!reader.IsDBNull(0))
                        {
                            Ids.Add(reader.GetInt32(0));
                        }

                        // Read the latest CustomerHistoryID
                        if (!reader.IsDBNull(1))
                        {
                            Ids.Add(reader.GetInt32(1));
                        }

                    }
                }
            }
           
            var CustomerIDs = await getCustomerIds(Ids);
            return await getData(CustomerIDs, Ids);
        } 
        public async Task<List<int>> getCustomerIds(List<int> Ids)
        {
            var customerIds = new List<int>();
            string query = @"
                    SELECT DISTINCT CustomerID
                    FROM CustomerHistory
                    WHERE CustomerHistoryID >= @StartId AND CustomerHistoryID <= @EndId;
            ";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StartId", Ids[0]);
                    command.Parameters.AddWithValue("@EndId", Ids[1]);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int customerId = reader.GetInt32(0);
                            customerIds.Add(customerId);
                        }
                    }
                };
            }
            foreach(var id in customerIds)
            {
                Console.WriteLine(id);
            }
            return customerIds;
        }
        public async Task<List<Customer>> getData(List<int> Data,List<int> Ids)
        {
            var ChartData = new List<Customer>();
            var customerIDs = string.Join(", ", Data);

            string query = @"
                SELECT CustomerID, CustomerStatusCodeID, ApplicationStatus, InsertedDate
                FROM CustomerHistory
                WHERE CustomerID IN (" + customerIDs + @")
                AND (CustomerHistoryID >= @StartId AND CustomerHistoryID <= @EndId)
                ORDER BY CustomerID, InsertedDate;
            ";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StartId", Ids[0]);
                    command.Parameters.AddWithValue("@EndId", Ids[1]);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int customerId = reader.GetInt32(0);
                            short CustomerStatusCode = reader.GetInt16(1);
                            short ApplicationStatusCode = reader.GetInt16(2);
                            var insertedDate = reader.GetDateTime(3);

                            // Find or create the Customer object
                            var customer = ChartData.Find(c => c.CustomerId == customerId);
                            if (customer == null)
                            {
                                customer = new Customer { CustomerId = customerId, Details = new List<Details>() };
                                ChartData.Add(customer);
                            }
                            var existingDetail = customer.Details
                                .FirstOrDefault(d => d.CustomerStatusCode == CustomerStatusCode && d.ApplicationStatusCode == ApplicationStatusCode);

                            if (existingDetail == null)
                            {
                                // Add new Details if not present
                                customer.Details.Add(new Details
                                {
                                    CustomerStatusCode = CustomerStatusCode,
                                    ApplicationStatusCode = ApplicationStatusCode,
                                    InsertedDate = insertedDate
                                });
                            }
                        }
                    }
                };
            }

            return ChartData;
        }
    }
}
