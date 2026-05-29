import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";
import { viteSingleFile } from "vite-plugin-singlefile";

export default defineConfig({
  root: "widget",
  plugins: [react(), viteSingleFile()],
  build: {
    outDir: "../dist/widget",
    emptyOutDir: true,
    rollupOptions: {
      input: "convy-summary-v1.html",
      output: {
        entryFileNames: "convy-summary-v1.js",
        assetFileNames: "convy-summary-v1.[ext]",
      },
    },
  },
});
