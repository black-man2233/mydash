import { createVuetify } from 'vuetify'
import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'

export default createVuetify({
  theme: {
    defaultTheme: 'dark',
    themes: {
      dark: {
        dark: true,
        colors: {
          primary: '#5C6BC0',
          secondary: '#26C6DA',
          background: '#0D1117',
          surface: '#161B22',
          'surface-variant': '#1F2937',
          success: '#2ea043',
          error: '#f85149',
          warning: '#d29922',
        }
      }
    }
  }
})
