# 🗄️ OmniBiz AI — Database Design Part 2: KPI/OKR, Workflow, AI, Notifications, Audit, Files

---

## 4. KPI/OKR MODULE (12 tables)

### 4.1 `evaluation_periods`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | uuid | PK | |
| company_id | uuid | FK→companies | |
| name | varchar(200) | NOT NULL | Q1-2026, Tháng 4/2026 |
| type | varchar(20) | NOT NULL | Monthly/Quarterly/HalfYear/Yearly |
| start_date | date | NOT NULL | |
| end_date | date | NOT NULL | |
| status | varchar(20) | DEFAULT 'Planning' | Planning/Active/Review/Closed |
| okr_weight | decimal(5,2) | DEFAULT 50 | % trọng số OKR |
| kpi_weight | decimal(5,2) | DEFAULT 50 | % trọng số KPI |
| check_in_frequency | varchar(20) | DEFAULT 'Weekly' | Daily/Weekly/BiWeekly/Monthly |
| allow_self_evaluation | boolean | DEFAULT true | |
| created_by | uuid | FK→users | |
| created_at | timestamptz | DEFAULT NOW() | |
| updated_at | timestamptz | | |

### 4.2 `objectives` (OKR - Objective)
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | uuid | PK | |
| company_id | uuid | FK→companies | |
| period_id | uuid | FK→evaluation_periods | |
| parent_id | uuid | FK→objectives | Cascade OKR |
| title | varchar(500) | NOT NULL | |
| description | text | | |
| owner_type | varchar(20) | NOT NULL | Company/Department/Individual |
| department_id | uuid | FK→departments | Khi owner_type=Department |
| owner_id | uuid | FK→employees | Khi owner_type=Individual |
| progress | decimal(5,2) | DEFAULT 0 | 0-100% (avg of KRs) |
| status | varchar(20) | DEFAULT 'Draft' | Draft/Active/Completed/Cancelled |
| priority | varchar(20) | DEFAULT 'Medium' | Low/Medium/High/Critical |
| start_date | date | | |
| due_date | date | | |
| completed_at | timestamptz | | |
| sort_order | int | DEFAULT 0 | |
| created_by | uuid | FK→users | |
| created_at | timestamptz | DEFAULT NOW() | |
| updated_at | timestamptz | | |
| is_deleted | boolean | DEFAULT false | |

**Indexes**: `IX_obj_period`, `IX_obj_parent`, `IX_obj_department`, `IX_obj_owner`, `IX_obj_status`

### 4.3 `key_results`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | uuid | PK | |
| objective_id | uuid | FK→objectives, NOT NULL | |
| title | varchar(500) | NOT NULL | |
| description | text | | |
| metric_type | varchar(20) | NOT NULL | Number/Percentage/Currency/Boolean/Milestone |
| unit | varchar(50) | | VND, %, items, etc. |
| start_value | decimal(18,2) | DEFAULT 0 | Giá trị ban đầu |
| target_value | decimal(18,2) | NOT NULL | Giá trị mục tiêu |
| current_value | decimal(18,2) | DEFAULT 0 | Giá trị hiện tại |
| progress | decimal(5,2) | DEFAULT 0 | 0-100% |
| weight | decimal(5,2) | NOT NULL | % trọng số trong Objective |
| direction | varchar(10) | DEFAULT 'Increase' | Increase/Decrease |
| status | varchar(20) | DEFAULT 'NotStarted' | NotStarted/OnTrack/AtRisk/Behind/Completed |
| assignee_id | uuid | FK→employees | |
| confidence_level | varchar(20) | | OnTrack/AtRisk/OffTrack |
| last_check_in_at | timestamptz | | |
| sort_order | int | DEFAULT 0 | |
| created_at | timestamptz | DEFAULT NOW() | |
| updated_at | timestamptz | | |

**Indexes**: `IX_kr_objective`, `IX_kr_assignee`, `IX_kr_status`

### 4.4 `kr_check_ins`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| key_result_id | uuid | FK→key_results, NOT NULL |
| check_in_date | date | NOT NULL |
| previous_value | decimal(18,2) | |
| new_value | decimal(18,2) | NOT NULL |
| progress | decimal(5,2) | |
| confidence | varchar(20) | | OnTrack/AtRisk/OffTrack |
| note | text | NOT NULL |
| blockers | text | |
| next_steps | text | |
| evidence_urls | jsonb | DEFAULT '[]' |
| status | varchar(20) | DEFAULT 'Submitted' | Submitted/Approved/Rejected |
| reviewed_by | uuid | FK→users |
| reviewed_at | timestamptz | |
| review_comment | text | |
| submitted_by | uuid | FK→users |
| created_at | timestamptz | DEFAULT NOW() |

### 4.5 `kpi_templates`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| company_id | uuid | FK→companies |
| name | varchar(300) | NOT NULL |
| description | text | |
| category | varchar(100) | | Sales/Finance/HR/Operations |
| metric_type | varchar(20) | NOT NULL |
| unit | varchar(50) | |
| default_target | decimal(18,2) | |
| formula | text | | Auto-calc formula |
| data_source | varchar(100) | | transactions/check_ins/manual |
| is_active | boolean | DEFAULT true |
| created_at | timestamptz | DEFAULT NOW() |

### 4.6 `kpis`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | uuid | PK | |
| company_id | uuid | FK→companies | |
| template_id | uuid | FK→kpi_templates | |
| period_id | uuid | FK→evaluation_periods | |
| name | varchar(300) | NOT NULL | |
| description | text | | |
| department_id | uuid | FK→departments | |
| assignee_id | uuid | FK→employees | |
| metric_type | varchar(20) | NOT NULL | Number/Percentage/Currency/Boolean |
| unit | varchar(50) | | |
| target_value | decimal(18,2) | NOT NULL | |
| current_value | decimal(18,2) | DEFAULT 0 | |
| min_value | decimal(18,2) | | Giá trị tối thiểu chấp nhận |
| max_value | decimal(18,2) | | |
| weight | decimal(5,2) | NOT NULL | % trọng số |
| progress | decimal(5,2) | DEFAULT 0 | |
| frequency | varchar(20) | DEFAULT 'Monthly' | Daily/Weekly/Monthly |
| formula | text | | |
| data_source | varchar(100) | | |
| direction | varchar(10) | DEFAULT 'Increase' | |
| status | varchar(20) | DEFAULT 'Active' | Active/Paused/Completed/Cancelled |
| score | decimal(5,2) | | Điểm đánh giá |
| rating | varchar(5) | | A/B/C/D/E |
| last_check_in_at | timestamptz | | |
| created_by | uuid | FK→users | |
| created_at | timestamptz | DEFAULT NOW() | |
| updated_at | timestamptz | | |
| is_deleted | boolean | DEFAULT false | |

**Indexes**: `IX_kpi_period`, `IX_kpi_department`, `IX_kpi_assignee`, `IX_kpi_status`

### 4.7 `kpi_check_ins`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| kpi_id | uuid | FK→kpis, NOT NULL |
| check_in_date | date | NOT NULL |
| previous_value | decimal(18,2) | |
| new_value | decimal(18,2) | NOT NULL |
| progress | decimal(5,2) | |
| note | text | NOT NULL |
| evidence_urls | jsonb | DEFAULT '[]' |
| status | varchar(20) | DEFAULT 'Submitted' |
| reviewed_by | uuid | FK→users |
| reviewed_at | timestamptz | |
| review_comment | text | |
| submitted_by | uuid | FK→users |
| created_at | timestamptz | DEFAULT NOW() |

### 4.8 `performance_evaluations`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| company_id | uuid | FK→companies |
| period_id | uuid | FK→evaluation_periods |
| employee_id | uuid | FK→employees |
| department_id | uuid | FK→departments |
| okr_score | decimal(5,2) | |
| kpi_score | decimal(5,2) | |
| total_score | decimal(5,2) | |
| rating | varchar(5) | | A/B/C/D/E |
| self_assessment | text | |
| manager_comment | text | |
| strengths | text | |
| improvements | text | |
| goals_next_period | text | |
| reviewer_id | uuid | FK→employees |
| status | varchar(20) | DEFAULT 'Draft' | Draft/SelfReview/ManagerReview/Completed |
| completed_at | timestamptz | |
| created_at | timestamptz | DEFAULT NOW() |
| updated_at | timestamptz | |

### 4.9 `evaluation_scores`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| evaluation_id | uuid | FK→performance_evaluations |
| source_type | varchar(10) | NOT NULL | OKR/KPI |
| source_id | uuid | NOT NULL | objective_id or kpi_id |
| source_name | varchar(300) | |
| weight | decimal(5,2) | |
| score | decimal(5,2) | |
| rating | varchar(5) | |
| comment | text | |

### 4.10 `kpi_targets_history`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| kpi_id | uuid | FK→kpis |
| old_target | decimal(18,2) | |
| new_target | decimal(18,2) | |
| reason | text | |
| changed_by | uuid | FK→users |
| created_at | timestamptz | DEFAULT NOW() |

### 4.11 `okr_alignment`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | uuid | PK | |
| source_objective_id | uuid | FK→objectives | Objective con |
| target_objective_id | uuid | FK→objectives | Objective cha |
| alignment_type | varchar(20) | | Contributing/Supporting |
| contribution_weight | decimal(5,2) | | |
| created_at | timestamptz | DEFAULT NOW() | |

### 4.12 `kpi_comments`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| kpi_id | uuid | FK→kpis |
| user_id | uuid | FK→users |
| comment | text | NOT NULL |
| comment_type | varchar(20) | DEFAULT 'Note' | Note/Feedback/Warning |
| created_at | timestamptz | DEFAULT NOW() |

---

## 5. WORKFLOW MODULE (6 tables)

### 5.1 `workflow_templates`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| company_id | uuid | FK→companies |
| name | varchar(200) | NOT NULL |
| entity_type | varchar(50) | NOT NULL | PaymentRequest/BudgetAdjustment/KPIApproval/LeaveRequest |
| description | text | |
| version | int | DEFAULT 1 |
| is_active | boolean | DEFAULT true |
| is_default | boolean | DEFAULT false |
| created_by | uuid | FK→users |
| created_at | timestamptz | DEFAULT NOW() |
| updated_at | timestamptz | |

### 5.2 `workflow_steps`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| template_id | uuid | FK→workflow_templates |
| step_order | int | NOT NULL |
| name | varchar(200) | NOT NULL |
| description | text | |
| approver_type | varchar(20) | NOT NULL | Role/Position/SpecificUser/DepartmentManager/DirectManager |
| approver_role_id | uuid | FK→roles |
| approver_position_id | uuid | FK→positions |
| approver_user_id | uuid | FK→users |
| is_required | boolean | DEFAULT true |
| can_delegate | boolean | DEFAULT false |
| timeout_hours | int | DEFAULT 48 |
| escalation_action | varchar(20) | DEFAULT 'Notify' | Notify/AutoApprove/Escalate |
| escalation_to_step | int | |
| sla_hours | int | |
| created_at | timestamptz | DEFAULT NOW() |

### 5.3 `workflow_conditions`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | uuid | PK | |
| template_id | uuid | FK→workflow_templates | |
| condition_group | int | DEFAULT 1 | Nhóm OR |
| field | varchar(100) | NOT NULL | amount/budget_percentage/department/priority |
| operator | varchar(10) | NOT NULL | gt/lt/eq/gte/lte/in/not_in |
| value | varchar(500) | NOT NULL | Giá trị so sánh |
| value_type | varchar(20) | DEFAULT 'number' | number/string/array |
| then_action | varchar(20) | DEFAULT 'AddStep' | AddStep/SkipStep/UseTemplate |
| then_step_order | int | | Step cần thêm/skip |
| then_template_id | uuid | FK→workflow_templates | Template thay thế |
| priority | int | DEFAULT 0 | Thứ tự evaluation |
| is_active | boolean | DEFAULT true | |
| created_at | timestamptz | DEFAULT NOW() | |

### 5.4 `workflow_instances`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| template_id | uuid | FK→workflow_templates |
| entity_type | varchar(50) | NOT NULL |
| entity_id | uuid | NOT NULL |
| current_step_order | int | DEFAULT 1 |
| total_steps | int | NOT NULL |
| status | varchar(20) | DEFAULT 'Pending' | Pending/InProgress/Approved/Rejected/Cancelled/Expired |
| initiated_by | uuid | FK→users |
| initiated_at | timestamptz | DEFAULT NOW() |
| completed_at | timestamptz | |
| cancelled_at | timestamptz | |
| cancellation_reason | text | |
| metadata | jsonb | DEFAULT '{}' | Snapshot of entity data |

**Indexes**: `IX_wi_entity`, `IX_wi_status`, `IX_wi_template`

### 5.5 `workflow_instance_steps`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| instance_id | uuid | FK→workflow_instances |
| step_order | int | NOT NULL |
| step_name | varchar(200) | |
| assigned_to | uuid | FK→users |
| delegated_to | uuid | FK→users |
| status | varchar(20) | DEFAULT 'Pending' | Pending/InProgress/Approved/Rejected/Skipped/Expired |
| started_at | timestamptz | |
| completed_at | timestamptz | |
| deadline_at | timestamptz | |
| reminder_sent | boolean | DEFAULT false |

### 5.6 `approval_actions`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| instance_step_id | uuid | FK→workflow_instance_steps |
| instance_id | uuid | FK→workflow_instances |
| user_id | uuid | FK→users, NOT NULL |
| action | varchar(20) | NOT NULL | Approve/Reject/Comment/RequestChange/Delegate |
| comment | text | |
| attachments | jsonb | DEFAULT '[]' |
| action_at | timestamptz | DEFAULT NOW() |
| ip_address | varchar(45) | |

---

## 6. AI MODULE (6 tables)

### 6.1 `ai_chat_sessions`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| user_id | uuid | FK→users |
| title | varchar(300) | |
| context_type | varchar(50) | | Dashboard/Finance/KPI/General |
| context_data | jsonb | DEFAULT '{}' | Current page filters, etc. |
| message_count | int | DEFAULT 0 |
| last_message_at | timestamptz | |
| is_archived | boolean | DEFAULT false |
| created_at | timestamptz | DEFAULT NOW() |

### 6.2 `ai_messages`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| session_id | uuid | FK→ai_chat_sessions |
| role | varchar(20) | NOT NULL | user/assistant/system |
| content | text | NOT NULL |
| content_type | varchar(20) | DEFAULT 'text' | text/chart/table/report |
| metadata | jsonb | DEFAULT '{}' | Token usage, model, etc. |
| citations | jsonb | DEFAULT '[]' | Data source references |
| charts_data | jsonb | | Chart rendering data |
| tokens_used | int | DEFAULT 0 |
| model | varchar(50) | |
| latency_ms | int | |
| created_at | timestamptz | DEFAULT NOW() |

### 6.3 `ai_generation_history`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| user_id | uuid | FK→users |
| company_id | uuid | FK→companies |
| module | varchar(50) | NOT NULL | Finance/KPI/Report/Chat |
| prompt_type | varchar(50) | NOT NULL | RiskAnalysis/Insight/Report/QA |
| input_summary | text | |
| input_data | jsonb | |
| output_content | text | NOT NULL |
| output_type | varchar(20) | DEFAULT 'text' |
| model | varchar(50) | |
| tokens_used | int | |
| rating | int | | 1-5 user rating |
| feedback | text | |
| is_reused | boolean | DEFAULT false |
| created_at | timestamptz | DEFAULT NOW() |
| expires_at | timestamptz | | Retention policy |

### 6.4 `ai_risk_assessments`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| entity_type | varchar(50) | NOT NULL | PaymentRequest/BudgetAdjustment |
| entity_id | uuid | NOT NULL |
| risk_score | decimal(5,2) | NOT NULL | 0-100 |
| risk_level | varchar(20) | NOT NULL | Low/Medium/High/Critical |
| risk_factors | jsonb | NOT NULL | Array of {factor, score, description} |
| recommendations | jsonb | DEFAULT '[]' |
| model | varchar(50) | |
| assessed_at | timestamptz | DEFAULT NOW() |
| assessed_by | varchar(20) | DEFAULT 'system' |

### 6.5 `ai_embeddings`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | uuid | PK | |
| source_type | varchar(50) | NOT NULL | transaction/budget/kpi/objective |
| source_id | uuid | NOT NULL | |
| content | text | NOT NULL | Searchable text |
| embedding | vector(1536) | NOT NULL | pgvector embedding |
| metadata | jsonb | DEFAULT '{}' | |
| company_id | uuid | FK→companies | Data isolation |
| created_at | timestamptz | DEFAULT NOW() | |
| updated_at | timestamptz | | |

**Indexes**: `IX_embeddings_vector` (ivfflat/hnsw index on embedding column)

### 6.6 `ai_prompt_templates`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| name | varchar(200) | NOT NULL |
| category | varchar(50) | NOT NULL |
| system_prompt | text | NOT NULL |
| user_prompt_template | text | NOT NULL |
| variables | jsonb | DEFAULT '[]' |
| model_config | jsonb | DEFAULT '{}' | temperature, max_tokens, etc. |
| is_active | boolean | DEFAULT true |
| version | int | DEFAULT 1 |
| created_at | timestamptz | DEFAULT NOW() |
| updated_at | timestamptz | |

---

## 7. NOTIFICATION MODULE (3 tables)

### 7.1 `notifications`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| user_id | uuid | FK→users, NOT NULL |
| title | varchar(300) | NOT NULL |
| message | text | NOT NULL |
| type | varchar(50) | NOT NULL | ApprovalRequest/ApprovalResult/KPIDeadline/BudgetWarning/AIAlert/System |
| priority | varchar(20) | DEFAULT 'Normal' |
| entity_type | varchar(50) | |
| entity_id | uuid | |
| action_url | varchar(500) | |
| is_read | boolean | DEFAULT false |
| read_at | timestamptz | |
| is_email_sent | boolean | DEFAULT false |
| created_at | timestamptz | DEFAULT NOW() |

**Indexes**: `IX_notif_user_read`, `IX_notif_user_created`

### 7.2 `notification_preferences`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| user_id | uuid | FK→users |
| notification_type | varchar(50) | NOT NULL |
| in_app_enabled | boolean | DEFAULT true |
| email_enabled | boolean | DEFAULT false |
| email_digest | varchar(20) | DEFAULT 'Instant' | Instant/Daily/Weekly |
| created_at | timestamptz | DEFAULT NOW() |
| updated_at | timestamptz | |

### 7.3 `email_queue`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| to_email | varchar(255) | NOT NULL |
| subject | varchar(500) | NOT NULL |
| body | text | NOT NULL |
| template_name | varchar(100) | |
| template_data | jsonb | |
| status | varchar(20) | DEFAULT 'Pending' | Pending/Sent/Failed |
| attempts | int | DEFAULT 0 |
| last_attempt_at | timestamptz | |
| error_message | text | |
| sent_at | timestamptz | |
| created_at | timestamptz | DEFAULT NOW() |

---

## 8. AUDIT & SYSTEM MODULE (4 tables)

### 8.1 `audit_logs`
| Column | Type | Constraints |
|--------|------|-------------|
| id | bigint | PK, GENERATED ALWAYS AS IDENTITY |
| user_id | uuid | FK→users |
| user_email | varchar(255) | |
| action | varchar(50) | NOT NULL | Login/Logout/Create/Update/Delete/Approve/Reject/Export/AIQuery |
| entity_type | varchar(100) | |
| entity_id | uuid | |
| entity_name | varchar(300) | |
| old_values | jsonb | |
| new_values | jsonb | |
| changes_summary | text | |
| ip_address | varchar(45) | |
| user_agent | varchar(500) | |
| request_path | varchar(500) | |
| request_method | varchar(10) | |
| response_status | int | |
| duration_ms | int | |
| created_at | timestamptz | DEFAULT NOW() |

**Indexes**: `IX_audit_user_created`, `IX_audit_entity`, `IX_audit_action_created`  
**Partitioning**: Partition by month on `created_at` for performance

### 8.2 `system_settings`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| company_id | uuid | FK→companies |
| key | varchar(100) | NOT NULL |
| value | text | NOT NULL |
| value_type | varchar(20) | DEFAULT 'string' | string/number/boolean/json |
| category | varchar(50) | |
| description | text | |
| is_sensitive | boolean | DEFAULT false |
| updated_by | uuid | FK→users |
| updated_at | timestamptz | DEFAULT NOW() |

**Unique**: `(company_id, key)`

### 8.3 `file_uploads`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| file_name | varchar(300) | NOT NULL |
| original_name | varchar(300) | NOT NULL |
| file_path | varchar(500) | NOT NULL |
| file_size | bigint | NOT NULL |
| content_type | varchar(100) | NOT NULL |
| entity_type | varchar(50) | |
| entity_id | uuid | |
| uploaded_by | uuid | FK→users |
| is_public | boolean | DEFAULT false |
| created_at | timestamptz | DEFAULT NOW() |
| is_deleted | boolean | DEFAULT false |

### 8.4 `background_jobs`
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| job_type | varchar(100) | NOT NULL | EmbeddingSync/ReportGeneration/DataCleanup/EmailDigest |
| status | varchar(20) | DEFAULT 'Pending' | Pending/Running/Completed/Failed |
| input_data | jsonb | |
| output_data | jsonb | |
| error_message | text | |
| started_at | timestamptz | |
| completed_at | timestamptz | |
| created_at | timestamptz | DEFAULT NOW() |
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
