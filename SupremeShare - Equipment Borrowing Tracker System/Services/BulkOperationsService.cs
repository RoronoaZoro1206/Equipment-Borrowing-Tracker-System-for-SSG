using Npgsql;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Services
{
    public interface IBulkOperationsService
    {
        Task<BulkOperationResult> BulkBorrowAsync(BulkBorrowRequest request);
        Task<BulkOperationResult> BulkReturnAsync(BulkReturnRequest request);
        Task<BulkOperationResult> BulkUpdateStatusAsync(BulkStatusUpdateRequest request);
    }

    public class BulkOperationsService : IBulkOperationsService
    {
        private readonly DatabaseHelper _db;
        private readonly IStudentRepository _studentRepo;
        private readonly IEquipmentUnitRepository _unitRepo;
        private readonly IAuditService _auditService;
        private readonly IDashboardNotificationService _notificationService;
        private readonly ILogger<BulkOperationsService> _logger;

        public BulkOperationsService(
            DatabaseHelper db,
            IStudentRepository studentRepo,
            IEquipmentUnitRepository unitRepo,
            IAuditService auditService,
            IDashboardNotificationService notificationService,
            ILogger<BulkOperationsService> logger)
        {
            _db = db;
            _studentRepo = studentRepo;
            _unitRepo = unitRepo;
            _auditService = auditService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<BulkOperationResult> BulkBorrowAsync(BulkBorrowRequest request)
        {
            var result = new BulkOperationResult { OperationType = "Bulk Borrow" };
            
            try
            {
                foreach (var item in request.Items)
                {
                    try
                    {
                        // Support both StudentId (int) and StudentIdNumber (string)
                        Models.Student? student = null;
                        if (item.StudentId > 0)
                        {
                            student = await _studentRepo.GetByIdAsync(item.StudentId);
                        }
                        else if (!string.IsNullOrEmpty(item.StudentIdNumber))
                        {
                            student = await _studentRepo.GetByIdNumberAsync(item.StudentIdNumber);
                        }

                        if (student == null || !student.StudentStatus)
                        {
                            result.Failures.Add(new BulkOperationFailure
                            {
                                ItemId = item.StudentIdNumber ?? item.StudentId.ToString(),
                                Reason = "Student not found or blocked"
                            });
                            continue;
                        }

                        var unit = await _unitRepo.GetByIdAsync(item.UnitId);
                        if (unit == null || unit.EunitStatus != "available")
                        {
                            result.Failures.Add(new BulkOperationFailure
                            {
                                ItemId = item.UnitId.ToString(),
                                Reason = "Unit not available"
                            });
                            continue;
                        }

                        var query = "CALL sp_process_borrow_by_unit(@p_student_id, @p_user_id, @p_eunit_id, @p_due_date, NULL, NULL)";
                        var parameters = new NpgsqlParameter[]
                        {
                            new("@p_student_id", student.StudentId),
                            new("@p_user_id", request.UserId),
                            new("@p_eunit_id", item.UnitId),
                            new("@p_due_date", item.DueDate)
                        };

                        await _db.ExecuteNonQueryAsync(query, parameters);
                        result.SuccessCount++;
                        
                        result.SuccessItems.Add(new BulkOperationSuccess
                        {
                            ItemId = $"{student.StudentIdNumber}-{item.UnitId}",
                            Description = $"Student {student.StudentIdNumber} borrowed unit {unit.EunitSerialNo}"
                        });
                    }
                    catch (Exception ex)
                    {
                        result.Failures.Add(new BulkOperationFailure
                        {
                            ItemId = $"{item.StudentIdNumber ?? item.StudentId.ToString()}-{item.UnitId}",
                            Reason = ex.Message
                        });
                    }
                }

                await _auditService.LogActionAsync(request.UserId, "BULK_BORROW", null, 
                    new { EventName = request.EventName, SuccessCount = result.SuccessCount, FailureCount = result.Failures.Count });
                
                await _notificationService.NotifyStatsUpdated();
                
                _logger.LogInformation("Bulk borrow completed: {Success} success, {Failures} failures", 
                    result.SuccessCount, result.Failures.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk borrow operation");
                result.Failures.Add(new BulkOperationFailure { ItemId = "SYSTEM", Reason = ex.Message });
            }

            return result;
        }

        public async Task<BulkOperationResult> BulkReturnAsync(BulkReturnRequest request)
        {
            var result = new BulkOperationResult { OperationType = "Bulk Return" };
            
            try
            {
                foreach (var borrowId in request.BorrowIds)
                {
                    try
                    {
                        var query = "CALL sp_process_return_boolean(@p_borrow_id, @p_return_condition, @p_user_id)";
                        var parameters = new NpgsqlParameter[]
                        {
                            new("@p_borrow_id", borrowId),
                            new("@p_return_condition", request.DefaultCondition),
                            new("@p_user_id", request.UserId)
                        };

                        await _db.ExecuteNonQueryAsync(query, parameters);
                        result.SuccessCount++;
                        
                        result.SuccessItems.Add(new BulkOperationSuccess
                        {
                            ItemId = borrowId.ToString(),
                            Description = $"Borrow record {borrowId} returned"
                        });
                    }
                    catch (Exception ex)
                    {
                        result.Failures.Add(new BulkOperationFailure
                        {
                            ItemId = borrowId.ToString(),
                            Reason = ex.Message
                        });
                    }
                }

                await _auditService.LogActionAsync(request.UserId, "BULK_RETURN", null, 
                    new { EventName = request.EventName, SuccessCount = result.SuccessCount, FailureCount = result.Failures.Count });
                
                await _notificationService.NotifyStatsUpdated();
                
                _logger.LogInformation("Bulk return completed: {Success} success, {Failures} failures", 
                    result.SuccessCount, result.Failures.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk return operation");
                result.Failures.Add(new BulkOperationFailure { ItemId = "SYSTEM", Reason = ex.Message });
            }

            return result;
        }

        public async Task<BulkOperationResult> BulkUpdateStatusAsync(BulkStatusUpdateRequest request)
        {
            var result = new BulkOperationResult { OperationType = "Bulk Status Update" };
            
            try
            {
                foreach (var unitId in request.UnitIds)
                {
                    try
                    {
                        var query = @"
                            UPDATE equipment_unit 
                            SET eunit_status = @status, eunit_last_updated = NOW()
                            WHERE eunit_id = @unit_id AND eunit_status != 'borrowed'";
                        
                        var parameters = new NpgsqlParameter[]
                        {
                            new("@status", request.NewStatus),
                            new("@unit_id", unitId)
                        };

                        var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);
                        
                        if (rowsAffected > 0)
                        {
                            result.SuccessCount++;
                            result.SuccessItems.Add(new BulkOperationSuccess
                            {
                                ItemId = unitId.ToString(),
                                Description = $"Unit {unitId} status updated to {request.NewStatus}"
                            });
                        }
                        else
                        {
                            result.Failures.Add(new BulkOperationFailure
                            {
                                ItemId = unitId.ToString(),
                                Reason = "Unit not found or currently borrowed"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Failures.Add(new BulkOperationFailure
                        {
                            ItemId = unitId.ToString(),
                            Reason = ex.Message
                        });
                    }
                }

                await _auditService.LogActionAsync(request.UserId, "BULK_STATUS_UPDATE", null, 
                    new { NewStatus = request.NewStatus, SuccessCount = result.SuccessCount, FailureCount = result.Failures.Count });
                
                await _notificationService.NotifyStatsUpdated();
                
                _logger.LogInformation("Bulk status update completed: {Success} success, {Failures} failures", 
                    result.SuccessCount, result.Failures.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk status update operation");
                result.Failures.Add(new BulkOperationFailure { ItemId = "SYSTEM", Reason = ex.Message });
            }

            return result;
        }
    }

    public class BulkBorrowRequest
    {
        public int UserId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public List<BulkBorrowItem> Items { get; set; } = new();
    }

    public class BulkBorrowItem
    {
        public int StudentId { get; set; }
        public string? StudentIdNumber { get; set; } // Support ID number lookup
        public int UnitId { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class BulkReturnRequest
    {
        public int UserId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public List<int> BorrowIds { get; set; } = new();
        public bool DefaultCondition { get; set; } = true;
    }

    public class BulkStatusUpdateRequest
    {
        public int UserId { get; set; }
        public List<int> UnitIds { get; set; } = new();
        public string NewStatus { get; set; } = string.Empty;
    }

    public class BulkOperationResult
    {
        public string OperationType { get; set; } = string.Empty;
        public int SuccessCount { get; set; }
        public List<BulkOperationSuccess> SuccessItems { get; set; } = new();
        public List<BulkOperationFailure> Failures { get; set; } = new();
        public bool HasFailures => Failures.Any();
        public bool IsCompleteSuccess => !HasFailures && SuccessCount > 0;
    }

    public class BulkOperationSuccess
    {
        public string ItemId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class BulkOperationFailure
    {
        public string ItemId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
