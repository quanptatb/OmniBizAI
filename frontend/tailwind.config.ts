import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  darkMode: "class",
  theme: {
    extend: {
      colors: {
        background: {
          primary: "var(--bg-primary)",
          secondary: "var(--bg-secondary)",
          tertiary: "var(--bg-tertiary)",
        },
        text: {
          primary: "var(--text-primary)",
          secondary: "var(--text-secondary)",
        },
        accent: {
          blue: "var(--accent-blue)",
          green: "var(--accent-green)",
          orange: "var(--accent-orange)",
          red: "var(--accent-red)",
          violet: "var(--accent-violet)",
          yellow: "var(--accent-yellow)",
        },
        border: "var(--border-color)",
      },
    },
  },
  plugins: [],
};
export default config;
