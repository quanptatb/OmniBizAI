document.addEventListener("DOMContentLoaded", () => {
    const toggle = document.getElementById("sidebarToggle");
    const scrim = document.getElementById("mobileScrim");

    toggle?.addEventListener("click", () => document.body.classList.toggle("sidebar-open"));
    scrim?.addEventListener("click", () => document.body.classList.remove("sidebar-open"));

    document.querySelectorAll("[data-demo-toast]").forEach((button) => {
        button.addEventListener("click", () => {
            const original = button.innerHTML;
            button.classList.add("disabled");
            button.innerHTML = '<i class="bi bi-check2-circle"></i> Đã lưu thành công';
            window.setTimeout(() => {
                button.classList.remove("disabled");
                button.innerHTML = original;
                // Auto close modal if it's inside one
                const modal = button.closest(".modal-overlay");
                if(modal) modal.classList.remove("show");
            }, 1000);
        });
    });

    // Modal Logic
    document.querySelectorAll("[data-modal-target]").forEach(btn => {
        btn.addEventListener("click", () => {
            const targetId = btn.getAttribute("data-modal-target");
            const modal = document.getElementById(targetId);
            if (modal) modal.classList.add("show");
        });
    });

    document.querySelectorAll("[data-modal-close]").forEach(btn => {
        btn.addEventListener("click", () => {
            const modal = btn.closest(".modal-overlay");
            if (modal) modal.classList.remove("show");
        });
    });

    // Close on overlay click
    document.querySelectorAll(".modal-overlay").forEach(overlay => {
        overlay.addEventListener("click", (e) => {
            if (e.target === overlay) {
                overlay.classList.remove("show");
            }
        });
    });
});
