import { ThemeProvider } from '@emotion/react'
import { createTheme } from '@mui/material'
import { blueGrey } from '@mui/material/colors'
import { AuthProvider } from './contexts/AuthContext'
import Router from './Router'

const theme = createTheme(
  {
    palette: {
      secondary: {
        main: blueGrey[500],
        contrastText: "#fff"
      }
    }
  }
);

function App() {

  return (
    <ThemeProvider theme={theme}>
      <AuthProvider>
        <Router/>
      </AuthProvider>
    </ThemeProvider>
  )
}

export default App
