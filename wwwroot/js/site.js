// ═══ OmniBizAI — Site JavaScript ═══════════════════════════════════

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

    // ── Auto-dismiss toast ──────────────────────────────────────
    const toast = document.getElementById('alertToast');
    if (toast) {
        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(60px)';
            toast.style.transition = '.4s ease';
            setTimeout(() => toast.remove(), 400);
        }, 5000);
    }

    // ── Page content fade-in ────────────────────────────────────
    document.querySelectorAll('.glass-card').forEach((el, i) => {
        el.classList.add('fade-in');
        if (i < 8) el.style.animationDelay = `${i * 0.06}s`;
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
                showAiError('Hệ thống AI tạm thời không khả dụng. Vui lòng thử lại sau.');
            }
        });
    }

    function showAiError(msg) {
        if (aiAnswer) {
            aiAnswer.innerHTML = `<div class="ai-answer-card border-danger"><div class="d-flex align-items-center gap-2 text-danger"><i class="fa-solid fa-circle-xmark"></i><span>${msg}</span></div></div>`;
        }
    }

    function renderAiAnswer(data) {
        if (!aiAnswer) return;
        const riskClass = (data.riskLevel || 'Low').toLowerCase();
        const riskLabel = { low: 'Thấp', medium: 'Trung bình', high: 'Cao' };
        aiAnswer.innerHTML = `
            <div class="ai-answer-card">
                <div class="d-flex justify-content-between align-items-start mb-3">
                    <h6 class="fw-bold mb-0"><i class="fa-solid fa-robot text-primary me-2"></i>Kết quả phân tích</h6>
                    <span class="ai-risk-badge ${riskClass}"><i class="fa-solid fa-shield-halved"></i> Rủi ro: ${riskLabel[riskClass] || data.riskLevel}</span>
                </div>
                <div class="mb-3">
                    <label class="text-muted small text-uppercase fw-bold mb-1">Tóm tắt</label>
                    <p class="mb-0">${escapeHtml(data.summary || '')}</p>
                </div>
                ${data.recommendation ? `<div class="mb-3"><label class="text-muted small text-uppercase fw-bold mb-1">Đề xuất hành động</label><div class="p-3 bg-light rounded border text-sm" style="white-space:pre-line;">${escapeHtml(data.recommendation)}</div></div>` : ''}
                <div class="text-end">
                    <small class="text-muted"><i class="fa-regular fa-clock me-1"></i>${new Date(data.createdAt).toLocaleString('vi-VN')}</small>
                </div>
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
