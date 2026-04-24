import { ReactNode } from "react";
import { cn } from "@/lib/utils";

interface StatCardProps {
  title: string;
  value: string | number;
  icon?: ReactNode;
  trend?: {
    value: number;
    isPositive: boolean;
  };
  className?: string;
}

export function StatCard({ title, value, icon, trend, className }: StatCardProps) {
  return (
    <div className={cn(
      "bg-white p-5 rounded-[14px] border border-border shadow-[0_1px_4px_rgba(26,115,232,0.06),0_1px_2px_rgba(0,0,0,0.04)] hover:shadow-[0_8px_28px_rgba(26,115,232,0.12)] transition-shadow duration-300", 
      className
    )}>
      <div className="flex items-center justify-between mb-3">
        <h3 className="text-[0.85rem] font-semibold text-text-secondary uppercase tracking-wide">{title}</h3>
        {icon && <div className="w-10 h-10 rounded-lg bg-primary-bg text-primary flex items-center justify-center">{icon}</div>}
      </div>
      <div className="flex items-baseline gap-2">
        <span className="text-[1.6rem] font-extrabold text-text-primary tracking-tight">{value}</span>
      </div>
      {trend && (
        <div className="mt-3 flex items-center text-[0.75rem]">
          <span className={cn(
            "inline-flex items-center px-1.5 py-0.5 rounded-md font-bold",
            trend.isPositive ? "bg-success-bg text-success" : "bg-danger-bg text-danger"
          )}>
            {trend.isPositive ? "+" : "-"}{Math.abs(trend.value)}%
          </span>
          <span className="text-text-muted ml-2 font-medium">vs last month</span>
        </div>
      )}
    </div>
  );
}
