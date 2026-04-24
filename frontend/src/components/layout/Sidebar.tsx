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
  ChevronLeft, 
  ChevronRight,
  LogOut,
  Hexagon
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
        "bg-gradient-to-b from-bg-sidebar-start via-bg-sidebar-mid to-bg-sidebar-end text-white transition-all duration-300 ease-in-out flex flex-col h-screen shrink-0 shadow-[4px_0_24px_rgba(13,33,55,0.15)] z-50",
        collapsed ? "w-[80px]" : "w-[280px]"
      )}
    >
      <div className="h-[68px] flex items-center px-[24px] shrink-0 relative border-b border-white/10">
        <div className="w-[38px] h-[38px] bg-gradient-to-br from-[#1a73e8] to-[#4285f4] rounded-[10px] flex items-center justify-center text-white shadow-[0_4px_12px_rgba(26,115,232,0.35)] shrink-0 mr-[14px]">
          <Hexagon size={20} fill="currentColor" className="text-white/20" />
        </div>
        
        <div className={cn("flex flex-col overflow-hidden whitespace-nowrap transition-all duration-300", collapsed ? "opacity-0 w-0" : "opacity-100 flex-1")}>
          <span className="font-extrabold text-[1.05rem] tracking-tight">OmniBiz System</span>
          <span className="text-[0.7rem] text-white/80">Manage AI & Business</span>
        </div>

        <button 
          onClick={() => setCollapsed(!collapsed)}
          className={cn("absolute -right-3 top-1/2 -translate-y-1/2 w-6 h-6 bg-primary text-white rounded-full flex items-center justify-center shadow-md hover:bg-primary-dark transition-colors z-50", collapsed && "right-[-12px]")}
        >
          {collapsed ? <ChevronRight size={14} /> : <ChevronLeft size={14} />}
        </button>
      </div>

      <div className="flex-1 overflow-y-auto py-4 flex flex-col gap-[2px] px-0">
        <div className={cn("px-6 pb-2 pt-4 text-[0.7rem] font-extrabold text-white/75 uppercase tracking-[0.1em]", collapsed ? "hidden" : "block")}>
          Overview
        </div>
        {navigation.map((item) => {
          const isActive = pathname === item.href || pathname.startsWith(item.href !== '/' ? item.href : '/___none');
          return (
            <Link
              key={item.name}
              href={item.href}
              className={cn(
                "flex items-center mx-3 px-4 py-2.5 rounded-lg transition-all duration-150 group overflow-hidden whitespace-nowrap relative",
                isActive 
                  ? "bg-gradient-to-br from-[#1a73e84d] to-[#4285f433] text-white font-bold" 
                  : "text-white/80 hover:text-white hover:bg-[#1a73e833] hover:translate-x-1"
              )}
              title={collapsed ? item.name : undefined}
            >
              {isActive && (
                <div className="absolute left-0 top-0 bottom-0 w-[3px] bg-[#4285f4] rounded-r-sm"></div>
              )}
              <item.icon size={20} className={cn("shrink-0 transition-transform group-hover:scale-110", isActive ? "text-[#4285f4]" : "text-white/90")} />
              <span className={cn("ml-3 text-[0.85rem]", collapsed ? "hidden" : "block")}>{item.name}</span>
            </Link>
          );
        })}
      </div>

      {user && (
        <div className="p-4 mt-auto">
          <div className="flex items-center gap-3 p-2 rounded-xl hover:bg-white/10 transition-colors cursor-pointer group">
            <div className="w-9 h-9 rounded-full bg-gradient-to-tr from-[#1a73e8] to-[#0f9d58] text-white flex items-center justify-center font-bold shadow-sm shrink-0">
              {user.fullName?.[0]?.toUpperCase() || 'U'}
            </div>
            {!collapsed && (
              <div className="flex flex-col flex-1 min-w-0">
                <span className="text-sm font-medium truncate text-white">{user.fullName}</span>
                <span className="text-xs text-white/60 truncate">{user.roles.join(', ')}</span>
              </div>
            )}
            {!collapsed && (
               <button 
                onClick={handleLogout}
                className="p-1.5 text-white/40 hover:text-[#d93025] hover:bg-white/10 rounded-lg transition-colors shrink-0 opacity-0 group-hover:opacity-100"
                title="Logout"
              >
                <LogOut size={16} />
              </button>
            )}
          </div>
        </div>
      )}
    </aside>
  );
}
