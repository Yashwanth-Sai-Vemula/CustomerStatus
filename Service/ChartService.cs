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
            var ApplicationStatusColors = new Dictionary<int, string>
            {
                { 185, "#FF5733" },  // ApplicationReceived - Orange
                { 190, "#33FF57" },  // IncompleteApplication - Green
                { 195, "#3357FF" },  // LNVerificationFailed - Blue
                { 200, "#FF33A1" },  // LNVerified - Pink
                { 205, "#A133FF" },  // CallPending - Purple
                { 210, "#33FFA1" },  // WaitingForDocuments - Teal
                { 211, "#A1FF33" },  // WorkDetailsCompleted - Lime
                { 212, "#FFA133" },  // PersonalDetailsCompleted - Coral
                { 213, "#33A1FF" },  // IncomeDetailsCompleted - Sky Blue
                { 214, "#FF5733" },  // BankDetailsCompleted - Orange
                { 215, "#33FF57" },  // OnlineCustomerInSupervisorRequest - Green
                { 216, "#3357FF" },  // Archive - Blue
                { 217, "#FF33A1" },  // OnlineInReview - Pink
                { 218, "#A133FF" },  // OnlineInSupervisorReview - Purple
                { 219, "#33FFA1" },  // DocumentCompleted - Teal
                { 220, "#A1FF33" },  // AccountCreated - Lime
                { 230, "#FFA133" },  // NeedMoreInfo - Coral
                { 240, "#33A1FF" },  // SendToStore - Sky Blue
                { 250, "#FF5733" },  // VANeedMoreInfo - Orange
                { 260, "#33FF57" },  // LNAuthFailed - Green
                { 270, "#3357FF" },  // LNAuthSuccess - Blue
                { 280, "#FF33A1" },  // DebitCardDetailsCompleted - Pink
                { 290, "#A133FF" },  // InVerification - Purple
                { 300, "#33FFA1" },  // Declined - Teal
                { 310, "#A1FF33" },  // Withdrawn - Lime
                { 320, "#FFA133" },  // ApprovedPendingSignature - Coral
                { 330, "#33A1FF" },  // Originated - Sky Blue
                { 340, "#FF5733" },  // AutoWithdrawal - Orange
                { 350, "#33FF57" },  // LoanClose - Green
                { 501, "#3357FF" },  // FSVerificationAccepted - Blue
                { 502, "#FF33A1" },  // RBLVerificationAccepted - Pink
                { 503, "#A133FF" },  // RBLVerificationDeclined - Purple
                { 504, "#33FFA1" },  // RBLLoanInitiated - Teal
                { 505, "#A1FF33" },  // RBLAgreementsAccepted - Lime
                { 506, "#FFA133" },  // RBLLoanDisbursed - Coral
                { 507, "#33A1FF" }   // RBLLoanClosed - Sky Blue
            };
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
                                if (!ApplicationStatusColors.ContainsKey(ApplicationStatusCode))
                                {
                                    ApplicationStatusColors[ApplicationStatusCode] = "#000000"; // Default color (black)
                                }
                                // Add new Details if not present
                                customer.Details.Add(new Details
                                {
                                    CustomerStatusCode = CustomerStatusCode,
                                    ApplicationStatusCode = ApplicationStatusCode,
                                    InsertedDate = insertedDate,
                                    Color = ApplicationStatusColors[ApplicationStatusCode]
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
