/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Views/**/*.cshtml',
    './Areas/**/*.cshtml',
    './ViewComponents/**/*.{cs,cshtml}',
    './ViewModels/**/*.cs',
    './wwwroot/js/**/*.js',
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          white: '#FFFFFF',
          light: '#FAF8F3',
          canvas: '#F6F2EA',
          sand: '#EFE7DA',
          cream: '#F9FAFB',
          smoke: '#9CA3AF',
          gray: '#6B7280',
          border: '#E5E7EB',
          dark: '#111827',
          charcoal: '#1F2937',
          black: '#0B0B0B',
          gold: '#B89A62',
          goldDeep: '#9E7F43',
          goldSoft: '#E7D9BD',
          red: '#E50914',
          redDark: '#B91C1C',
          orange: '#C9A66B',
          amber: '#D8B982',
        },
      },
      fontSize: {
        hero: ['4.5rem', { lineHeight: '0.95', letterSpacing: '0' }],
        display: ['3.5rem', { lineHeight: '1', letterSpacing: '0' }],
        section: ['2.5rem', { lineHeight: '1.1', letterSpacing: '0' }],
        lead: ['1.125rem', { lineHeight: '1.75', letterSpacing: '0' }],
        kicker: ['0.75rem', { lineHeight: '1', letterSpacing: '0.14em' }],
      },
      fontFamily: {
        sans: ['Quicksand', 'ui-sans-serif', 'system-ui', 'sans-serif'],
      },
      borderRadius: {
        xs: '0.25rem',
        sm: '0.375rem',
        md: '0.5rem',
        button: '999px',
      },
      boxShadow: {
        ember: '0 16px 44px rgba(184, 154, 98, 0.2)',
        glow: '0 0 0 1px rgba(184, 154, 98, 0.22), 0 18px 52px rgba(17, 24, 39, 0.12)',
        panel: '0 24px 70px rgba(15, 23, 42, 0.08)',
        soft: '0 12px 36px rgba(15, 23, 42, 0.06)',
      },
      keyframes: {
        fadeUp: {
          '0%': { opacity: '0', transform: 'translateY(16px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        imageZoom: {
          '0%': { transform: 'scale(1)' },
          '100%': { transform: 'scale(1.06)' },
        },
        buttonGlow: {
          '0%, 100%': { boxShadow: '0 0 0 rgba(229, 9, 20, 0)' },
          '50%': { boxShadow: '0 0 28px rgba(249, 115, 22, 0.42)' },
        },
        drawerIn: {
          '0%': { transform: 'translateX(100%)' },
          '100%': { transform: 'translateX(0)' },
        },
        headerDrop: {
          '0%': { transform: 'translateY(-100%)' },
          '100%': { transform: 'translateY(0)' },
        },
      },
      animation: {
        'fade-up': 'fadeUp 640ms ease-out both',
        'fade-in': 'fadeIn 420ms ease-out both',
        'image-zoom': 'imageZoom 7s ease-out both',
        'button-glow': 'buttonGlow 2.4s ease-in-out infinite',
        'drawer-in': 'drawerIn 280ms ease-out both',
        'header-drop': 'headerDrop 260ms ease-out both',
      },
    },
  },
  plugins: [],
};
