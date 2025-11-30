/**
 * SupremeShare UI Utilities
 * Toastify.js for quick notifications
 * SweetAlert2 for confirmations and dialogs
 */

// ============================================
// TOAST NOTIFICATIONS (Toastify.js)
// ============================================
const Toast = {
    success: (message, duration = 3000) => {
        Toastify({
            text: message,
            duration: duration,
            gravity: "top",
            position: "right",
            style: {
                background: "linear-gradient(135deg, #28a745 0%, #20c997 100%)",
                borderRadius: "8px",
                boxShadow: "0 4px 15px rgba(40, 167, 69, 0.3)"
            },
            className: "toast-success"
        }).showToast();
    },

    error: (message, duration = 4000) => {
        Toastify({
            text: message,
            duration: duration,
            gravity: "top",
            position: "right",
            style: {
                background: "linear-gradient(135deg, #dc3545 0%, #c82333 100%)",
                borderRadius: "8px",
                boxShadow: "0 4px 15px rgba(220, 53, 69, 0.3)"
            },
            className: "toast-error"
        }).showToast();
    },

    warning: (message, duration = 3500) => {
        Toastify({
            text: message,
            duration: duration,
            gravity: "top",
            position: "right",
            style: {
                background: "linear-gradient(135deg, #ffc107 0%, #e0a800 100%)",
                color: "#212529",
                borderRadius: "8px",
                boxShadow: "0 4px 15px rgba(255, 193, 7, 0.3)"
            },
            className: "toast-warning"
        }).showToast();
    },

    info: (message, duration = 3000) => {
        Toastify({
            text: message,
            duration: duration,
            gravity: "top",
            position: "right",
            style: {
                background: "linear-gradient(135deg, #17a2b8 0%, #138496 100%)",
                borderRadius: "8px",
                boxShadow: "0 4px 15px rgba(23, 162, 184, 0.3)"
            },
            className: "toast-info"
        }).showToast();
    }
};

// ============================================
// CONFIRMATION DIALOGS (SweetAlert2)
// ============================================
const Confirm = {
    delete: async (itemName = "this item") => {
        const result = await Swal.fire({
            title: "Delete Confirmation",
            html: `Are you sure you want to delete <strong>${itemName}</strong>?<br><small class="text-muted">This action cannot be undone.</small>`,
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#dc3545",
            cancelButtonColor: "#6c757d",
            confirmButtonText: '<i class="bi bi-trash me-1"></i>Yes, Delete',
            cancelButtonText: "Cancel",
            reverseButtons: true
        });
        return result.isConfirmed;
    },

    action: async (title, message, confirmText = "Confirm", icon = "question") => {
        const result = await Swal.fire({
            title: title,
            html: message,
            icon: icon,
            showCancelButton: true,
            confirmButtonColor: "#8B0000",
            cancelButtonColor: "#6c757d",
            confirmButtonText: confirmText,
            cancelButtonText: "Cancel",
            reverseButtons: true
        });
        return result.isConfirmed;
    },

    markLost: async (equipmentName) => {
        const result = await Swal.fire({
            title: "Mark as Lost?",
            html: `<p>Are you sure you want to mark <strong>${equipmentName}</strong> as lost?</p>
                   <div class="alert alert-warning mt-3 mb-0">
                       <i class="bi bi-exclamation-triangle me-2"></i>
                       <strong>Warning:</strong> The student will be automatically blocked from borrowing.
                   </div>`,
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#dc3545",
            cancelButtonColor: "#6c757d",
            confirmButtonText: '<i class="bi bi-exclamation-triangle me-1"></i>Mark as Lost',
            cancelButtonText: "Cancel",
            reverseButtons: true,
            input: "textarea",
            inputLabel: "Remarks (optional)",
            inputPlaceholder: "Enter any additional notes..."
        });
        return { confirmed: result.isConfirmed, remarks: result.value || "" };
    },

    return: async (equipmentName) => {
        const result = await Swal.fire({
            title: "Process Return",
            html: `<p>Processing return for <strong>${equipmentName}</strong></p>`,
            icon: "question",
            showCancelButton: true,
            confirmButtonColor: "#28a745",
            cancelButtonColor: "#6c757d",
            confirmButtonText: '<i class="bi bi-check-circle me-1"></i>Good Condition',
            cancelButtonText: "Cancel",
            showDenyButton: true,
            denyButtonColor: "#ffc107",
            denyButtonText: '<i class="bi bi-exclamation-circle me-1"></i>Damaged',
            reverseButtons: false
        });
        if (result.isConfirmed) return { proceed: true, condition: true };
        if (result.isDenied) return { proceed: true, condition: false };
        return { proceed: false };
    },

    unblock: async (studentName) => {
        const result = await Swal.fire({
            title: "Unblock Student",
            html: `<p>Unblock <strong>${studentName}</strong> from borrowing?</p>`,
            icon: "question",
            showCancelButton: true,
            confirmButtonColor: "#28a745",
            cancelButtonColor: "#6c757d",
            confirmButtonText: '<i class="bi bi-unlock me-1"></i>Unblock',
            cancelButtonText: "Cancel",
            reverseButtons: true,
            input: "textarea",
            inputLabel: "Reason for unblocking",
            inputPlaceholder: "e.g., Item found, Replacement provided...",
            inputValidator: (value) => {
                if (!value || value.trim() === "") {
                    return "Please provide a reason for unblocking";
                }
            }
        });
        return { confirmed: result.isConfirmed, reason: result.value || "" };
    }
};

// ============================================
// LOADING STATES
// ============================================
const Loading = {
    show: (message = "Processing...") => {
        Swal.fire({
            title: message,
            allowOutsideClick: false,
            allowEscapeKey: false,
            showConfirmButton: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });
    },

    hide: () => {
        Swal.close();
    },

    button: (btn, loading = true) => {
        if (loading) {
            btn.dataset.originalText = btn.innerHTML;
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Processing...';
        } else {
            btn.disabled = false;
            btn.innerHTML = btn.dataset.originalText || btn.innerHTML;
        }
    }
};

// ============================================
// RESULT DIALOGS
// ============================================
const Result = {
    success: (title, message) => {
        return Swal.fire({
            title: title,
            html: message,
            icon: "success",
            confirmButtonColor: "#8B0000",
            confirmButtonText: "OK"
        });
    },

    error: (title, message) => {
        return Swal.fire({
            title: title,
            html: message,
            icon: "error",
            confirmButtonColor: "#dc3545",
            confirmButtonText: "OK"
        });
    },

    bulkOperation: (data) => {
        const successHtml = data.successItems?.length > 0
            ? `<div class="text-start mt-3"><strong class="text-success">✓ Successful (${data.successCount}):</strong><ul class="small">${data.successItems.slice(0, 5).map(i => `<li>${i.description}</li>`).join('')}${data.successItems.length > 5 ? `<li>...and ${data.successItems.length - 5} more</li>` : ''}</ul></div>`
            : '';
        const failureHtml = data.failures?.length > 0
            ? `<div class="text-start mt-3"><strong class="text-danger">✗ Failed (${data.failures.length}):</strong><ul class="small">${data.failures.slice(0, 5).map(i => `<li>${i.itemId}: ${i.reason}</li>`).join('')}${data.failures.length > 5 ? `<li>...and ${data.failures.length - 5} more</li>` : ''}</ul></div>`
            : '';

        return Swal.fire({
            title: data.operationType || "Operation Complete",
            html: `<p><strong>${data.successCount}</strong> successful, <strong>${data.failures?.length || 0}</strong> failed</p>${successHtml}${failureHtml}`,
            icon: data.failures?.length > 0 ? "warning" : "success",
            confirmButtonColor: "#8B0000"
        });
    }
};

// ============================================
// DATATABLE UTILITIES
// ============================================
const DataTableConfig = {
    default: {
        responsive: true,
        pageLength: 10,
        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        language: {
            search: "_INPUT_",
            searchPlaceholder: "Search...",
            lengthMenu: "Show _MENU_ entries",
            info: "Showing _START_ to _END_ of _TOTAL_ entries",
            infoEmpty: "No entries available",
            infoFiltered: "(filtered from _MAX_ total entries)",
            paginate: {
                first: '<i class="bi bi-chevron-double-left"></i>',
                previous: '<i class="bi bi-chevron-left"></i>',
                next: '<i class="bi bi-chevron-right"></i>',
                last: '<i class="bi bi-chevron-double-right"></i>'
            },
            emptyTable: "No data available",
            zeroRecords: "No matching records found"
        },
        dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>rtip'
    },

    init: (selector, options = {}) => {
        return $(selector).DataTable({
            ...DataTableConfig.default,
            ...options
        });
    }
};

// ============================================
// FORM UTILITIES
// ============================================
const Form = {
    getToken: () => {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    },

    serialize: (formElement) => {
        const formData = new FormData(formElement);
        const data = {};
        formData.forEach((value, key) => {
            data[key] = value;
        });
        return data;
    },

    reset: (formElement) => {
        formElement.reset();
        formElement.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
        formElement.querySelectorAll('.invalid-feedback').forEach(el => el.textContent = '');
    }
};

// ============================================
// API UTILITIES
// ============================================
const Api = {
    post: async (url, data) => {
        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': Form.getToken()
                },
                body: JSON.stringify(data)
            });
            return await response.json();
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    },

    get: async (url) => {
        try {
            const response = await fetch(url);
            return await response.json();
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    }
};

// Make utilities globally available
window.Toast = Toast;
window.Confirm = Confirm;
window.Loading = Loading;
window.Result = Result;
window.DataTableConfig = DataTableConfig;
window.Form = Form;
window.Api = Api;
