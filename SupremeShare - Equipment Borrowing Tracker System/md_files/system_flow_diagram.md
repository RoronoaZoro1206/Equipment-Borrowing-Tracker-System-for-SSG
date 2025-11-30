┌─────────────────────────────────────────────────────────────────┐
│                        LOGIN PAGE                                │
│                    (Auth/Login.cshtml)                          │
└─────────────────────────┬───────────────────────────────────────┘
                          │
            ┌─────────────┴─────────────┐
            ▼                           ▼
┌───────────────────────┐   ┌───────────────────────┐
│      ADMIN ROLE       │   │   SUPPORT_ADMIN ROLE  │
│   /Admin/Index        │   │   /SupportAdmin/Index │
└───────────┬───────────┘   └───────────┬───────────┘
            │                           │
    ┌───────┴───────┐           ┌───────┴───────┐
    ▼               ▼           ▼               ▼
┌───────┐     ┌─────────┐   ┌───────┐     ┌─────────┐
│ FULL  │     │ MANAGE  │   │ VIEW  │     │ PROCESS │
│ACCESS │     │  ALL    │   │ ONLY  │     │ BORROW/ │
│       │     │ RECORDS │   │       │     │ RETURN  │
└───────┘     └─────────┘   └───────┘     └─────────┘

BORROW FLOW:
Student ID → Validate Student (fn_check_student_eligibility)
           → Select Equipment → Check Stock (fn_get_available_stock)
           → Select Unit → Set Due Date
           → sp_process_borrow_transaction
           → Unit Status: 'available' → 'borrowed'
           → Audit Log Entry

RETURN FLOW:
Scan/Search Borrow Record → Verify Student
                         → Check Condition (Good/Damaged)
                         → sp_process_return_transaction
                         → Unit Status: 'borrowed' → 'available'/'for review'
                         → If Damaged: eunit_condition = FALSE
                         → Audit Log Entry

LOST PROTOCOL (Trigger):
Mark as 'lost' → trg_enforce_lost_protocol fires
              → Unit Status: 'lost'
              → Student Status: FALSE (blocked)
              → Audit Log Entry
