// ═══ OmniBizAI — Apple Design System JS ═════════════════════════════

document.addEventListener('DOMContentLoaded', function () {
    // ── Sidebar toggle ──────────────────────────────────────────
    const sidebar = document.getElementById('sidebar');
    const toggle = document.getElementById('sidebarToggle');
    if (toggle && sidebar) {
        toggle.addEventListener('click', () => sidebar.classList.toggle('open'));
        document.addEventListener('click', (e) => {
            if (window.innerWidth < 992 && sidebar.classList.contains('open') &&
                !sidebar.contains(e.target) && !toggle.contains(e.target)) {
                sidebar.classList.remove('open');
            }
        });
    }


    // ── Sidebar nav group toggles ──────────────────────────────
    document.querySelectorAll('[data-nav-group-toggle]').forEach(btn => {
        btn.addEventListener('click', () => {
            const targetId = btn.getAttribute('data-target');
            const panel = document.getElementById(targetId);
            if (!panel) return;
            const isOpen = panel.classList.toggle('open');
            btn.classList.toggle('expanded', isOpen);
            btn.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
        });
    });

    // ── Auto-dismiss toasts ─────────────────────────────────────
    document.querySelectorAll('.alert-toast').forEach(toast => {
        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(60px)';
            toast.style.transition = '.35s ease';
            setTimeout(() => toast.remove(), 350);
        }, 5000);
    });

    // ── showAppToast (global) ───────────────────────────────────
    window.showAppToast = function(opts) {
        const { message, tone = 'info', title } = opts || {};
        if (!message) return;
        const icons = { success: 'fa-circle-check', error: 'fa-circle-xmark', warning: 'fa-triangle-exclamation', info: 'fa-circle-info' };
        const toast = document.createElement('div');
        toast.className = `alert-toast ${tone}`;
        toast.innerHTML = `<i class="fa-solid ${icons[tone] || icons.info}"></i><span>${title ? '<strong>' + title + '</strong> — ' : ''}${message}</span><button class="toast-close" onclick="this.parentElement.remove()">&times;</button>`;
        document.body.appendChild(toast);
        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(60px)';
            toast.style.transition = '.35s ease';
            setTimeout(() => toast.remove(), 350);
        }, 5000);
    };

    // ── Page content fade-in ────────────────────────────────────
    document.querySelectorAll('.glass-card,.content-card,.stat-card').forEach((el, i) => {
        el.classList.add('fade-in');
        if (i < 12) el.style.animationDelay = `${i * 0.05}s`;
    });

    // ── Button press effect (Apple scale 0.97) ──────────────────
    document.querySelectorAll('.btn,.btn-primary-custom,.btn-outline-custom,.action-btn').forEach(btn => {
        btn.addEventListener('mousedown', () => btn.style.transform = 'scale(0.97)');
        btn.addEventListener('mouseup', () => btn.style.transform = '');
        btn.addEventListener('mouseleave', () => btn.style.transform = '');
    });

    // ── AI Chat panel logic ─────────────────────────────────────
    const aiForm = document.getElementById('aiAskForm');
    const aiAnswer = document.getElementById('aiAnswerArea');
    const aiLoading = document.getElementById('aiLoading');

    if (aiForm) {
        aiForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            const question = document.getElementById('aiQuestion')?.value;
            if (!question || question.trim().length < 5) {
                showAiError('Vui lòng nhập câu hỏi (tối thiểu 5 ký tự).');
                return;
            }

            if (aiLoading) aiLoading.style.display = 'flex';
            if (aiAnswer) aiAnswer.innerHTML = '';

            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                const resp = await fetch('/AiInsights/Ask', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token || '' },
                    body: JSON.stringify({ question: question, contextType: 'Dashboard', contextId: null })
                });

                if (aiLoading) aiLoading.style.display = 'none';

                if (!resp.ok) {
                    const err = await resp.json().catch(() => ({}));
                    showAiError(err.error || 'Đã xảy ra lỗi khi gọi AI.');
                    return;
                }

                const data = await resp.json();
                renderAiAnswer(data);
            } catch (ex) {
                if (aiLoading) aiLoading.style.display = 'none';
                showAiError('Hệ thống AI tạm thời không khả dụng.');
            }
        });
    }

    function showAiError(msg) {
        if (aiAnswer) {
            aiAnswer.innerHTML = `<div class="ai-answer-card" style="border-color:var(--danger)"><div class="d-flex align-items-center gap-2" style="color:var(--danger)"><i class="fa-solid fa-circle-xmark"></i><span>${msg}</span></div></div>`;
        }
    }

    function renderAiAnswer(data) {
        if (!aiAnswer) return;
        const riskClass = (data.riskLevel || 'Low').toLowerCase();
        const riskLabel = { low: 'Thấp', medium: 'Trung bình', high: 'Cao' };
        aiAnswer.innerHTML = `
            <div class="ai-answer-card">
                <div class="d-flex justify-content-between align-items-start mb-3">
                    <h6 style="font-weight:600;margin:0"><i class="fa-solid fa-robot me-2" style="color:var(--apple-blue)"></i>Kết quả phân tích</h6>
                    <span class="ai-risk-badge ${riskClass}"><i class="fa-solid fa-shield-halved"></i> Rủi ro: ${riskLabel[riskClass] || data.riskLevel}</span>
                </div>
                <div class="mb-3">
                    <label style="font-size:.7rem;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.04em">Tóm tắt</label>
                    <p style="margin:4px 0 0">${escapeHtml(data.summary || '')}</p>
                </div>
                ${data.recommendation ? `<div class="mb-3"><label style="font-size:.7rem;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.04em">Đề xuất</label><div style="padding:12px;background:var(--parchment);border-radius:var(--r-sm);margin-top:4px;font-size:.85rem;white-space:pre-line">${escapeHtml(data.recommendation)}</div></div>` : ''}
                <div class="text-end"><small style="color:var(--text-muted)"><i class="fa-regular fa-clock me-1"></i>${new Date(data.createdAt).toLocaleString('vi-VN')}</small></div>
            </div>`;
    }

    function escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    // ── Approval modal ──────────────────────────────────────────
    window.openApprovalModal = function (taskId, action) {
        const modal = document.getElementById('approvalModal');
        if (!modal) return;
        document.getElementById('modalTaskId').value = taskId;
        document.getElementById('modalAction').value = action;
        document.getElementById('modalTitle').textContent = action === 'Approve' ? 'Phê duyệt yêu cầu' : 'Từ chối yêu cầu';
        const noteGroup = document.getElementById('modalNoteGroup');
        if (noteGroup) noteGroup.classList.toggle('d-none', action === 'Approve');
        const noteInput = document.getElementById('modalNote');
        if (noteInput) noteInput.required = action !== 'Approve';
        const btn = document.getElementById('modalSubmitBtn');
        if (btn) {
            btn.textContent = action === 'Approve' ? 'Xác nhận duyệt' : 'Xác nhận từ chối';
            btn.className = action === 'Approve' ? 'btn btn-success' : 'btn btn-danger';
        }
        new bootstrap.Modal(modal).show();
    };

    // ── Notification bell ────────────────────────────────────────
    pollNotifCount();
    setInterval(pollNotifCount, 30000); // Poll every 30s

    // Close panel when clicking outside
    document.addEventListener('click', (e) => {
        const panel = document.getElementById('notifPanel');
        const btn = document.getElementById('notifBtn');
        if (panel && panel.style.display !== 'none' && !panel.contains(e.target) && btn && !btn.contains(e.target)) {
            panel.style.display = 'none';
        }
    });
});

// ═══ NOTIFICATION FUNCTIONS ═════════════════════════════════════════

function toggleNotifPanel() {
    const panel = document.getElementById('notifPanel');
    if (!panel) return;
    const isVisible = panel.style.display !== 'none';
    panel.style.display = isVisible ? 'none' : 'block';
    if (!isVisible) loadNotifPanel();
}

async function pollNotifCount() {
    try {
        const res = await fetch('/Notifications/UnreadCount');
        if (!res.ok) return;
        const data = await res.json();
        const badge = document.getElementById('notifCountBadge');
        const dot = document.getElementById('notifDot');
        if (badge) {
            if (data.count > 0) {
                badge.textContent = data.count > 99 ? '99+' : data.count;
                badge.style.display = 'flex';
            } else {
                badge.style.display = 'none';
            }
        }
        if (dot) dot.style.display = data.count > 0 ? 'block' : 'none';
    } catch (e) { /* silent */ }
}

async function loadNotifPanel() {
    const body = document.getElementById('notifPanelBody');
    if (!body) return;
    try {
        const res = await fetch('/Notifications/Recent');
        if (!res.ok) throw new Error();
        const data = await res.json();
        if (!data.items || data.items.length === 0) {
            body.innerHTML = '<div class="notif-panel-empty"><i class="fa-solid fa-bell-slash" style="font-size:1.4rem;opacity:.4;margin-bottom:8px"></i><div>Chưa có thông báo mới</div></div>';
            return;
        }
        const iconMap = { 'OperationRequest': 'fa-list-check', 'ApprovalTask': 'fa-circle-check', 'Budget': 'fa-piggy-bank', 'Expense': 'fa-receipt', 'ProcurementRequest': 'fa-cart-shopping', 'PurchaseOrder': 'fa-file-invoice', 'Customer': 'fa-building', 'Vendor': 'fa-truck', 'Employee': 'fa-id-badge', 'KpiDefinition': 'fa-gauge-high', 'OkrObjective': 'fa-bullseye', 'PaymentRequest': 'fa-file-invoice-dollar', 'AppUser': 'fa-user' };
        body.innerHTML = data.items.map(n => {
            const icon = iconMap[n.entityName] || 'fa-bell';
            const readCls = n.isRead ? 'read' : 'unread';
            const ago = formatTimeAgo(n.createdAt);
            return `<div class="notif-panel-item ${readCls}" onclick="markNotifRead('${n.deliveryId}', this)">
                <div class="notif-panel-icon ${readCls}"><i class="fa-solid ${icon}"></i></div>
                <div class="notif-panel-content">
                    <div class="notif-panel-title ${readCls}">${escNotif(n.title)}</div>
                    <div class="notif-panel-time">${escNotif(n.senderName)} · ${ago}</div>
                </div>
            </div>`;
        }).join('');
    } catch (e) {
        body.innerHTML = '<div class="notif-panel-empty">Không thể tải thông báo</div>';
    }
}

async function markNotifRead(id, el) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    try {
        await fetch('/Notifications/MarkRead?id=' + id, {
            method: 'POST',
            headers: { 'RequestVerificationToken': token }
        });
        if (el) { el.classList.remove('unread'); el.classList.add('read'); }
        pollNotifCount();
    } catch (e) { /* silent */ }
}

async function markAllNotifRead() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    try {
        await fetch('/Notifications/MarkAllRead', { method: 'POST', headers: { 'RequestVerificationToken': token } });
        document.querySelectorAll('.notif-panel-item.unread').forEach(el => { el.classList.remove('unread'); el.classList.add('read'); });
        pollNotifCount();
    } catch (e) { /* silent */ }
}

function formatTimeAgo(dateStr) {
    const d = new Date(dateStr);
    const diff = (Date.now() - d.getTime()) / 1000;
    if (diff < 60) return 'Vừa xong';
    if (diff < 3600) return Math.floor(diff / 60) + ' phút';
    if (diff < 86400) return Math.floor(diff / 3600) + ' giờ';
    if (diff < 604800) return Math.floor(diff / 86400) + ' ngày';
    return d.toLocaleDateString('vi-VN');
}

function escNotif(s) { if (!s) return ''; const d = document.createElement('div'); d.textContent = s; return d.innerHTML; }

