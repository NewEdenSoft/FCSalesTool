// OmegaWatch Tailwind preset. Usage in tailwind.config.js:
//   module.exports = { presets: [require('./design/tailwind.preset.js')], content: [...] }
// Values mirror tokens.json; see DESIGN.md for rules.

/** @type {import('tailwindcss').Config} */
module.exports = {
  theme: {
    extend: {
      colors: {
        ow: {
          bg: '#0A0E13',
          surface: '#11171F',
          raised: '#161E28',
          highlight: '#1A1710',
          border: '#222D3A',
          'border-strong': '#3A4757',
          text: '#E6EBF1',
          muted: '#93A1B2',
          'on-accent': '#0A0E13',
          accent: '#F2A93B',
          'accent-hover': '#FFC566',
          plex: '#6CC6F0',
          'plex-border': '#2B5A70',
          danger: '#F0857A',
        },
      },
      fontFamily: {
        heading: ['"Space Grotesk"', 'system-ui', 'sans-serif'],
        body: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'ui-monospace', 'monospace'],
      },
      fontSize: {
        display: ['56px', { lineHeight: '1.05', letterSpacing: '-0.02em', fontWeight: '700' }],
        h1: ['44px', { lineHeight: '1.1', letterSpacing: '-0.02em', fontWeight: '700' }],
        h2: ['28px', { lineHeight: '1.2', fontWeight: '600' }],
        h3: ['20px', { lineHeight: '1.3', fontWeight: '600' }],
        h4: ['18px', { lineHeight: '1.3', fontWeight: '600' }],
        'body-lg': ['17px', { lineHeight: '1.5' }],
        body: ['15px', { lineHeight: '1.5' }],
        'body-sm': ['14px', { lineHeight: '1.5' }],
        caption: ['13px', { lineHeight: '1.4' }],
        eyebrow: ['12px', { lineHeight: '1.4', letterSpacing: '0.08em' }],
        'num-xl': ['52px', { lineHeight: '1', fontWeight: '600' }],
        'num-lg': ['22px', { lineHeight: '1.2', fontWeight: '600' }],
      },
      borderRadius: {
        tag: '4px',
        control: '8px',
        panel: '10px',
      },
      maxWidth: {
        content: '1200px',
      },
      height: {
        control: '44px',
        cta: '48px',
      },
      screens: {
        sm: '480px',
        md: '768px',
        lg: '1024px',
        xl: '1280px',
      },
      transitionDuration: {
        DEFAULT: '120ms',
      },
    },
  },
};
