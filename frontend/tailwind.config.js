/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Module accents
        'retoc': '#f59e0b',
        'uasset': '#06b6d4',
        'injector': '#ec4899',
        'uwp': '#10b981',

        // Status colors
        'status-success': '#10b981',
        'status-error': '#ef4444',
        'status-warning': '#f59e0b',
        'status-info': '#3b82f6',
      },
      fontFamily: {
        'display': ['Rajdhani', 'sans-serif'],
        'body': ['Work Sans', 'system-ui', 'sans-serif'],
        'mono': ['JetBrains Mono', 'Consolas', 'monospace'],
      },
      fontSize: {
        'xs': '0.6875rem',
        'sm': '0.8125rem',
        'base': '0.875rem',
        'lg': '1rem',
        'xl': '1.25rem',
        '2xl': '1.75rem',
        '3xl': '2.25rem',
      },
      borderRadius: {
        'sm': '3px',
        'md': '6px',
        'lg': '12px',
      },
      spacing: {
        '1': '0.25rem',
        '2': '0.5rem',
        '3': '0.75rem',
        '4': '1rem',
        '6': '1.5rem',
        '8': '2rem',
        '12': '3rem',
      },
    },
  },
  plugins: [],
}
