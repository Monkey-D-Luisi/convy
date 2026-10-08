import path from "node:path";
import { pathToFileURL } from "node:url";

export async function verifyRuntimeTree(root) {
  const { readdir, readFile, realpath, stat } = await import("node:fs/promises");
  const { join, sep } = await import("node:path");
  const boundary = await realpath(root), visited = new Set();
  const forbidden = new Set(["braces", "micromatch", "fast-glob", "chokidar", "tailwindcss", "vite-plugin-singlefile", "postcss-selector-parser"]);
  let packages = 0;
  async function visit(directory) {
    const resolved = await realpath(directory);
    if (resolved !== boundary && !resolved.startsWith(boundary + sep)) throw Error("Runtime link escapes inspection boundary");
    if (visited.has(resolved)) return;
    visited.add(resolved);
    for (const entry of await readdir(directory, { withFileTypes: true })) {
      const file = join(directory, entry.name);
      let kind = entry;
      if (entry.isSymbolicLink()) {
        const target = await realpath(file);
        if (!target.startsWith(boundary + sep)) throw Error("Runtime link escapes inspection boundary");
        kind = await stat(file);
      }
      if (kind.isDirectory()) {
        if (forbidden.has(entry.name)) throw Error(`Development-only dependency shipped: ${file}`);
        await visit(file);
      } else if (kind.isFile() && entry.name === "package.json") {
        const pkg = JSON.parse(await readFile(file, "utf8")); packages++;
        if (forbidden.has(pkg.name)) throw Error(`Development-only dependency shipped: ${pkg.name}`);
        if (pkg.name === "sharp" && pkg.version !== "0.35.5") throw Error(`Unreviewed sharp: ${pkg.version}`);
        if (pkg.name === "@grpc/grpc-js" && pkg.version !== "1.13.6") throw Error(`Unreviewed grpc: ${pkg.version}`);
      }
    }
  }
  await visit(root);
  if (!packages) throw Error("No runtime packages found");
  return { root, packages, developmentOnlyAbsent: [...forbidden] };
}
if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) console.log(JSON.stringify(await verifyRuntimeTree(process.argv[2])));
