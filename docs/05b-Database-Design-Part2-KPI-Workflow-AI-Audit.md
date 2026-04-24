# 🗄️ OmniBiz AI — Database Design Part 2: KPI/OKR, Workflow, AI, Notifications, Audit, Files

---

## 4. KPI/OKR MODULE (12 tables)

### 4.1 `evaluation_periods`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | uniqueidentifier | PK | |
| company_id | uniqueidentifier | FK→Companies | |
| name | nvarchar(200) | NOT NULL | Q1-2026, Tháng 4/2026 |
| type | nvarchar(20) | NOT NULL | Monthly/Quarterly/HalfYear/Yearly |
| start_date | date | NOT NULL | |
| end_date | date | NOT NULL | |
| status | nvarchar(20) | DEFAULT 'Planning' | Planning/Active/Review/Closed |
| okr_weight | decimal(5,2) | DEFAULT 50 | % trọng số OKR |
| kpi_weight | decimal(5,2) | DEFAULT 50 | % trọng số KPI |
| check_in_frequency | nvarchar(20) | DEFAULT 'Weekly' | Daily/Weekly/BiWeekly/Monthly |
| allow_self_evaluation | bit | DEFAULT 1 | |
| created_by | uniqueidentifier | FK→Users | |
| created_at | datetime2 | DEFAULT GETUTCDATE() | |
| updated_at | datetime2 | | |

### 4.2 `objectives` (OKR - Objective)
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | uniqueidentifier | PK | |
| company_id | uniqueidentifier | FK→Companies | |
| period_id | uniqueidentifier | FK→EvaluationPeriods | |
| parent_id | uniqueidentifier | FK→Objectives | Cascade OKR |
| title | nvarchar(500) | NOT NULL | |
| description | nvarchar(max) | | |
| owner_type | nvarchar(20) | NOT NULL | Company/Department/Individual |
| department_id | uniqueidentifier | FK→Departments | Khi owner_type=Department |
| owner_id | uniqueidentifier | FK→Employees | Khi owner_type=Individual |
| progress | decimal(5,2) | DEFAULT 0 | 0-100% (avg of KRs) |
| status | nvarchar(20) | DEFAULT 'Draft' | Draft/Active/Completed/Cancelled |
| priority | nvarchar(20) | DEFAULT 'Medium' | Low/Medium/High/Critical |
| start_date | date | | |
| due_date | date | | |
| completed_at | datetime2 | | |
| sort_order | int | DEFAULT 0 | |
| created_by | uniqueidentifier | FK→Users | |
| created_at | datetime2 | DEFAULT GETUTCDATE() | |
| updated_at | datetime2 | | |
| is_deleted | bit | DEFAULT 0 | |

**Indexes**: `IX_obj_period`, `IX_obj_parent`, `IX_obj_department`, `IX_obj_owner`, `IX_obj_status`

### 4.3 `key_results`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | uniqueidentifier | PK | |
| objective_id | uniqueidentifier | FK→Objectives, NOT NULL | |
| title | nvarchar(500) | NOT NULL | |
| description | nvarchar(max) | | |
| metric_type | nvarchar(20) | NOT NULL | Number/Percentage/Currency/Boolean/Milestone |
| unit | nvarchar(50) | | VND, %, items, etc. |
| start_value | decimal(18,2) | DEFAULT 0 | Giá trị ban đầu |
| target_value | decimal(18,2) | NOT NULL | Giá trị mục tiêu |
| current_value | decimal(18,2) | DEFAULT 0 | Giá trị hiện tại |
| progress | decimal(5,2) | DEFAULT 0 | 0-100% |
| weight | decimal(5,2) | NOT NULL | % trọng số trong Objective |
| direction | nvarchar(10) | DEFAULT 'Increase' | Increase/Decrease |
| status | nvarchar(20) | DEFAULT 'NotStarted' | NotStarted/OnTrack/AtRisk/Behind/Completed |
| assignee_id | uniqueidentifier | FK→Employees | |
| confidence_level | nvarchar(20) | | OnTrack/AtRisk/OffTrack |
| last_check_in_at | datetime2 | | |
| sort_order | int | DEFAULT 0 | |
| created_at | datetime2 | DEFAULT GETUTCDATE() | |
| updated_at | datetime2 | | |

**Indexes**: `IX_kr_objective`, `IX_kr_assignee`, `IX_kr_status`

### 4.4 `kr_check_ins`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| key_result_id | uniqueidentifier | FK→KeyResults, NOT NULL |
| check_in_date | date | NOT NULL |
| previous_value | decimal(18,2) | |
| new_value | decimal(18,2) | NOT NULL |
| progress | decimal(5,2) | |
| confidence | nvarchar(20) | | OnTrack/AtRisk/OffTrack |
| note | nvarchar(max) | NOT NULL |
| blockers | nvarchar(max) | |
| next_steps | nvarchar(max) | |
| evidence_urls | nvarchar(max) | DEFAULT '[]' |
| status | nvarchar(20) | DEFAULT 'Submitted' | Submitted/Approved/Rejected |
| reviewed_by | uniqueidentifier | FK→Users |
| reviewed_at | datetime2 | |
| review_comment | nvarchar(max) | |
| submitted_by | uniqueidentifier | FK→Users |
| created_at | datetime2 | DEFAULT GETUTCDATE() |

### 4.5 `kpi_templates`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| company_id | uniqueidentifier | FK→Companies |
| name | nvarchar(300) | NOT NULL |
| description | nvarchar(max) | |
| category | nvarchar(100) | | Sales/Finance/HR/Operations |
| metric_type | nvarchar(20) | NOT NULL |
| unit | nvarchar(50) | |
| default_target | decimal(18,2) | |
| formula | nvarchar(max) | | Auto-calc formula |
| data_source | nvarchar(100) | | transactions/check_ins/manual |
| is_active | bit | DEFAULT 1 |
| created_at | datetime2 | DEFAULT GETUTCDATE() |

### 4.6 `kpis`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | uniqueidentifier | PK | |
| company_id | uniqueidentifier | FK→Companies | |
| template_id | uniqueidentifier | FK→KPITemplates | |
| period_id | uniqueidentifier | FK→EvaluationPeriods | |
| name | nvarchar(300) | NOT NULL | |
| description | nvarchar(max) | | |
| department_id | uniqueidentifier | FK→Departments | |
| assignee_id | uniqueidentifier | FK→Employees | |
| metric_type | nvarchar(20) | NOT NULL | Number/Percentage/Currency/Boolean |
| unit | nvarchar(50) | | |
| target_value | decimal(18,2) | NOT NULL | |
| current_value | decimal(18,2) | DEFAULT 0 | |
| min_value | decimal(18,2) | | Giá trị tối thiểu chấp nhận |
| max_value | decimal(18,2) | | |
| weight | decimal(5,2) | NOT NULL | % trọng số |
| progress | decimal(5,2) | DEFAULT 0 | |
| frequency | nvarchar(20) | DEFAULT 'Monthly' | Daily/Weekly/Monthly |
| formula | nvarchar(max) | | |
| data_source | nvarchar(100) | | |
| direction | nvarchar(10) | DEFAULT 'Increase' | |
| status | nvarchar(20) | DEFAULT 'Active' | Active/Paused/Completed/Cancelled |
| score | decimal(5,2) | | Điểm đánh giá |
| rating | nvarchar(5) | | A/B/C/D/E |
| last_check_in_at | datetime2 | | |
| created_by | uniqueidentifier | FK→Users | |
| created_at | datetime2 | DEFAULT GETUTCDATE() | |
| updated_at | datetime2 | | |
| is_deleted | bit | DEFAULT 0 | |

**Indexes**: `IX_kpi_period`, `IX_kpi_department`, `IX_kpi_assignee`, `IX_kpi_status`

### 4.7 `kpi_check_ins`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| kpi_id | uniqueidentifier | FK→KPIs, NOT NULL |
| check_in_date | date | NOT NULL |
| previous_value | decimal(18,2) | |
| new_value | decimal(18,2) | NOT NULL |
| progress | decimal(5,2) | |
| note | nvarchar(max) | NOT NULL |
| evidence_urls | nvarchar(max) | DEFAULT '[]' |
| status | nvarchar(20) | DEFAULT 'Submitted' |
| reviewed_by | uniqueidentifier | FK→Users |
| reviewed_at | datetime2 | |
| review_comment | nvarchar(max) | |
| submitted_by | uniqueidentifier | FK→Users |
| created_at | datetime2 | DEFAULT GETUTCDATE() |

### 4.8 `performance_evaluations`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| company_id | uniqueidentifier | FK→Companies |
| period_id | uniqueidentifier | FK→EvaluationPeriods |
| employee_id | uniqueidentifier | FK→Employees |
| department_id | uniqueidentifier | FK→Departments |
| okr_score | decimal(5,2) | |
| kpi_score | decimal(5,2) | |
| total_score | decimal(5,2) | |
| rating | nvarchar(5) | | A/B/C/D/E |
| self_assessment | nvarchar(max) | |
| manager_comment | nvarchar(max) | |
| strengths | nvarchar(max) | |
| improvements | nvarchar(max) | |
| goals_next_period | nvarchar(max) | |
| reviewer_id | uniqueidentifier | FK→Employees |
| status | nvarchar(20) | DEFAULT 'Draft' | Draft/SelfReview/ManagerReview/Completed |
| completed_at | datetime2 | |
| created_at | datetime2 | DEFAULT GETUTCDATE() |
| updated_at | datetime2 | |

### 4.9 `evaluation_scores`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| evaluation_id | uniqueidentifier | FK→PerformanceEvaluations |
| source_type | nvarchar(10) | NOT NULL | OKR/KPI |
| source_id | uniqueidentifier | NOT NULL | objective_id or kpi_id |
| source_name | nvarchar(300) | |
| weight | decimal(5,2) | |
| score | decimal(5,2) | |
| rating | nvarchar(5) | |
| comment | nvarchar(max) | |

### 4.10 `kpi_targets_history`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| kpi_id | uniqueidentifier | FK→KPIs |
| old_target | decimal(18,2) | |
| new_target | decimal(18,2) | |
| reason | nvarchar(max) | |
| changed_by | uniqueidentifier | FK→Users |
| created_at | datetime2 | DEFAULT GETUTCDATE() |

### 4.11 `okr_alignment`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | uniqueidentifier | PK | |
| source_objective_id | uniqueidentifier | FK→Objectives | Objective con |
| target_objective_id | uniqueidentifier | FK→Objectives | Objective cha |
| alignment_type | nvarchar(20) | | Contributing/Supporting |
| contribution_weight | decimal(5,2) | | |
| created_at | datetime2 | DEFAULT GETUTCDATE() | |

### 4.12 `kpi_comments`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| kpi_id | uniqueidentifier | FK→KPIs |
| user_id | uniqueidentifier | FK→Users |
| comment | nvarchar(max) | NOT NULL |
| comment_type | nvarchar(20) | DEFAULT 'Note' | Note/Feedback/Warning |
| created_at | datetime2 | DEFAULT GETUTCDATE() |

---

## 5. WORKFLOW MODULE (6 tables)

### 5.1 `workflow_templates`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| company_id | uniqueidentifier | FK→Companies |
| name | nvarchar(200) | NOT NULL |
| entity_type | nvarchar(50) | NOT NULL | PaymentRequest/BudgetAdjustment/KPIApproval/LeaveRequest |
| description | nvarchar(max) | |
| version | int | DEFAULT 1 |
| is_active | bit | DEFAULT 1 |
| is_default | bit | DEFAULT 0 |
| created_by | uniqueidentifier | FK→Users |
| created_at | datetime2 | DEFAULT GETUTCDATE() |
| updated_at | datetime2 | |

### 5.2 `workflow_steps`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| template_id | uniqueidentifier | FK→WorkflowTemplates |
| step_order | int | NOT NULL |
| name | nvarchar(200) | NOT NULL |
| description | nvarchar(max) | |
| approver_type | nvarchar(20) | NOT NULL | Role/Position/SpecificUser/DepartmentManager/DirectManager |
| approver_role_id | uniqueidentifier | FK→Roles |
| approver_position_id | uniqueidentifier | FK→Positions |
| approver_user_id | uniqueidentifier | FK→Users |
| is_required | bit | DEFAULT 1 |
| can_delegate | bit | DEFAULT 0 |
| timeout_hours | int | DEFAULT 48 |
| escalation_action | nvarchar(20) | DEFAULT 'Notify' | Notify/AutoApprove/Escalate |
| escalation_to_step | int | |
| sla_hours | int | |
| created_at | datetime2 | DEFAULT GETUTCDATE() |

### 5.3 `workflow_conditions`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | uniqueidentifier | PK | |
| template_id | uniqueidentifier | FK→WorkflowTemplates | |
| condition_group | int | DEFAULT 1 | Nhóm OR |
| field | nvarchar(100) | NOT NULL | amount/budget_percentage/department/priority |
| operator | nvarchar(10) | NOT NULL | gt/lt/eq/gte/lte/in/not_in |
| value | nvarchar(500) | NOT NULL | Giá trị so sánh |
| value_type | nvarchar(20) | DEFAULT 'number' | number/string/array |
| then_action | nvarchar(20) | DEFAULT 'AddStep' | AddStep/SkipStep/UseTemplate |
| then_step_order | int | | Step cần thêm/skip |
| then_template_id | uniqueidentifier | FK→WorkflowTemplates | Template thay thế |
| priority | int | DEFAULT 0 | Thứ tự evaluation |
| is_active | bit | DEFAULT 1 | |
| created_at | datetime2 | DEFAULT GETUTCDATE() | |

### 5.4 `workflow_instances`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| template_id | uniqueidentifier | FK→WorkflowTemplates |
| entity_type | nvarchar(50) | NOT NULL |
| entity_id | uniqueidentifier | NOT NULL |
| current_step_order | int | DEFAULT 1 |
| total_steps | int | NOT NULL |
| status | nvarchar(20) | DEFAULT 'Pending' | Pending/InProgress/Approved/Rejected/Cancelled/Expired |
| initiated_by | uniqueidentifier | FK→Users |
| initiated_at | datetime2 | DEFAULT GETUTCDATE() |
| completed_at | datetime2 | |
| cancelled_at | datetime2 | |
| cancellation_reason | nvarchar(max) | |
| metadata | nvarchar(max) | DEFAULT '{}' | Snapshot of entity data |

**Indexes**: `IX_wi_entity`, `IX_wi_status`, `IX_wi_template`

### 5.5 `workflow_instance_steps`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| instance_id | uniqueidentifier | FK→WorkflowInstances |
| step_order | int | NOT NULL |
| step_name | nvarchar(200) | |
| assigned_to | uniqueidentifier | FK→Users |
| delegated_to | uniqueidentifier | FK→Users |
| status | nvarchar(20) | DEFAULT 'Pending' | Pending/InProgress/Approved/Rejected/Skipped/Expired |
| started_at | datetime2 | |
| completed_at | datetime2 | |
| deadline_at | datetime2 | |
| reminder_sent | bit | DEFAULT 0 |

### 5.6 `approval_actions`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| instance_step_id | uniqueidentifier | FK→WorkflowInstanceSteps |
| instance_id | uniqueidentifier | FK→WorkflowInstances |
| user_id | uniqueidentifier | FK→Users, NOT NULL |
| action | nvarchar(20) | NOT NULL | Approve/Reject/Comment/RequestChange/Delegate |
| comment | nvarchar(max) | |
| attachments | nvarchar(max) | DEFAULT '[]' |
| action_at | datetime2 | DEFAULT GETUTCDATE() |
| ip_address | nvarchar(45) | |

---

## 6. AI MODULE (6 tables)

### 6.1 `ai_chat_sessions`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| user_id | uniqueidentifier | FK→Users |
| title | nvarchar(300) | |
| context_type | nvarchar(50) | | Dashboard/Finance/KPI/General |
| context_data | nvarchar(max) | DEFAULT '{}' | Current page filters, etc. |
| message_count | int | DEFAULT 0 |
| last_message_at | datetime2 | |
| is_archived | bit | DEFAULT 0 |
| created_at | datetime2 | DEFAULT GETUTCDATE() |

### 6.2 `ai_messages`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| session_id | uniqueidentifier | FK→AIChatSessions |
| role | nvarchar(20) | NOT NULL | user/assistant/system |
| content | nvarchar(max) | NOT NULL |
| content_type | nvarchar(20) | DEFAULT 'text' | text/chart/table/report |
| metadata | nvarchar(max) | DEFAULT '{}' | Token usage, model, etc. |
| citations | nvarchar(max) | DEFAULT '[]' | Data source references |
| charts_data | nvarchar(max) | | Chart rendering data |
| tokens_used | int | DEFAULT 0 |
| model | nvarchar(50) | |
| latency_ms | int | |
| created_at | datetime2 | DEFAULT GETUTCDATE() |

### 6.3 `ai_generation_history`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| user_id | uniqueidentifier | FK→Users |
| company_id | uniqueidentifier | FK→Companies |
| module | nvarchar(50) | NOT NULL | Finance/KPI/Report/Chat |
| prompt_type | nvarchar(50) | NOT NULL | RiskAnalysis/Insight/Report/QA |
| input_summary | nvarchar(max) | |
| input_data | nvarchar(max) | |
| output_content | nvarchar(max) | NOT NULL |
| output_type | nvarchar(20) | DEFAULT 'text' |
| model | nvarchar(50) | |
| tokens_used | int | |
| rating | int | | 1-5 user rating |
| feedback | nvarchar(max) | |
| is_reused | bit | DEFAULT 0 |
| created_at | datetime2 | DEFAULT GETUTCDATE() |
| expires_at | datetime2 | | Retention policy |

### 6.4 `ai_risk_assessments`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| entity_type | nvarchar(50) | NOT NULL | PaymentRequest/BudgetAdjustment |
| entity_id | uniqueidentifier | NOT NULL |
| risk_score | decimal(5,2) | NOT NULL | 0-100 |
| risk_level | nvarchar(20) | NOT NULL | Low/Medium/High/Critical |
| risk_factors | nvarchar(max) | NOT NULL | Array of {factor, score, description} |
| recommendations | nvarchar(max) | DEFAULT '[]' |
| model | nvarchar(50) | |
| assessed_at | datetime2 | DEFAULT GETUTCDATE() |
| assessed_by | nvarchar(20) | DEFAULT 'system' |

### 6.5 `ai_embeddings`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | uniqueidentifier | PK | |
| source_type | nvarchar(50) | NOT NULL | transaction/budget/kpi/objective |
| source_id | uniqueidentifier | NOT NULL | |
| content | nvarchar(max) | NOT NULL | Searchable text |
| embedding | varbinary(max) | NOT NULL | vector search embedding |
| metadata | nvarchar(max) | DEFAULT '{}' | |
| company_id | uniqueidentifier | FK→Companies | Data isolation |
| created_at | datetime2 | DEFAULT GETUTCDATE() | |
| updated_at | datetime2 | | |

**Indexes**: `IX_embeddings_vector` (Full-text index or custom similarity search)

### 6.6 `ai_prompt_templates`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| name | nvarchar(200) | NOT NULL |
| category | nvarchar(50) | NOT NULL |
| system_prompt | nvarchar(max) | NOT NULL |
| user_prompt_template | nvarchar(max) | NOT NULL |
| variables | nvarchar(max) | DEFAULT '[]' |
| model_config | nvarchar(max) | DEFAULT '{}' | temperature, max_tokens, etc. |
| is_active | bit | DEFAULT 1 |
| version | int | DEFAULT 1 |
| created_at | datetime2 | DEFAULT GETUTCDATE() |
| updated_at | datetime2 | |

---

## 7. NOTIFICATION MODULE (3 tables)

### 7.1 `notifications`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| user_id | uniqueidentifier | FK→Users, NOT NULL |
| title | nvarchar(300) | NOT NULL |
| message | nvarchar(max) | NOT NULL |
| type | nvarchar(50) | NOT NULL | ApprovalRequest/ApprovalResult/KPIDeadline/BudgetWarning/AIAlert/System |
| priority | nvarchar(20) | DEFAULT 'Normal' |
| entity_type | nvarchar(50) | |
| entity_id | uniqueidentifier | |
| action_url | nvarchar(500) | |
| is_read | bit | DEFAULT 0 |
| read_at | datetime2 | |
| is_email_sent | bit | DEFAULT 0 |
| created_at | datetime2 | DEFAULT GETUTCDATE() |

**Indexes**: `IX_notif_user_read`, `IX_notif_user_created`

### 7.2 `notification_preferences`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| user_id | uniqueidentifier | FK→Users |
| notification_type | nvarchar(50) | NOT NULL |
| in_app_enabled | bit | DEFAULT 1 |
| email_enabled | bit | DEFAULT 0 |
| email_digest | nvarchar(20) | DEFAULT 'Instant' | Instant/Daily/Weekly |
| created_at | datetime2 | DEFAULT GETUTCDATE() |
| updated_at | datetime2 | |

### 7.3 `email_queue`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| to_email | nvarchar(255) | NOT NULL |
| subject | nvarchar(500) | NOT NULL |
| body | nvarchar(max) | NOT NULL |
| template_name | nvarchar(100) | |
| template_data | nvarchar(max) | |
| status | nvarchar(20) | DEFAULT 'Pending' | Pending/Sent/Failed |
| attempts | int | DEFAULT 0 |
| last_attempt_at | datetime2 | |
| error_message | nvarchar(max) | |
| sent_at | datetime2 | |
| created_at | datetime2 | DEFAULT GETUTCDATE() |

---

## 8. AUDIT & SYSTEM MODULE (4 tables)

### 8.1 `audit_logs`
| Column | Type | Constraints |
|--------|------|-------------|
| id | bigint | PK, IDENTITY(1,1) |
| user_id | uniqueidentifier | FK→Users |
| user_email | nvarchar(255) | |
| action | nvarchar(50) | NOT NULL | Login/Logout/Create/Update/Delete/Approve/Reject/Export/AIQuery |
| entity_type | nvarchar(100) | |
| entity_id | uniqueidentifier | |
| entity_name | nvarchar(300) | |
| old_values | nvarchar(max) | |
| new_values | nvarchar(max) | |
| changes_summary | nvarchar(max) | |
| ip_address | nvarchar(45) | |
| user_agent | nvarchar(500) | |
| request_path | nvarchar(500) | |
| request_method | nvarchar(10) | |
| response_status | int | |
| duration_ms | int | |
| created_at | datetime2 | DEFAULT GETUTCDATE() |

**Indexes**: `IX_audit_user_created`, `IX_audit_entity`, `IX_audit_action_created`  
**Partitioning**: Partition by month (SQL Server table partitioning) on `created_at` for performance

### 8.2 `system_settings`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| company_id | uniqueidentifier | FK→Companies |
| key | nvarchar(100) | NOT NULL |
| value | nvarchar(max) | NOT NULL |
| value_type | nvarchar(20) | DEFAULT 'string' | string/number/boolean/json |
| category | nvarchar(50) | |
| description | nvarchar(max) | |
| is_sensitive | bit | DEFAULT 0 |
| updated_by | uniqueidentifier | FK→Users |
| updated_at | datetime2 | DEFAULT GETUTCDATE() |

**Unique**: `(company_id, key)`

### 8.3 `file_uploads`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| file_name | nvarchar(300) | NOT NULL |
| original_name | nvarchar(300) | NOT NULL |
| file_path | nvarchar(500) | NOT NULL |
| file_size | bigint | NOT NULL |
| content_type | nvarchar(100) | NOT NULL |
| entity_type | nvarchar(50) | |
| entity_id | uniqueidentifier | |
| uploaded_by | uniqueidentifier | FK→Users |
| is_public | bit | DEFAULT 0 |
| created_at | datetime2 | DEFAULT GETUTCDATE() |
| is_deleted | bit | DEFAULT 0 |

### 8.4 `background_jobs`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uniqueidentifier | PK |
| job_type | nvarchar(100) | NOT NULL | EmbeddingSync/ReportGeneration/DataCleanup/EmailDigest |
| status | nvarchar(20) | DEFAULT 'Pending' | Pending/Running/Completed/Failed |
| input_data | nvarchar(max) | |
| output_data | nvarchar(max) | |
| error_message | nvarchar(max) | |
| started_at | datetime2 | |
| completed_at | datetime2 | |
| created_at | datetime2 | DEFAULT GETUTCDATE() |
| retry_count | int | DEFAULT 0 |
| max_retries | int | DEFAULT 3 |

---

## 9. Database Summary

| Module | Tables | Description |
|--------|--------|-------------|
| Identity | 8 | Users, Roles, Permissions, Sessions, Tokens |
| Organization | 6 | Companies, Departments, Positions, Employees |
| Finance | 16 | Budgets, Payment Requests, Transactions, Vendors, Wallets |
| KPI/OKR | 12 | Objectives, Key Results, KPIs, Check-ins, Evaluations |
| Workflow | 6 | Templates, Steps, Conditions, Instances, Approvals |
| AI | 6 | Chat, Messages, History, Risk, Embeddings, Prompts |
| Notification | 3 | Notifications, Preferences, Email Queue |
| Audit & System | 4 | Audit Logs, Settings, Files, Background Jobs |
| **Total** | **61 tables** | |

---

## 10. Migration Strategy

1. **Tool**: EF Core Migrations (Code-First)
2. **Naming**: `YYYYMMDDHHMMSS_DescriptiveName` (e.g., `20260501120000_InitialCreate`)
3. **Process**: 
   - Dev: `dotnet ef migrations add <name>` → `dotnet ef database update`
   - Staging/Prod: Generate SQL script → Review → Apply
4. **Seed Data**: `SeedDataService` chạy khi startup (idempotent)
5. **Rollback**: Mỗi migration có `Down()` method

## 11. Data Lifecycle & Retention

| Data Type | Retention | Action |
|-----------|-----------|--------|
| Audit Logs | 1 year | Archive to cold storage |
| AI Chat History | 90 days | Auto-delete |
| AI Embeddings | Rebuilt nightly | Drop & recreate |
| Notifications | 6 months | Auto-delete read |
| Soft-deleted records | 90 days | Permanent delete job |
| File uploads | Indefinite | Manual cleanup |
| Background jobs | 30 days | Auto-cleanup completed |
