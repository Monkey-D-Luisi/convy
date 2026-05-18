import type { Config } from "tailwindcss";

const config: Config = {
  content: ["./app/**/*.{ts,tsx}", "./components/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        ink: "#1f2f2b",
        muted: "#65726f",
        line: "#dbe5e1",
        surface: "#f6faf8",
        brand: "#0b7a5f",
        caution: "#b57b11",
        info: "#246b8f",
      },
    },
  },
  plugins: [],
};

export default config;
