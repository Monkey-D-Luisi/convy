import express from "express";
import { StreamableHTTPServerTransport } from "@modelcontextprotocol/sdk/server/streamableHttp.js";
import { authenticateRequest } from "./auth.js";
import { loadConfig, type McpConfig } from "./config.js";
import { createProtectedResourceMetadata } from "./metadata.js";
import { createConvyMcpServer } from "./tools/server.js";

export function createApp(config: McpConfig) {
  const app = express();

  app.disable("x-powered-by");
  app.use(express.json({ limit: "1mb" }));
  const mcpRateLimit = createFixedWindowRateLimit(120, 10 * 60 * 1000);

  app.get("/health", (_req, res) => {
    res.json({ status: "ok" });
  });

  app.get("/.well-known/oauth-protected-resource", (_req, res) => {
    res.json(createProtectedResourceMetadata(config));
  });

  app.get("/.well-known/openai-apps-challenge", (_req, res) => {
    if (!config.openAiAppsChallengeToken) {
      res.status(404).type("text/plain").send("Not found");
      return;
    }

    res.type("text/plain").send(config.openAiAppsChallengeToken);
  });

  const sendDocs = (_req: express.Request, res: express.Response) => {
    res.type("text/plain").send([
      "Convy MCP beta",
      "",
      "Read tools: convy_get_context, convy_get_shopping_context, convy_get_shopping_list, convy_get_task_list, convy_get_recent_activity.",
      "Limited write tools: convy_add_shopping_items, convy_update_shopping_items_status, convy_add_tasks, convy_update_tasks_status.",
      "Scopes: convy.households.read, convy.lists.read, convy.items.read, convy.tasks.read, convy.activity.read, convy.items.write, convy.tasks.write.",
    ].join("\n"));
  };

  app.get("/docs", sendDocs);

  const handleMcpRequest = async (req: express.Request, res: express.Response) => {
    const auth = await authenticateRequest(req, res, config);
    if (!auth) {
      return;
    }

    const server = createConvyMcpServer(config, auth);
    const transport = new StreamableHTTPServerTransport({
      sessionIdGenerator: undefined,
    });

    try {
      await server.connect(transport);
      await transport.handleRequest(req, res, req.body);
    } finally {
      await server.close();
    }
  };

  app.post("/mcp", mcpRateLimit, handleMcpRequest);

  return app;
}

function createFixedWindowRateLimit(limit: number, windowMs: number) {
  const buckets = new Map<string, { count: number; resetAt: number }>();

  return (req: express.Request, res: express.Response, next: express.NextFunction) => {
    const now = Date.now();
    const key = req.ip ?? req.socket.remoteAddress ?? "unknown";
    const bucket = buckets.get(key);

    if (!bucket || bucket.resetAt <= now) {
      buckets.set(key, { count: 1, resetAt: now + windowMs });
      next();
      return;
    }

    if (bucket.count >= limit) {
      res.status(429).json({ error: "rate_limited" });
      return;
    }

    bucket.count += 1;
    next();
  };
}

if (import.meta.url === `file://${process.argv[1]}`) {
  const config = loadConfig();
  createApp(config).listen(config.port, () => {
    console.log(`Convy MCP server listening on port ${config.port}`);
  });
}
