const apiBaseUrl = (process.env.CONVY_API_BASE_URL ?? "http://api:8080").replace(/\/+$/, "");

export async function POST(request: Request) {
  const authorization = request.headers.get("authorization");
  if (!authorization?.toLowerCase().startsWith("bearer ")) {
    return Response.json({ error: "authorization_required" }, { status: 401 });
  }

  const response = await fetch(`${apiBaseUrl}/api/v1/mcp/oauth/authorize/approve`, {
    method: "POST",
    headers: {
      authorization,
      "content-type": "application/json",
    },
    body: await request.text(),
    cache: "no-store",
  });

  const contentType = response.headers.get("content-type") ?? "";
  const body = contentType.includes("application/json") ? await response.json() : { error: await response.text() };

  return Response.json(body, {
    status: response.status,
    headers: {
      "cache-control": "no-store",
    },
  });
}
