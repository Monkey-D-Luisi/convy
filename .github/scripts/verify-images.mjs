import { spawnSync } from "node:child_process";
import { verifyRuntimeTree } from "./runtime-dependencies.mjs";
const images = process.argv.slice(2);
if (images.length !== 3 || new Set(images).size !== 3 || images.some(i => !/^convy-(dashboard|auth|mcp):ci$/.test(i))) throw Error("Require all three reviewed image tags");
for (const image of images) {
  const code = `console.log(JSON.stringify(await (${verifyRuntimeTree.toString()})("/app")))`;
  const result = spawnSync("docker", ["run", "--rm", "--entrypoint", "node", image, "--input-type=module", "-e", code], { encoding: "utf8" });
  console.log(image, result.stdout);
  if (result.error || result.status !== 0) throw Error(result.stderr || "Runtime inspection failed");
}
