import { mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { pathToFileURL } from "node:url";
import { execFileSync } from "node:child_process";

const chromePath = process.env.CHROME_PATH ?? "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const builtWidgetPath = new URL("../../dist/widget/convy-summary-v1.html", import.meta.url);
const tempDir = mkdtempSync(join(tmpdir(), "convy-widget-refresh-"));

try {
  const harnessPath = join(tempDir, "harness.html");
  const html = readFileSync(builtWidgetPath, "utf8").replace("<head>", `<head>${fixtureScript()}`);
  writeFileSync(harnessPath, html, "utf8");

  const dom = execFileSync(chromePath, [
    "--headless=new",
    "--disable-gpu",
    "--no-first-run",
    "--disable-background-networking",
    "--virtual-time-budget=3000",
    "--dump-dom",
    pathToFileURL(harnessPath).href,
  ], { encoding: "utf8" });

  for (const expected of [
    "Action unavailable",
    "Convy refresh failed: rejected by fixture",
    "data-refresh-error-visible=\"true\"",
  ]) {
    if (!dom.includes(expected)) {
      throw new Error(`Expected refreshed error state to include: ${expected}`);
    }
  }

  console.log("widget refresh failure fixture passed");
} finally {
  rmSync(tempDir, { force: true, recursive: true });
}

function fixtureScript() {
  return `
    <script>
    window.openai = {
      toolOutput: {
        structuredContent: {
          data: {
            list: { id: "00000000-0000-4000-8000-000000000001" },
            pendingItems: [{ id: "item-1", title: "Milk" }]
          },
          meta: { source: "convy_api" }
        }
      },
      callTool: async () => { throw new Error("rejected by fixture"); }
    };
    window.addEventListener("load", () => {
      setTimeout(() => {
        document.querySelector(".refresh")?.click();
      }, 250);
      setTimeout(() => {
        if (document.body.textContent.includes("Convy refresh failed: rejected by fixture")) {
          document.body.setAttribute("data-refresh-error-visible", "true");
        }
      }, 1000);
    });
    </script>
  `;
}
