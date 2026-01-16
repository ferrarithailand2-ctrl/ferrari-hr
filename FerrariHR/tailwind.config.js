/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Pages/**/*.{cshtml,html}",
    "./Views/**/*.{cshtml,html}",
    "./Pages/Shared/**/*.{cshtml,html}"
  ],
  theme: {
    extend: {
      colors: {
        ferrariRed: "#D40000",
        ferrariGold: "#FFD700",
        ferrariDark: "#111111"
      }
    }
  },
  plugins: [],
};
