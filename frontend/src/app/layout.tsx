import type { Metadata } from "next";
import type { ReactNode } from "react";
import "./globals.css";
import { Providers } from "./providers";

export const metadata: Metadata = {
  title: "OmniBiz AI",
  description: "AI Business Operating System for SME decision-making"
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="vi" className="light">
      <body>
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
