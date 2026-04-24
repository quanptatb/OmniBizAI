"use client";

import { Bell, Search, Menu } from "lucide-react";

export function Header() {
  return (
    <header className="h-[68px] bg-white border-b border-border flex items-center justify-between px-8 shrink-0 z-10 sticky top-0 shadow-[0_2px_8px_rgba(26,115,232,0.04)]">
      <div className="flex items-center gap-5 flex-1">
        <button className="w-10 h-10 border-none bg-primary-light rounded-md flex items-center justify-center text-primary cursor-pointer transition-all hover:bg-primary hover:text-white hover:shadow-[0_4px_12px_rgba(26,115,232,0.25)] lg:hidden">
          <Menu size={20} />
        </button>
        <div className="relative w-full max-w-[320px]">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-text-muted" />
          <input 
            type="text" 
            placeholder="Tìm kiếm nhanh..." 
            className="w-full h-[42px] pl-11 pr-4 rounded-xl border-[1.5px] border-border text-[0.85rem] text-text-primary bg-[#f8fafe] transition-all outline-none focus:border-primary focus:bg-white focus:shadow-[0_0_0_3px_rgba(26,115,232,0.12)]"
          />
        </div>
      </div>
      
      <div className="flex items-center gap-3 ml-auto">
        <div className="relative">
          <button className="w-[42px] h-[42px] border-none bg-primary-light rounded-md flex items-center justify-center text-primary cursor-pointer transition-all hover:bg-primary hover:text-white hover:shadow-[0_4px_12px_rgba(26,115,232,0.25)]">
            <Bell size={20} />
          </button>
          <span className="absolute -top-1.5 -right-1.5 min-w-[18px] h-[18px] px-1 rounded-full bg-gradient-to-br from-danger to-[#ea4335] text-white text-[10px] font-extrabold leading-[14px] text-center border-2 border-white shadow-[0_2px_6px_rgba(217,48,37,0.35)]">
            3
          </span>
        </div>
      </div>
    </header>
  );
}
