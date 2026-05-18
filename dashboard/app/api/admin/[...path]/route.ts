import { NextRequest } from "next/server";

const apiBaseUrl = process.env.CONVY_API_BASE_URL ?? "http://api:8080";
const firebaseTokenHeader = "x-firebase-id-token";

export async function GET(request: NextRequest, context: { params: Promise<{ path: string[] }> }) {
  const { path } = await context.params;
  const target = new URL(`/api/v1/admin/${path.join("/")}`, apiBaseUrl);
  target.search = request.nextUrl.search;

  const firebaseToken = request.headers.get(firebaseTokenHeader);
  const response = await fetch(target, {
    headers: firebaseToken ? { authorization: `Bearer ${firebaseToken}` } : undefined,
    cache: "no-store",
  });

  const body = await response.text();
  return new Response(body, {
    status: response.status,
    headers: {
      "content-type": response.headers.get("content-type") ?? "application/json",
      "cache-control": "no-store",
    },
  });
}
