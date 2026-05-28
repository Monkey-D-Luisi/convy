"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Auth,
  User,
  onAuthStateChanged,
  signInWithEmailAndPassword,
  signOut,
} from "firebase/auth";
import {
  FormEvent,
  ReactNode,
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import {
  FirebaseRuntimeConfig,
  getFirebaseAuth,
  isFirebaseConfigured,
} from "@/lib/firebase";

type AdminAuthContextValue = {
  token: string;
};

const AdminAuthContext = createContext<AdminAuthContextValue | null>(null);

const navItems = [
  { href: "/", label: "Overview" },
  { href: "/usage", label: "Usage" },
  { href: "/openai", label: "AI" },
  { href: "/mcp", label: "MCP" },
  { href: "/backups", label: "Backups" },
  { href: "/system", label: "System" },
];

export function useAdminToken() {
  const context = useContext(AdminAuthContext);
  if (!context) {
    throw new Error("useAdminToken must be used inside AdminShell.");
  }

  return context.token;
}

export function AdminShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const [dashboardAuth, setDashboardAuth] = useState<Auth | null>(null);
  const [firebaseConfigured, setFirebaseConfigured] = useState(true);
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState("");
  const [loading, setLoading] = useState(true);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [authError, setAuthError] = useState("");

  useEffect(() => {
    let unsubscribe: (() => void) | undefined;
    let cancelled = false;

    async function configureAuth() {
      try {
        const response = await fetch("/api/config/firebase", { cache: "no-store" });
        const config = (await response.json()) as FirebaseRuntimeConfig;

        if (!isFirebaseConfigured(config)) {
          setFirebaseConfigured(false);
          setLoading(false);
          return;
        }

        const nextAuth = getFirebaseAuth(config);
        setDashboardAuth(nextAuth);
        unsubscribe = onAuthStateChanged(nextAuth, async (nextUser) => {
          if (cancelled) {
            return;
          }
          setUser(nextUser);
          setToken(nextUser ? await nextUser.getIdToken() : "");
          setLoading(false);
        });
      } catch (error) {
        if (!cancelled) {
          setAuthError(error instanceof Error ? error.message : "Firebase configuration failed.");
          setFirebaseConfigured(false);
          setLoading(false);
        }
      }
    }

    void configureAuth();

    return () => {
      cancelled = true;
      unsubscribe?.();
    };
  }, []);

  useEffect(() => {
    if (!dashboardAuth || !user) {
      return;
    }

    const id = window.setInterval(async () => {
      setToken(await user.getIdToken(true));
    }, 10 * 60 * 1000);

    return () => window.clearInterval(id);
  }, [dashboardAuth, user]);

  const contextValue = useMemo(() => ({ token }), [token]);

  async function handleSignIn(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!dashboardAuth) {
      return;
    }

    setAuthError("");
    try {
      await signInWithEmailAndPassword(dashboardAuth, email.trim(), password);
    } catch (error) {
      setAuthError(error instanceof Error ? error.message : "Authentication failed.");
    }
  }

  if (!firebaseConfigured) {
    return (
      <main className="grid min-h-screen place-items-center px-6">
        <section className="w-full max-w-md rounded-lg border border-line bg-white p-8 shadow-sm">
          <h1 className="text-2xl font-semibold text-ink">Convy Admin</h1>
          <p className="mt-3 text-sm leading-6 text-muted">Firebase dashboard configuration is missing.</p>
        </section>
      </main>
    );
  }

  if (loading) {
    return (
      <main className="grid min-h-screen place-items-center px-6">
        <div className="h-10 w-10 rounded-full border-4 border-line border-t-brand" aria-label="Loading" />
      </main>
    );
  }

  if (!user) {
    return (
      <main className="grid min-h-screen place-items-center px-6">
        <form className="w-full max-w-sm rounded-lg border border-line bg-white p-8 shadow-sm" onSubmit={handleSignIn}>
          <h1 className="text-2xl font-semibold text-ink">Convy Admin</h1>
          <label className="mt-6 block text-sm font-medium text-ink" htmlFor="email">
            Email
          </label>
          <input
            id="email"
            className="mt-2 h-11 w-full rounded-md border border-line px-3 outline-none focus:border-brand"
            autoComplete="email"
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
          />
          <label className="mt-4 block text-sm font-medium text-ink" htmlFor="password">
            Password
          </label>
          <input
            id="password"
            className="mt-2 h-11 w-full rounded-md border border-line px-3 outline-none focus:border-brand"
            autoComplete="current-password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
          {authError ? <p className="mt-4 text-sm text-red-700">{authError}</p> : null}
          <button className="mt-6 h-11 w-full rounded-md bg-brand px-4 font-semibold text-white" type="submit">
            Sign in
          </button>
        </form>
      </main>
    );
  }

  return (
    <AdminAuthContext.Provider value={contextValue}>
      <div className="min-h-screen bg-surface">
        <header className="border-b border-line bg-white">
          <div className="mx-auto flex w-full max-w-7xl flex-col gap-4 px-4 py-4 sm:px-6 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <p className="text-sm font-medium text-muted">Convy</p>
              <h1 className="text-2xl font-semibold text-ink">Admin</h1>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <nav className="flex flex-wrap gap-2" aria-label="Dashboard views">
                {navItems.map((item) => {
                  const active = pathname === item.href;
                  return (
                    <Link
                      className={`rounded-md px-3 py-2 text-sm font-semibold ${
                        active ? "bg-brand text-white" : "border border-line bg-white text-ink"
                      }`}
                      href={item.href}
                      key={item.href}
                    >
                      {item.label}
                    </Link>
                  );
                })}
              </nav>
              <button
                className="rounded-md border border-line bg-white px-3 py-2 text-sm font-semibold text-ink"
                onClick={() => dashboardAuth && signOut(dashboardAuth)}
                type="button"
              >
                Sign out
              </button>
            </div>
          </div>
        </header>
        <main className="mx-auto w-full max-w-7xl px-4 py-6 sm:px-6">
          {token ? children : <div className="h-10 w-10 rounded-full border-4 border-line border-t-brand" />}
        </main>
      </div>
    </AdminAuthContext.Provider>
  );
}
