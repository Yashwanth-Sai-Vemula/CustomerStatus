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
      
       
        public async Task<List<Customer>> getData()
        {
            var (lastCustomerHistoryID, LastInsertedDate) = await GetLastData();
            var (earliestCustomerHistoryID, closestCustomerHistoryID) = await GetClosestHistoryIDAsync(lastCustomerHistoryID, LastInsertedDate);
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

            string query = @"
                SELECT CustomerID, CustomerStatusCodeID, ApplicationStatus, InsertedDate
                FROM CustomerHistory
                WHERE (CustomerHistoryID >= @StartId AND CustomerHistoryID <= @EndId)
                ORDER BY CustomerID, InsertedDate;
            ";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.CommandTimeout = 120;
                    command.Parameters.AddWithValue("@StartId", earliestCustomerHistoryID);
                    command.Parameters.AddWithValue("@EndId", lastCustomerHistoryID);
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
        public async Task<(int, DateTime)> GetLastData()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                SELECT TOP 1 CustomerHistoryID, InsertedDate
                FROM CustomerHistory
                ORDER BY CustomerHistoryID DESC";

                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int maxCustomerHistoryID = reader.GetInt32(0);
                        DateTime maxInsertedDate = reader.GetDateTime(1);
                        return (maxCustomerHistoryID, maxInsertedDate);
                    }
                }
            }
            return (0, DateTime.MinValue);
        }
        public async Task<(int,int)> GetClosestHistoryIDAsync(int maxCustomerHistoryID, DateTime lastInsertedDate)
        {
            int stepSize = 1000;

            int closestCustomerHistoryID = maxCustomerHistoryID;
            DateTime closestDate = lastInsertedDate;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                while (stepSize > 0)
                {
                    string query = @"
                    SELECT InsertedDate 
                    FROM CustomerHistory
                    WHERE CustomerHistoryID = @CustomerHistoryID";

                    DateTime currentDate;

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CustomerHistoryID", closestCustomerHistoryID);

                        object result = await command.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            currentDate = (DateTime)result;
                        }
                        else
                        {
                            throw new Exception("CustomerHistoryID not found.");
                        }
                    }

                    TimeSpan difference = currentDate - lastInsertedDate.AddHours(-1);
                    if (Math.Abs(difference.Seconds) <= 0)
                    {
                        break;
                    }
                    if (difference.TotalSeconds > 0)
                    {
                        closestCustomerHistoryID -= stepSize;
                    }
                    else if (difference.TotalSeconds < 0)
                    {
                        closestCustomerHistoryID += stepSize;
                    }
                    else
                    {
                        break;
                    }

                    if (Math.Abs(difference.TotalSeconds) < 600)
                    {
                        stepSize = Math.Max(stepSize / 2, 1); 
                    }
                }

                int earliestCustomerHistoryID = closestCustomerHistoryID - stepSize;
                int latestCustomerHistoryID = closestCustomerHistoryID + stepSize;

                return (earliestCustomerHistoryID, closestCustomerHistoryID);
            } 
        }
    }

}
