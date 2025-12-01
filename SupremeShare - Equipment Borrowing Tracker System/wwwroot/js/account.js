// Account Management JavaScript
$(document).ready(function () {
    // Initialize forms
    initializeForms();
    
    // Password strength indicator
    $('#newPassword').on('input', function () {
        updatePasswordStrength($(this).val());
    });
});

function initializeForms() {
    // Username form
    $('#usernameForm').on('submit', function (e) {
        e.preventDefault();
        updateUsername();
    });

    // Email form
    $('#emailForm').on('submit', function (e) {
        e.preventDefault();
        requestEmailChange();
    });

    // Password form
    $('#passwordForm').on('submit', function (e) {
        e.preventDefault();
        changePassword();
    });

    // Personal info form
    $('#personalInfoForm').on('submit', function (e) {
        e.preventDefault();
        updatePersonalInfo();
    });
}

// Open Account Info Modal with populated data
function openAccountInfoModal() {
    $.get('/Account/GetAccountInfo', function (res) {
        if (res.success) {
            const data = res.data;
            $('#editUsername').val(data.username);
            $('#currentEmailDisplay').val(data.email || 'Not set');
            $('#editEmail').val('');
            
            // Show pending email if exists
            if (data.hasPendingEmailVerification && data.pendingEmail) {
                $('#pendingEmailAlert').removeClass('d-none');
                $('#pendingEmailDisplay').text(data.pendingEmail);
            } else {
                $('#pendingEmailAlert').addClass('d-none');
            }
            
            // Clear password fields
            $('#currentPassword, #newPassword, #confirmPassword').val('');
            $('#passwordStrength').html('');
            
            // Show modal
            new bootstrap.Modal(document.getElementById('accountInfoModal')).show();
        } else {
            showToast('error', res.message || 'Failed to load account info');
        }
    }).fail(function () {
        showToast('error', 'Failed to load account information');
    });
}

// Open Personal Info Modal with populated data
function openPersonalInfoModal() {
    $.get('/Account/GetAccountInfo', function (res) {
        if (res.success) {
            const data = res.data;
            $('#editLastName').val(data.lastName);
            $('#editFirstName').val(data.firstName);
            $('#editMiddleName').val(data.middleName);
            $('#editSuffix').val(data.suffix);
            $('#editContactNo').val(data.contactNo);
            
            new bootstrap.Modal(document.getElementById('personalInfoModal')).show();
        } else {
            showToast('error', res.message || 'Failed to load personal info');
        }
    }).fail(function () {
        showToast('error', 'Failed to load personal information');
    });
}

// Update Username
function updateUsername() {
    const username = $('#editUsername').val().trim();
    
    if (!username || username.length < 3) {
        showToast('error', 'Username must be at least 3 characters');
        return;
    }
    
    if (!/^[a-zA-Z0-9_]+$/.test(username)) {
        showToast('error', 'Username can only contain letters, numbers, and underscores');
        return;
    }

    $.ajax({
        url: '/Account/UpdateUsername',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ username: username }),
        headers: { 'RequestVerificationToken': getAntiForgeryToken() },
        success: function (res) {
            if (res.success) {
                showToast('success', res.message);
                $('#displayUsername').val(username);
                bootstrap.Modal.getInstance(document.getElementById('accountInfoModal'))?.hide();
            } else {
                showToast('error', res.message);
            }
        },
        error: function () {
            showToast('error', 'Failed to update username');
        }
    });
}

// Request Email Change
function requestEmailChange() {
    const newEmail = $('#editEmail').val().trim();
    
    if (!newEmail || !isValidEmail(newEmail)) {
        showToast('error', 'Please enter a valid email address');
        return;
    }

    showLoading('Sending verification email...');

    $.ajax({
        url: '/Account/RequestEmailChange',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ newEmail: newEmail }),
        headers: { 'RequestVerificationToken': getAntiForgeryToken() },
        success: function (res) {
            hideLoading();
            if (res.success) {
                showToast('success', res.message);
                $('#pendingEmailAlert').removeClass('d-none');
                $('#pendingEmailDisplay').text(newEmail);
                $('#editEmail').val('');
            } else {
                showToast('error', res.message);
            }
        },
        error: function () {
            hideLoading();
            showToast('error', 'Failed to send verification email');
        }
    });
}

// Cancel Email Change
function cancelEmailChange() {
    Swal.fire({
        title: 'Cancel Email Change?',
        text: 'Are you sure you want to cancel the email change request?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#8B0000',
        cancelButtonColor: '#6c757d',
        confirmButtonText: '<i class="bi bi-x-circle me-1"></i> Yes, Cancel Request',
        cancelButtonText: 'No, Keep It',
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/Account/CancelEmailChange',
                type: 'POST',
                headers: { 'RequestVerificationToken': getAntiForgeryToken() },
                success: function (res) {
                    if (res.success) {
                        Swal.fire({
                            title: 'Cancelled!',
                            text: res.message,
                            icon: 'success',
                            confirmButtonColor: '#8B0000'
                        });
                        $('#pendingEmailAlert').addClass('d-none');
                        // Update main display
                        $('.input-group-text.bg-warning').remove();
                        $('small.text-warning').remove();
                    } else {
                        showToast('error', res.message);
                    }
                },
                error: function () {
                    showToast('error', 'Failed to cancel email change');
                }
            });
        }
    });
}

// Change Password
function changePassword() {
    const currentPassword = $('#currentPassword').val();
    const newPassword = $('#newPassword').val();
    const confirmPassword = $('#confirmPassword').val();

    // Validation
    if (!currentPassword) {
        showToast('error', 'Please enter your current password');
        return;
    }

    if (!newPassword || newPassword.length < 8) {
        showToast('error', 'New password must be at least 8 characters');
        return;
    }

    if (!isStrongPassword(newPassword)) {
        showToast('error', 'Password must contain uppercase, lowercase, number, and special character');
        return;
    }

    if (newPassword !== confirmPassword) {
        showToast('error', 'Passwords do not match');
        return;
    }

    showLoading('Changing password...');

    $.ajax({
        url: '/Account/ChangePassword',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            currentPassword: currentPassword,
            newPassword: newPassword,
            confirmPassword: confirmPassword
        }),
        headers: { 'RequestVerificationToken': getAntiForgeryToken() },
        success: function (res) {
            hideLoading();
            if (res.success) {
                showToast('success', res.message);
                $('#currentPassword, #newPassword, #confirmPassword').val('');
                $('#passwordStrength').html('');
                bootstrap.Modal.getInstance(document.getElementById('accountInfoModal'))?.hide();
            } else {
                showToast('error', res.message);
            }
        },
        error: function () {
            hideLoading();
            showToast('error', 'Failed to change password');
        }
    });
}

// Update Personal Info
function updatePersonalInfo() {
    const data = {
        lastName: $('#editLastName').val().trim(),
        firstName: $('#editFirstName').val().trim(),
        middleName: $('#editMiddleName').val().trim() || null,
        suffix: $('#editSuffix').val().trim() || null,
        contactNo: $('#editContactNo').val().trim() || null
    };

    if (!data.lastName || !data.firstName) {
        showToast('error', 'Last name and first name are required');
        return;
    }

    $.ajax({
        url: '/Account/UpdatePersonalInfo',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        headers: { 'RequestVerificationToken': getAntiForgeryToken() },
        success: function (res) {
            if (res.success) {
                showToast('success', res.message);
                
                // Update display fields
                $('#displayLastName').val(data.lastName);
                $('#displayFirstName').val(data.firstName);
                $('#displayMiddleName').val(data.middleName || '');
                $('#displaySuffix').val(data.suffix || '');
                $('#displayContactNo').val(data.contactNo || '');
                $('#lastUpdatedDisplay').text(res.lastUpdated);
                
                bootstrap.Modal.getInstance(document.getElementById('personalInfoModal'))?.hide();
            } else {
                showToast('error', res.message);
            }
        },
        error: function () {
            showToast('error', 'Failed to update personal information');
        }
    });
}

// Toggle password visibility
function togglePassword(inputId) {
    const input = document.getElementById(inputId);
    const icon = input.nextElementSibling.querySelector('i');
    
    if (input.type === 'password') {
        input.type = 'text';
        icon.classList.remove('bi-eye');
        icon.classList.add('bi-eye-slash');
    } else {
        input.type = 'password';
        icon.classList.remove('bi-eye-slash');
        icon.classList.add('bi-eye');
    }
}

// Password strength indicator
function updatePasswordStrength(password) {
    const strengthDiv = $('#passwordStrength');
    
    if (!password) {
        strengthDiv.html('');
        return;
    }

    let strength = 0;
    let feedback = [];

    if (password.length >= 8) strength++;
    else feedback.push('At least 8 characters');

    if (/[a-z]/.test(password)) strength++;
    else feedback.push('Lowercase letter');

    if (/[A-Z]/.test(password)) strength++;
    else feedback.push('Uppercase letter');

    if (/\d/.test(password)) strength++;
    else feedback.push('Number');

    if (/[@$!%*?&]/.test(password)) strength++;
    else feedback.push('Special character (@$!%*?&)');

    let color, text;
    if (strength <= 2) {
        color = 'danger';
        text = 'Weak';
    } else if (strength <= 3) {
        color = 'warning';
        text = 'Fair';
    } else if (strength <= 4) {
        color = 'info';
        text = 'Good';
    } else {
        color = 'success';
        text = 'Strong';
    }

    let html = `<div class="progress" style="height: 5px;">
        <div class="progress-bar bg-${color}" style="width: ${strength * 20}%"></div>
    </div>
    <small class="text-${color}">${text}</small>`;

    if (feedback.length > 0) {
        html += `<br><small class="text-muted">Missing: ${feedback.join(', ')}</small>`;
    }

    strengthDiv.html(html);
}

// Validation helpers
function isValidEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function isStrongPassword(password) {
    return /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/.test(password);
}

// Get anti-forgery token
function getAntiForgeryToken() {
    return $('input[name="__RequestVerificationToken"]').val() || 
           $('meta[name="csrf-token"]').attr('content') || '';
}

// Toast notification - uses global Toast from ui-utils.js
function showToast(type, message) {
    if (typeof Toast !== 'undefined' && Toast[type]) {
        Toast[type](message);
    } else if (typeof Toastify !== 'undefined') {
        Toastify({
            text: message,
            duration: 3000,
            gravity: "top",
            position: "right",
            style: {
                background: type === 'success' ? '#28a745' : type === 'error' ? '#dc3545' : '#17a2b8'
            }
        }).showToast();
    } else {
        alert(message);
    }
}

// Loading overlay - uses global Loading from ui-utils.js
function showLoading(message) {
    if (typeof Loading !== 'undefined') {
        Loading.show(message);
    } else if (typeof Swal !== 'undefined') {
        Swal.fire({
            title: message || 'Please wait...',
            allowOutsideClick: false,
            didOpen: () => Swal.showLoading()
        });
    }
}

function hideLoading() {
    if (typeof Loading !== 'undefined') {
        Loading.hide();
    } else if (typeof Swal !== 'undefined') {
        Swal.close();
    }
}
