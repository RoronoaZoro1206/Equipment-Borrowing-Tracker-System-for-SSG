/**
 * SupremeShare Global SignalR Client
 * Handles real-time updates across all pages
 * All notifications are logged to console (F12) only
 */

(function () {
    'use strict';

    // SignalR connection instance
    let connection = null;
    let isConnected = false;
    let reconnectAttempts = 0;
    const maxReconnectAttempts = 5;

    // Console styling for better visibility in F12
    const styles = {
        connected: 'color: #22c55e; font-weight: bold;',
        disconnected: 'color: #ef4444; font-weight: bold;',
        reconnecting: 'color: #f59e0b; font-weight: bold;',
        notification: 'color: #3b82f6; font-weight: bold;',
        transaction: 'color: #8b5cf6; font-weight: bold;',
        equipment: 'color: #06b6d4; font-weight: bold;',
        audit: 'color: #ec4899; font-weight: bold;',
        overdue: 'color: #ef4444; font-weight: bold;',
        stats: 'color: #10b981; font-weight: bold;'
    };

    // Initialize SignalR connection
    function initializeSignalR() {
        // Check if SignalR library is loaded
        if (typeof signalR === 'undefined') {
            console.warn('[SignalR] Library not loaded. Real-time updates disabled.');
            return;
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/dashboard')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        // Register event handlers
        registerEventHandlers();

        // Start connection
        startConnection();
    }

    // Start the SignalR connection
    async function startConnection() {
        try {
            await connection.start();
            isConnected = true;
            reconnectAttempts = 0;
            console.log('%c[SignalR] ✓ Connected to real-time hub', styles.connected);
            console.log('%c[SignalR] Real-time updates are now active', styles.notification);
        } catch (err) {
            isConnected = false;
            console.error('%c[SignalR] ✗ Connection failed:', styles.disconnected, err.message);
            
            // Retry connection
            if (reconnectAttempts < maxReconnectAttempts) {
                reconnectAttempts++;
                const delay = Math.min(1000 * Math.pow(2, reconnectAttempts), 30000);
                console.log(`%c[SignalR] Retrying in ${delay/1000}s (attempt ${reconnectAttempts}/${maxReconnectAttempts})`, styles.reconnecting);
                setTimeout(startConnection, delay);
            }
        }
    }

    // Register all SignalR event handlers
    function registerEventHandlers() {
        // Connection state handlers
        connection.onreconnecting((error) => {
            isConnected = false;
            console.log('%c[SignalR] ⟳ Reconnecting...', styles.reconnecting, error?.message || '');
        });

        connection.onreconnected((connectionId) => {
            isConnected = true;
            console.log('%c[SignalR] ✓ Reconnected', styles.connected, `(${connectionId})`);
        });

        connection.onclose((error) => {
            isConnected = false;
            console.log('%c[SignalR] ✗ Connection closed', styles.disconnected, error?.message || '');
        });

        // Stats Updated - Dashboard refresh
        connection.on('StatsUpdated', () => {
            console.log('%c[SignalR] 📊 Stats Updated', styles.stats, '- Dashboard data refreshed');
            triggerPageRefresh('stats');
        });

        // New Transaction
        connection.on('NewTransaction', (data) => {
            console.log('%c[SignalR] 📝 New Transaction', styles.transaction);
            console.table({
                'Transaction Code': data.transactionCode,
                'Student': data.studentName,
                'Action': data.action,
                'Time': data.timestamp
            });
            triggerPageRefresh('transaction');
        });

        // Overdue Alert
        connection.on('OverdueAlert', (count) => {
            console.log('%c[SignalR] ⚠️ Overdue Alert', styles.overdue, `- ${count} item(s) overdue`);
            triggerPageRefresh('overdue');
        });

        // Equipment Status Changed
        connection.on('EquipmentStatusChanged', (data) => {
            console.log('%c[SignalR] 🔧 Equipment Status Changed', styles.equipment);
            console.table({
                'Equipment': data.equipmentName,
                'Serial No': data.serialNo,
                'New Status': data.newStatus,
                'Time': data.timestamp
            });
            triggerPageRefresh('equipment');
        });

        // Audit Log Entry
        connection.on('AuditLogEntry', (data) => {
            console.log('%c[SignalR] 📋 New Audit Entry', styles.audit);
            console.table({
                'Action': data.action,
                'User': data.username,
                'Time': data.timestamp
            });
            triggerPageRefresh('audit');
        });

        // Student Updated
        connection.on('StudentUpdated', (data) => {
            console.log('%c[SignalR] 👤 Student Updated', styles.notification, data);
            triggerPageRefresh('student');
        });

        // User Updated
        connection.on('UserUpdated', (data) => {
            console.log('%c[SignalR] 👥 User Updated', styles.notification, data);
            triggerPageRefresh('user');
        });

        // Generic Refresh
        connection.on('RefreshStats', () => {
            console.log('%c[SignalR] 🔄 Refresh Requested', styles.stats);
            triggerPageRefresh('all');
        });
    }

    // Trigger page-specific refresh based on event type
    function triggerPageRefresh(eventType) {
        const path = window.location.pathname.toLowerCase();

        // Dispatch custom event for page-specific handlers
        const event = new CustomEvent('signalr:update', {
            detail: { type: eventType, timestamp: new Date().toISOString() }
        });
        document.dispatchEvent(event);

        // Auto-refresh based on current page
        if (eventType === 'all' || eventType === 'stats') {
            if (path.includes('/admin') && path.endsWith('/admin')) {
                if (typeof loadDashboardStats === 'function') loadDashboardStats();
            }
        }

        if (eventType === 'transaction' || eventType === 'all') {
            if (path.includes('/transactions')) {
                if (typeof loadTransactions === 'function') loadTransactions();
                if (typeof refreshTable === 'function') refreshTable();
            }
        }

        if (eventType === 'equipment' || eventType === 'all') {
            if (path.includes('/equipment')) {
                if (typeof loadEquipment === 'function') loadEquipment();
                if (typeof refreshTable === 'function') refreshTable();
            }
        }

        if (eventType === 'audit' || eventType === 'all') {
            if (path.includes('/auditlog')) {
                if (typeof loadAuditLogs === 'function') loadAuditLogs(1);
            }
        }

        if (eventType === 'student' || eventType === 'all') {
            if (path.includes('/students')) {
                if (typeof loadStudents === 'function') loadStudents();
                if (typeof refreshTable === 'function') refreshTable();
            }
        }

        if (eventType === 'user' || eventType === 'all') {
            if (path.includes('/users')) {
                if (typeof loadUsers === 'function') loadUsers();
                if (typeof refreshTable === 'function') refreshTable();
            }
        }

        if (eventType === 'overdue' || eventType === 'all') {
            if (path.includes('/reports/overdue') || path.includes('/bulkoperations')) {
                if (typeof loadOverdueItems === 'function') loadOverdueItems();
            }
        }
    }

    // Public API
    window.SupremeShareSignalR = {
        // Check connection status
        isConnected: () => isConnected,

        // Get connection instance
        getConnection: () => connection,

        // Manually trigger refresh
        refresh: (type = 'all') => triggerPageRefresh(type),

        // Join a specific room
        joinRoom: async (roomName) => {
            if (connection && isConnected) {
                await connection.invoke('JoinReportRoom', roomName);
                console.log(`%c[SignalR] Joined room: ${roomName}`, styles.notification);
            }
        },

        // Leave a room
        leaveRoom: async (roomName) => {
            if (connection && isConnected) {
                await connection.invoke('LeaveReportRoom', roomName);
                console.log(`%c[SignalR] Left room: ${roomName}`, styles.notification);
            }
        }
    };

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeSignalR);
    } else {
        initializeSignalR();
    }

})();
