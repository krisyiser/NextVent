/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    './src/**/*.{js,ts,jsx,tsx}',
    './src/components/ui/**/*.{js,ts,jsx,tsx}',
    './src/app/**/*.{js,ts,jsx,tsx}',
  ],
  theme: {
    extend: {
      colors: {
        white: '#ffffff',
        black: '#000000',
        'royal-blue': '#0A1F44',
        // Additional corporate neutrals can be added here
      },
      borderWidth: {
        DEFAULT: '1px',
      },
      spacing: {
        // Compact padding values
        0.5: '0.125rem',
        1: '0.25rem',
        1.5: '0.375rem',
        2: '0.5rem',
        2.5: '0.625rem',
        3: '0.75rem',
        3.5: '0.875rem',
        4: '1rem',
      },
    },
  },
  plugins: [],
};
