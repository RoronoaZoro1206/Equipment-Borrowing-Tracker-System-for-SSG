using System.Data;
using System.Text;
using Npgsql;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportToCsvAsync(DataTable data);
        Task<byte[]> ExportOverdueReportAsync();
        Task<byte[]> ExportEquipmentUtilizationAsync();
        Task<byte[]> ExportStudentHistoryAsync(int studentId);
        Task<byte[]> ExportAuditLogAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<byte[]> ExportInventoryReportAsync();
    }

    public class ExportService : IExportService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<ExportService> _logger;

        public ExportService(DatabaseHelper db, ILogger<ExportService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public Task<byte[]> ExportToCsvAsync(DataTable data)
        {
            var csv = new StringBuilder();
            var headers = data.Columns.Cast<DataColumn>().Select(c => EscapeCsvField(c.ColumnName));
            csv.AppendLine(string.Join(",", headers));

            foreach (DataRow row in data.Rows)
            {
                var fields = row.ItemArray.Select(f => EscapeCsvField(f?.ToString() ?? ""));
                csv.AppendLine(string.Join(",", fields));
            }

            return Task.FromResult(Encoding.UTF8.GetBytes(csv.ToString()));
        }

        public async Task<byte[]> ExportOverdueReportAsync()
        {
            var query = @"
                SELECT 
                    EXTRACT(DAY FROM (NOW() - b.borrow_due_date))::INT as ""Days Overdue"",
                    s.student_id_number as ""Student ID"",
                    CONCAT(s.student_first_name, ' ', s.student_last_name) as ""Student Name"",
                    s.student_college as ""College"",
                    s.student_email as ""Email"",
                    e.equipment_name as ""Equipment"",
                    eu.eunit_serial_no as ""Serial No"",
                    b.borrow_due_date as ""Due Date"",
                    t.transaction_code as ""Transaction Code""
                FROM borrow_record b
                JOIN transaction t ON b.transaction_id = t.transaction_id
                JOIN student s ON t.student_id = s.student_id
                JOIN equipment_unit eu ON b.eunit_id = eu.eunit_id
                JOIN equipment e ON eu.equipment_id = e.equipment_id
                WHERE b.borrow_status IN ('borrowed', 'overdue') AND b.borrow_due_date < NOW()
                ORDER BY b.borrow_due_date ASC";

            var dt = await _db.ExecuteQueryAsync(query);
            return await ExportToCsvAsync(dt);
        }

        public async Task<byte[]> ExportEquipmentUtilizationAsync()
        {
            var query = @"
                SELECT 
                    e.equipment_name as ""Equipment Name"",
                    e.equipment_category as ""Category"",
                    COUNT(DISTINCT eu.eunit_id) as ""Total Units"",
                    COUNT(DISTINCT CASE WHEN eu.eunit_status = 'available' THEN eu.eunit_id END) as ""Available"",
                    COUNT(DISTINCT CASE WHEN eu.eunit_status = 'borrowed' THEN eu.eunit_id END) as ""Borrowed"",
                    COUNT(b.borrow_id) as ""Total Borrows""
                FROM equipment e
                LEFT JOIN equipment_unit eu ON e.equipment_id = eu.equipment_id
                LEFT JOIN borrow_record b ON eu.eunit_id = b.eunit_id
                GROUP BY e.equipment_id, e.equipment_name, e.equipment_category
                ORDER BY ""Total Borrows"" DESC";

            var dt = await _db.ExecuteQueryAsync(query);
            return await ExportToCsvAsync(dt);
        }

        public async Task<byte[]> ExportStudentHistoryAsync(int studentId)
        {
            var query = @"
                SELECT 
                    t.transaction_code as ""Transaction Code"",
                    e.equipment_name as ""Equipment"",
                    eu.eunit_serial_no as ""Serial No"",
                    t.transaction_date_borrowed as ""Date Borrowed"",
                    b.borrow_due_date as ""Due Date"",
                    b.borrow_date_returned as ""Date Returned"",
                    b.borrow_status as ""Status""
                FROM transaction t
                JOIN borrow_record b ON t.transaction_id = b.transaction_id
                JOIN equipment_unit eu ON b.eunit_id = eu.eunit_id
                JOIN equipment e ON eu.equipment_id = e.equipment_id
                WHERE t.student_id = @student_id
                ORDER BY t.transaction_date_borrowed DESC";

            var parameters = new[] { new NpgsqlParameter("@student_id", studentId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);
            return await ExportToCsvAsync(dt);
        }

        public async Task<byte[]> ExportAuditLogAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var whereClause = "";
            var parameters = new List<NpgsqlParameter>();

            if (fromDate.HasValue)
            {
                whereClause = " WHERE a.audit_logged_at >= @from_date";
                parameters.Add(new NpgsqlParameter("@from_date", fromDate.Value));
            }

            if (toDate.HasValue)
            {
                whereClause += fromDate.HasValue ? " AND" : " WHERE";
                whereClause += " a.audit_logged_at <= @to_date";
                parameters.Add(new NpgsqlParameter("@to_date", toDate.Value));
            }

            var query = $@"
                SELECT 
                    a.audit_logged_at as ""Timestamp"",
                    COALESCE(u.user_username, 'System') as ""User"",
                    a.audit_action as ""Action"",
                    a.audit_old_data as ""Old Value"",
                    a.audit_new_data as ""New Value""
                FROM audit_log a
                LEFT JOIN ""USER"" u ON a.user_id = u.user_id
                {whereClause}
                ORDER BY a.audit_logged_at DESC";

            var dt = await _db.ExecuteQueryAsync(query, parameters.ToArray());
            return await ExportToCsvAsync(dt);
        }

        public async Task<byte[]> ExportInventoryReportAsync()
        {
            var query = @"
                SELECT 
                    e.equipment_name as ""Equipment Name"",
                    e.equipment_category as ""Category"",
                    eu.eunit_serial_no as ""Serial Number"",
                    eu.eunit_status as ""Status"",
                    CASE WHEN eu.eunit_condition THEN 'Good' ELSE 'Damaged' END as ""Condition""
                FROM equipment e
                JOIN equipment_unit eu ON e.equipment_id = eu.equipment_id
                ORDER BY e.equipment_name, eu.eunit_serial_no";

            var dt = await _db.ExecuteQueryAsync(query);
            return await ExportToCsvAsync(dt);
        }

        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
                return $"\"{field.Replace("\"", "\"\"")}\"";
            return field;
        }
    }
}
