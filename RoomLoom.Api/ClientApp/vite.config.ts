import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Builds the React island to wwwroot/js/room/room.js with a fixed name
// so the Razor page reference never changes.
export default defineConfig({
  plugins: [react()],
  build: {
    outDir: "../wwwroot/js/room",
    emptyOutDir: true,
    rollupOptions: {
      input: "src/room.tsx",
      output: {
        entryFileNames: "room.js",
        assetFileNames: "room.[ext]",
      },
    },
  },
});
