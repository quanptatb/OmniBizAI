"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/stores/auth";
import { useMutation } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type { LoginResponse } from "@/types/api";
import { Hexagon } from "lucide-react";

export default function LoginPage() {
  const [email, setEmail] = useState("director@omnibiz.ai");
  const [password, setPassword] = useState("Test@123456");
  const router = useRouter();
  const { setAuth } = useAuthStore();

  const loginMutation = useMutation({
    mutationFn: async () => {
      return apiClient.post<any, LoginResponse>("/auth/login", { email, password });
    },
    onSuccess: (data) => {
      setAuth(data);
      router.push("/");
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    loginMutation.mutate();
  };

  return (
    <div className="min-h-screen bg-bg-body flex items-center justify-center p-4 relative overflow-hidden">
      {/* Decorative blobs */}
      <div className="absolute top-[-150px] right-[-100px] w-[500px] h-[500px] rounded-full bg-[#f8bbd0] opacity-[0.15] blur-[80px]" />
      <div className="absolute bottom-[-100px] left-[-100px] w-[450px] h-[450px] rounded-full bg-[#8c6a85] opacity-[0.1] blur-[80px]" />

      <div className="max-w-[420px] w-full bg-white rounded-2xl shadow-[0_12px_32px_rgba(0,0,0,0.06),0_0_0_1px_rgba(0,0,0,0.04)] overflow-hidden relative z-10">
        <div className="p-10">
          <div className="mb-10 text-center flex flex-col items-center">
            <div className="w-[48px] h-[48px] bg-gradient-to-br from-[#1a73e8] to-[#4285f4] rounded-[12px] flex items-center justify-center text-white shadow-[0_6px_16px_rgba(26,115,232,0.35)] mb-4">
              <Hexagon size={26} fill="currentColor" className="text-white/20" />
            </div>
            <h1 className="text-[1.6rem] font-extrabold text-text-primary tracking-tight">Welcome Back</h1>
            <p className="text-[0.85rem] text-text-muted mt-1">Sign in to your workspace</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-5">
            <div>
              <label className="block text-[0.85rem] font-semibold text-text-primary mb-1.5">Email address</label>
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full px-4 py-[11px] rounded-[10px] border-[1.5px] border-border focus:border-primary focus:bg-white bg-[#f8fafe] transition-all outline-none text-[0.95rem] text-text-primary shadow-[0_1px_2px_rgba(0,0,0,0.02)] focus:shadow-[0_0_0_3px_rgba(26,115,232,0.12)]"
                placeholder="name@company.com"
              />
            </div>

            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label className="block text-[0.85rem] font-semibold text-text-primary">Password</label>
                <a href="#" className="text-[0.8rem] font-medium text-primary hover:text-primary-dark transition-colors">Forgot?</a>
              </div>
              <input
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full px-4 py-[11px] rounded-[10px] border-[1.5px] border-border focus:border-primary focus:bg-white bg-[#f8fafe] transition-all outline-none text-[0.95rem] text-text-primary shadow-[0_1px_2px_rgba(0,0,0,0.02)] focus:shadow-[0_0_0_3px_rgba(26,115,232,0.12)]"
                placeholder="••••••••"
              />
            </div>

            {loginMutation.error && (
              <div className="p-3 bg-danger-bg text-danger text-[0.85rem] font-medium rounded-lg border border-danger/20">
                {(loginMutation.error as any).message || "Invalid email or password"}
              </div>
            )}

            <button
              type="submit"
              disabled={loginMutation.isPending}
              className="w-full py-[12px] px-4 bg-gradient-to-br from-[#1a73e8] to-[#4285f4] hover:opacity-90 hover:-translate-y-[1px] text-white font-bold text-[0.95rem] rounded-[10px] shadow-[0_4px_12px_rgba(26,115,232,0.3)] transition-all focus:ring-4 focus:ring-primary/20 disabled:opacity-70 disabled:cursor-not-allowed disabled:transform-none"
            >
              {loginMutation.isPending ? "Signing in..." : "Sign in"}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
