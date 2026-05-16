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
});
