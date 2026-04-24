"use client";

import { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { cn } from '@/lib/utils';
import { 
  LayoutDashboard, 
  Wallet, 
  Target, 
  GitMerge, 
  Users, 
  Bot, 
  Settings, 
  ChevronLeft, 
  ChevronRight,
  LogOut
} from 'lucide-react';
import { useAuthStore } from '@/stores/auth';

const navigation = [
  { name: 'Dashboard', href: '/', icon: LayoutDashboard },
  { name: 'Finance', href: '/finance/budgets', icon: Wallet },
  { name: 'KPI/OKR', href: '/performance/objectives', icon: Target },
  { name: 'Workflow', href: '/workflow/approvals', icon: GitMerge },
  { name: 'Organization', href: '/organization/departments', icon: Users },
  { name: 'AI Copilot', href: '/ai/copilot', icon: Bot },
];

export function Sidebar() {
  const pathname = usePathname();
  const [collapsed, setCollapsed] = useState(false);
  const { user, clearAuth } = useAuthStore();

  const handleLogout = () => {
    clearAuth();
    if (typeof window !== 'undefined') {
      window.location.href = '/login';
    }
  };

  return (
    <aside
      className={cn(
        "bg-[#0a0f1c] text-white transition-all duration-300 ease-in-out flex flex-col h-screen border-r border-slate-800",
        collapsed ? "w-[72px]" : "w-[260px]"
      )}
    >
      <div className="h-16 flex items-center px-4 justify-between border-b border-slate-800">
        {!collapsed && <span className="font-bold text-lg tracking-tight bg-gradient-to-r from-blue-400 to-indigo-400 bg-clip-text text-transparent">OmniBiz AI</span>}
        <button 
          onClick={() => setCollapsed(!collapsed)}
          className={cn("p-1.5 rounded-md hover:bg-slate-800 text-slate-400 hover:text-white transition-colors", collapsed && "mx-auto")}
        >
          {collapsed ? <ChevronRight size={18} /> : <ChevronLeft size={18} />}
        </button>
      </div>

      <div className="flex-1 overflow-y-auto py-4 flex flex-col gap-1 px-3">
        {navigation.map((item) => {
          const isActive = pathname === item.href || pathname.startsWith(item.href !== '/' ? item.href : '/___none');
          return (
            <Link
              key={item.name}
              href={item.href}
              className={cn(
                "flex items-center gap-3 px-3 py-2.5 rounded-lg transition-colors group",
                isActive 
                  ? "bg-blue-600/10 text-blue-400 font-medium" 
                  : "text-slate-400 hover:text-white hover:bg-slate-800/50"
              )}
              title={collapsed ? item.name : undefined}
            >
              <item.icon size={20} className={cn("shrink-0", isActive ? "text-blue-400" : "text-slate-400 group-hover:text-white")} />
              {!collapsed && <span>{item.name}</span>}
            </Link>
          );
        })}
      </div>

      {user && (
        <div className="p-4 border-t border-slate-800">
          <div className={cn("flex items-center", collapsed ? "justify-center" : "justify-between")}>
            {!collapsed && (
              <div className="flex flex-col truncate pr-2">
                <span className="text-sm font-medium truncate">{user.fullName}</span>
                <span className="text-xs text-slate-500 truncate">{user.roles.join(', ')}</span>
              </div>
            )}
            <button 
              onClick={handleLogout}
              className="p-2 text-slate-400 hover:text-red-400 hover:bg-slate-800 rounded-lg transition-colors shrink-0"
              title="Logout"
            >
              <LogOut size={18} />
            </button>
          </div>
        </div>
      )}
    </aside>
  );
}
