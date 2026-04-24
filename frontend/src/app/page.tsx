"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { askAi, createPaymentRequest, getApprovalQueue, getBudgets, getDashboard, login } from "@/lib/api";
import type { ApprovalQueueItem, Budget, DashboardOverview, LoginResponse } from "@/types/api";

const currency = new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 });
const emptyGuid = "00000000-0000-0000-0000-000000000000";

type SectionKey = "dashboard" | "finance" | "performance" | "workflow" | "ai";

const sections: Array<{ key: SectionKey; label: string; title: string }> = [
  { key: "dashboard", label: "Dashboard", title: "Dashboard dieu hanh" },
  { key: "finance", label: "Finance", title: "Tai chinh va de nghi chi" },
  { key: "performance", label: "KPI/OKR", title: "KPI/OKR" },
  { key: "workflow", label: "Workflow", title: "Hang doi phe duyet" },
  { key: "ai", label: "AI Copilot", title: "AI Copilot" }
];

export default function HomePage() {
  const [activeSection, setActiveSection] = useState<SectionKey>("dashboard");
  const [auth, setAuth] = useState<LoginResponse | null>(null);
  const [dashboard, setDashboard] = useState<DashboardOverview | null>(null);
  const [budgets, setBudgets] = useState<Budget[]>([]);
  const [approvalQueue, setApprovalQueue] = useState<ApprovalQueueItem[]>([]);
  const [email, setEmail] = useState("director@omnibiz.ai");
  const [password, setPassword] = useState("Test@123456");
  const [message, setMessage] = useState("");
  const [aiAnswer, setAiAnswer] = useState("");
  const [status, setStatus] = useState("");
  const [paymentAmount, setPaymentAmount] = useState(15000000);
  const [isLoading, setIsLoading] = useState(false);

  const token = auth?.accessToken;
  const canUseWorkspace = Boolean(token);
  const activeTitle = sections.find((section) => section.key === activeSection)?.title ?? "Dashboard dieu hanh";
  const primaryBudget = budgets[0];

  useEffect(() => {
    const stored = localStorage.getItem("omnibiz-auth");
    if (stored) {
      setAuth(JSON.parse(stored) as LoginResponse);
    }
  }, []);

  useEffect(() => {
    if (!token) return;
    void refreshWorkspace(token);
  }, [token]);

  const netCash = useMemo(() => {
    if (!dashboard) return 0;
    return dashboard.totalIncome - dashboard.totalExpense;
  }, [dashboard]);

  async function refreshWorkspace(authToken = token) {
    if (!authToken) return;
    setIsLoading(true);
    try {
      const [dashboardResult, budgetResult, approvalResult] = await Promise.all([
        getDashboard(authToken),
        getBudgets(authToken),
        getApprovalQueue(authToken).catch(() => ({ items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 }))
      ]);
      setDashboard(dashboardResult);
      setBudgets(budgetResult.items);
      setApprovalQueue(approvalResult.items);
      setStatus("Du lieu da cap nhat");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Tai du lieu that bai");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setStatus("Dang dang nhap...");
    try {
      const result = await login(email, password);
      setAuth(result);
      localStorage.setItem("omnibiz-auth", JSON.stringify(result));
      setStatus("Dang nhap thanh cong");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Dang nhap that bai");
    }
  }

  function handleLogout() {
    localStorage.removeItem("omnibiz-auth");
    setAuth(null);
    setDashboard(null);
    setBudgets([]);
    setApprovalQueue([]);
    setActiveSection("dashboard");
    setStatus("Da dang xuat");
  }

  async function handleAskAi(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!token || !message.trim()) return;
    setStatus("AI dang phan tich...");
    try {
      const result = await askAi(token, message);
      setAiAnswer(result.content);
      setStatus("AI da tra loi");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "AI request failed");
    }
  }

  async function handleCreatePaymentRequest(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!token || !auth) return;

    setStatus("Dang tao de nghi chi...");
    try {
      await createPaymentRequest(token, {
        title: "De nghi thanh toan tu frontend",
        description: "Khoan chi duoc tao tu OmniBiz AI web client",
        departmentId: primaryBudget?.departmentId ?? auth.user.departmentId ?? emptyGuid,
        requesterId: auth.user.id,
        budgetId: primaryBudget?.id,
        categoryId: primaryBudget?.categoryId ?? emptyGuid,
        currency: "VND",
        priority: "Normal",
        items: [{ description: "Demo service", quantity: 1, unit: "lot", unitPrice: paymentAmount, totalPrice: paymentAmount }]
      });
      setStatus("Da tao de nghi chi");
      await refreshWorkspace(token);
      setActiveSection("workflow");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Tao de nghi chi that bai");
    }
  }

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <div className="brand">OmniBiz AI</div>
        <nav aria-label="Main navigation">
          {sections.map((section) => (
            <button
              key={section.key}
              className={section.key === activeSection ? "nav-item active" : "nav-item"}
              type="button"
              onClick={() => setActiveSection(section.key)}
              disabled={!canUseWorkspace}
            >
              {section.label}
            </button>
          ))}
        </nav>
        {auth && (
          <div className="sidebar-footer">
            <span>{auth.user.fullName}</span>
            <small>{auth.user.roles.join(", ")}</small>
            <button className="ghost-button" type="button" onClick={handleLogout}>Dang xuat</button>
          </div>
        )}
      </aside>

      <section className="workspace">
        <header className="topbar">
          <div>
            <p className="eyebrow">SME Operating System</p>
            <h1>{activeTitle}</h1>
          </div>
          <div className="topbar-actions">
            {canUseWorkspace && <button className="secondary-button" type="button" onClick={() => void refreshWorkspace()} disabled={isLoading}>Lam moi</button>}
            <span className="status">{isLoading ? "Dang tai..." : status || "San sang"}</span>
          </div>
        </header>

        {!canUseWorkspace ? (
          <form className="panel login" onSubmit={handleLogin}>
            <h2>Dang nhap</h2>
            <label>Email<input value={email} onChange={(event) => setEmail(event.target.value)} /></label>
            <label>Mat khau<input type="password" value={password} onChange={(event) => setPassword(event.target.value)} /></label>
            <button type="submit">Dang nhap</button>
          </form>
        ) : (
          <>
            {activeSection === "dashboard" && <DashboardSection dashboard={dashboard} netCash={netCash} />}
            {activeSection === "finance" && (
              <FinanceSection
                budgets={budgets}
                paymentAmount={paymentAmount}
                onPaymentAmountChange={setPaymentAmount}
                onCreatePaymentRequest={handleCreatePaymentRequest}
              />
            )}
            {activeSection === "performance" && <PerformanceSection dashboard={dashboard} />}
            {activeSection === "workflow" && <WorkflowSection items={approvalQueue} />}
            {activeSection === "ai" && (
              <AiSection
                message={message}
                aiAnswer={aiAnswer}
                onMessageChange={setMessage}
                onAskAi={handleAskAi}
              />
            )}
          </>
        )}
      </section>
    </main>
  );
}

function DashboardSection({ dashboard, netCash }: { dashboard: DashboardOverview | null; netCash: number }) {
  return (
    <>
      <section className="metrics">
        <Metric title="Tong thu" value={currency.format(dashboard?.totalIncome ?? 0)} />
        <Metric title="Tong chi" value={currency.format(dashboard?.totalExpense ?? 0)} />
        <Metric title="Dong tien rong" value={currency.format(netCash)} />
        <Metric title="KPI trung binh" value={`${dashboard?.averageKpiProgress ?? 0}%`} />
        <Metric title="Cho duyet" value={`${dashboard?.pendingApprovals ?? 0}`} />
      </section>
      <section className="grid">
        <div className="panel">
          <h2>Canh bao rui ro</h2>
          <ul className="risk-list">
            {(dashboard?.riskAlerts.length ? dashboard.riskAlerts : ["Chua co canh bao moi"]).map((risk) => <li key={risk}>{risk}</li>)}
          </ul>
        </div>
      </section>
    </>
  );
}

function FinanceSection({
  budgets,
  paymentAmount,
  onPaymentAmountChange,
  onCreatePaymentRequest
}: {
  budgets: Budget[];
  paymentAmount: number;
  onPaymentAmountChange: (value: number) => void;
  onCreatePaymentRequest: (event: FormEvent<HTMLFormElement>) => void;
}) {
  return (
    <section className="grid">
      <div className="panel">
        <h2>Ngan sach dang theo doi</h2>
        <div className="table-list">
          {budgets.length === 0 ? (
            <p className="empty-state">Chua co budget nao duoc seed.</p>
          ) : budgets.map((budget) => (
            <article className="list-row" key={budget.id}>
              <div>
                <strong>{budget.name}</strong>
                <span>{budget.warningLevel} - {budget.status}</span>
              </div>
              <div className="row-value">
                <strong>{currency.format(budget.remainingAmount)}</strong>
                <span>con lai</span>
              </div>
            </article>
          ))}
        </div>
      </div>

      <form className="panel" onSubmit={onCreatePaymentRequest}>
        <h2>Tao de nghi chi nhanh</h2>
        <label>So tien<input type="number" min="1000" value={paymentAmount} onChange={(event) => onPaymentAmountChange(Number(event.target.value))} /></label>
        <button type="submit">Tao de nghi chi</button>
        <p className="helper-text">Form tu dong dung budget/category seed dau tien neu co.</p>
      </form>
    </section>
  );
}

function PerformanceSection({ dashboard }: { dashboard: DashboardOverview | null }) {
  return (
    <section className="metrics compact">
      <Metric title="KPI trung binh" value={`${dashboard?.averageKpiProgress ?? 0}%`} />
      <Metric title="Trang thai OKR" value="Dang theo doi" />
      <Metric title="Can xu ly" value={`${dashboard?.riskAlerts.length ?? 0}`} />
    </section>
  );
}

function WorkflowSection({ items }: { items: ApprovalQueueItem[] }) {
  return (
    <section className="panel">
      <h2>Approval queue</h2>
      <div className="table-list">
        {items.length === 0 ? (
          <p className="empty-state">Khong co workflow nao dang cho duyet.</p>
        ) : items.map((item) => (
          <article className="list-row" key={item.instanceId}>
            <div>
              <strong>{item.entityType}</strong>
              <span>Entity: {item.entityId}</span>
            </div>
            <div className="row-value">
              <strong>{item.status}</strong>
              <span>Step {item.currentStepOrder}</span>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}

function AiSection({
  message,
  aiAnswer,
  onMessageChange,
  onAskAi
}: {
  message: string;
  aiAnswer: string;
  onMessageChange: (value: string) => void;
  onAskAi: (event: FormEvent<HTMLFormElement>) => void;
}) {
  return (
    <form className="panel ai-panel" onSubmit={onAskAi}>
      <h2>Hoi AI ve du lieu van hanh</h2>
      <textarea value={message} onChange={(event) => onMessageChange(event.target.value)} placeholder="Hoi: Phong nao vuot ngan sach?" />
      <button type="submit">Gui cau hoi</button>
      {aiAnswer && <pre>{aiAnswer}</pre>}
    </form>
  );
}

function Metric({ title, value }: { title: string; value: string }) {
  return (
    <div className="metric">
      <span>{title}</span>
      <strong>{value}</strong>
    </div>
  );
}
