"use client";

import { FirebaseOptions, getApps, initializeApp } from "firebase/app";
import { Auth, getAuth } from "firebase/auth";

export type FirebaseRuntimeConfig = {
  apiKey: string;
  authDomain: string;
  projectId: string;
  appId: string;
};

export function isFirebaseConfigured(config: FirebaseRuntimeConfig | null) {
  return Boolean(config?.apiKey && config.authDomain && config.projectId && config.appId);
}

export function getFirebaseAuth(config: FirebaseRuntimeConfig): Auth {
  const appName = "convy-dashboard";
  const existing = getApps().find((app) => app.name === appName);
  const app = existing ?? initializeApp(config satisfies FirebaseOptions, appName);

  return getAuth(app);
}
