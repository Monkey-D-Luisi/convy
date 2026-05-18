import type { Metadata } from "next";
import type { ReactNode } from "react";
import "./globals.css";
import { AdminShell } from "@/components/admin-shell";

export const metadata: Metadata = {
  title: "Convy Admin",
  description: "Convy operational dashboard",
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="en">
      <body>
        <AdminShell>{children}</AdminShell>
      </body>
    </html>
  );
}
