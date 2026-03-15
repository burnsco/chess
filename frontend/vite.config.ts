import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

export default defineConfig({
  plugins: [react()],
  optimizeDeps: {
    exclude: ["stockfish"],
  },
  server: {
    proxy: {
      "/api": "http://localhost:5000",
      "/gamehub": { target: "ws://localhost:5000", ws: true },
    },
    headers: {
      "Cross-Origin-Embedder-Policy": "require-corp",
      "Cross-Origin-Opener-Policy": "same-origin",
      "Cross-Origin-Resource-Policy": "same-origin",
    },
  },
  test: {
    environment: "node",
    include: ["tests/**/*.test.ts"],
  },
});
