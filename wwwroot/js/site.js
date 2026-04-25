document.addEventListener("DOMContentLoaded", () => {
    const toggle = document.getElementById("sidebarToggle");
    const scrim = document.getElementById("mobileScrim");

    toggle?.addEventListener("click", () => document.body.classList.toggle("sidebar-open"));
    scrim?.addEventListener("click", () => document.body.classList.remove("sidebar-open"));

    document.querySelectorAll("[data-demo-toast]").forEach((button) => {
        button.addEventListener("click", () => {
            const original = button.innerHTML;
            button.classList.add("disabled");
            button.innerHTML = '<i class="bi bi-check2-circle"></i> Đã ghi nhận';
            window.setTimeout(() => {
                button.classList.remove("disabled");
                button.innerHTML = original;
            }, 1400);
        });
    });
});
