"use client";

import {
  GoogleAuthProvider,
  type Auth,
  type User,
  onAuthStateChanged,
  signInWithEmailAndPassword,
  signInWithPopup,
  signOut,
} from "firebase/auth";
import { FormEvent, Suspense, useEffect, useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";
import {
  type FirebaseRuntimeConfig,
  getFirebaseAuth,
  isFirebaseConfigured,
} from "@/lib/firebase";

const readOnlyScopes = [
  "convy.households.read",
  "convy.lists.read",
  "convy.items.read",
  "convy.tasks.read",
  "convy.activity.read",
];
const writeScopes = [
  "convy.items.write",
  "convy.tasks.write",
];
const supportedScopes = [...readOnlyScopes, ...writeScopes];

export function AuthorizeClient() {
  return (
    <Suspense fallback={<LoadingView />}>
      <AuthorizeContent />
    </Suspense>
  );
}

function AuthorizeContent() {
  const searchParams = useSearchParams();
  const [auth, setAuth] = useState<Auth | null>(null);
  const [firebaseConfigured, setFirebaseConfigured] = useState(true);
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [authError, setAuthError] = useState("");
  const [approvalError, setApprovalError] = useState("");
  const [approving, setApproving] = useState(false);

  const request = useMemo(() => parseOAuthRequest(searchParams), [searchParams]);

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
        setAuth(nextAuth);
        unsubscribe = onAuthStateChanged(nextAuth, (nextUser) => {
          if (!cancelled) {
            setUser(nextUser);
            setLoading(false);
          }
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

  async function handleSignIn(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!auth) {
      return;
    }

    setAuthError("");
    try {
      await signInWithEmailAndPassword(auth, email.trim(), password);
    } catch (error) {
      setAuthError(error instanceof Error ? error.message : "Authentication failed.");
    }
  }

  async function handleGoogleSignIn() {
    if (!auth) {
      return;
    }

    setAuthError("");
    try {
      const provider = new GoogleAuthProvider();
      await signInWithPopup(auth, provider);
    } catch (error) {
      setAuthError(error instanceof Error ? error.message : "Google authentication failed.");
    }
  }

  async function handleApprove() {
    if (!user || !request.isValid) {
      return;
    }

    setApproving(true);
    setApprovalError("");

    try {
      const token = await user.getIdToken(true);
      const response = await fetch("/api/oauth/approve", {
        method: "POST",
        headers: {
          authorization: `Bearer ${token}`,
          "content-type": "application/json",
        },
        body: JSON.stringify({
          clientId: request.clientId,
          redirectUri: request.redirectUri,
          resource: request.resource,
          scopes: request.scopes,
          state: request.state,
          codeChallenge: request.codeChallenge,
          codeChallengeMethod: request.codeChallengeMethod,
        }),
      });
      const body = (await response.json()) as { redirectUri?: string; error?: string };

      if (!response.ok || !body.redirectUri) {
        throw new Error(body.error ?? "Authorization failed.");
      }

      window.location.assign(body.redirectUri);
    } catch (error) {
      setApprovalError(error instanceof Error ? error.message : "Authorization failed.");
      setApproving(false);
    }
  }

  if (!firebaseConfigured) {
    return <CenteredMessage title="Convy Authorization" message="Firebase configuration is missing." />;
  }

  if (!request.isValid) {
    return <CenteredMessage title="Invalid OAuth request" message={request.error} />;
  }

  if (loading) {
    return <LoadingView />;
  }

  if (!user) {
    return (
      <main className="grid min-h-screen place-items-center px-6">
        <form className="w-full max-w-sm rounded-lg border border-line bg-white p-8 shadow-sm" onSubmit={handleSignIn}>
          <p className="text-sm font-medium text-muted">Convy</p>
          <h1 className="mt-1 text-2xl font-semibold text-ink">Sign in to authorize ChatGPT</h1>
          <button
            className="mt-6 h-11 w-full rounded-md border border-line bg-white px-4 font-semibold text-ink"
            onClick={handleGoogleSignIn}
            type="button"
          >
            Sign in with Google
          </button>
          <div className="mt-5 flex items-center gap-3 text-xs font-medium uppercase text-muted">
            <span className="h-px flex-1 bg-line" />
            <span>Email</span>
            <span className="h-px flex-1 bg-line" />
          </div>
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
          {authError ? <p className="mt-4 text-sm text-danger">{authError}</p> : null}
          <button className="mt-6 h-11 w-full rounded-md bg-brand px-4 font-semibold text-white" type="submit">
            Sign in
          </button>
        </form>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-surface px-4 py-8 sm:px-6">
      <section className="mx-auto w-full max-w-2xl rounded-lg border border-line bg-white p-6 shadow-sm sm:p-8">
        <div className="flex flex-col gap-4 border-b border-line pb-6 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <p className="text-sm font-medium text-muted">Convy</p>
            <h1 className="mt-1 text-2xl font-semibold text-ink">Authorize ChatGPT</h1>
            <p className="mt-2 text-sm text-muted">{user.email}</p>
          </div>
          <button
            className="h-10 rounded-md border border-line px-3 text-sm font-semibold text-ink"
            onClick={() => auth && signOut(auth)}
            type="button"
          >
            Sign out
          </button>
        </div>

        <div className="mt-6 rounded-md border border-info/20 bg-info/5 p-4">
          <p className="text-sm font-semibold text-ink">ChatGPT is requesting read access and limited write access.</p>
          <ul className="mt-3 space-y-2 text-sm leading-6 text-muted">
            <li>View your Convy households and household member names and roles.</li>
            <li>View household lists, shopping items, tasks, and recent activity.</li>
            <li>ChatGPT can create and complete shopping items and tasks when you approve the action.</li>
            <li>Write access is limited to the <span className="font-mono">convy.items.write</span> and <span className="font-mono">convy.tasks.write</span> scopes.</li>
          </ul>
        </div>

        <div className="mt-4 rounded-md border border-danger/25 bg-danger/5 p-4">
          <p className="text-sm font-semibold text-danger">
            In beta v1, ChatGPT cannot edit, delete, archive, invite, leave, view admin metrics, access backups, or manage lists.
          </p>
        </div>

        <dl className="mt-6 grid gap-3 text-sm sm:grid-cols-2">
          <div className="rounded-md border border-line p-3">
            <dt className="font-semibold text-ink">Client</dt>
            <dd className="mt-1 break-all text-muted">{request.clientId}</dd>
          </div>
          <div className="rounded-md border border-line p-3">
            <dt className="font-semibold text-ink">Resource</dt>
            <dd className="mt-1 break-all text-muted">{request.resource}</dd>
          </div>
        </dl>

        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
          {approvalError ? <p className="text-sm text-danger sm:mr-auto">{approvalError}</p> : null}
          <button
            className="h-11 rounded-md border border-line px-4 font-semibold text-ink"
            onClick={() => auth && signOut(auth)}
            type="button"
          >
            Not now
          </button>
          <button
            className="h-11 rounded-md bg-brand px-4 font-semibold text-white disabled:opacity-60"
            disabled={approving}
            onClick={handleApprove}
            type="button"
          >
            {approving ? "Authorizing" : "Authorize"}
          </button>
        </div>
      </section>
    </main>
  );
}

function LoadingView() {
  return (
    <main className="grid min-h-screen place-items-center px-6">
      <div className="h-10 w-10 rounded-full border-4 border-line border-t-brand" aria-label="Loading" />
    </main>
  );
}

function CenteredMessage({ title, message }: { title: string; message: string }) {
  return (
    <main className="grid min-h-screen place-items-center px-6">
      <section className="w-full max-w-md rounded-lg border border-line bg-white p-8 shadow-sm">
        <h1 className="text-2xl font-semibold text-ink">{title}</h1>
        <p className="mt-3 text-sm leading-6 text-muted">{message}</p>
      </section>
    </main>
  );
}

function parseOAuthRequest(searchParams: URLSearchParams) {
  const responseType = searchParams.get("response_type") ?? "";
  const clientId = searchParams.get("client_id") ?? "";
  const redirectUri = searchParams.get("redirect_uri") ?? "";
  const resource = searchParams.get("resource") ?? "";
  const scope = searchParams.get("scope") ?? "";
  const state = searchParams.get("state");
  const codeChallenge = searchParams.get("code_challenge") ?? "";
  const codeChallengeMethod = searchParams.get("code_challenge_method") ?? "";
  const scopes = scope.split(/\s+/).filter(Boolean);

  if (responseType !== "code") {
    return invalidRequest("OAuth response_type must be code.");
  }

  if (!clientId || !redirectUri || !resource || !codeChallenge) {
    return invalidRequest("OAuth request is missing required parameters.");
  }

  if (codeChallengeMethod !== "S256") {
    return invalidRequest("OAuth request must use PKCE S256.");
  }

  if (scopes.length === 0) {
    return invalidRequest("OAuth request is missing Convy scopes.");
  }

  const unsupportedScope = scopes.find((requestedScope) => !supportedScopes.includes(requestedScope));
  if (unsupportedScope) {
    return invalidRequest(`Unsupported beta scope: ${unsupportedScope}`);
  }

  return {
    isValid: true as const,
    clientId,
    redirectUri,
    resource,
    scopes,
    state,
    codeChallenge,
    codeChallengeMethod,
  };
}

function invalidRequest(error: string) {
  return {
    isValid: false as const,
    error,
  };
}
