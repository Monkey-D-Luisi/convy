import { spawnSync } from "node:child_process";
import { verifyRuntimeTree } from "./runtime-dependencies.mjs";
const image = process.argv[2];
if (!/^sha256:[a-f0-9]{64}$/.test(image ?? "")) throw Error("Require an immutable image ID");
const code = `console.log(JSON.stringify(await (${verifyRuntimeTree.toString()})("/app")))`;
const result = spawnSync("docker", ["run", "--rm", "--network", "none", "--memory", "256m", "--entrypoint", "node", image, "--input-type=module", "-e", code], { encoding: "utf8" });
if (result.error || result.status !== 0) throw Error("Release runtime closure verification failed");
console.log(result.stdout);
