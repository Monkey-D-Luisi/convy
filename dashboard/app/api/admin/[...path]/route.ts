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

  const headers = new Headers();
  headers.set("cache-control", "no-store");
  headers.set("content-type", response.headers.get("content-type") ?? "application/json");
  const contentDisposition = response.headers.get("content-disposition");
  if (contentDisposition) {
    headers.set("content-disposition", contentDisposition);
  }
  const contentLength = response.headers.get("content-length");
  if (contentLength) {
    headers.set("content-length", contentLength);
  }

  return new Response(response.body, {
    status: response.status,
    headers,
  });
}
