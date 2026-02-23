import React from 'react';
import ReactDOM from 'react-dom/client';
import { ChakraProvider, extendTheme } from '@chakra-ui/react';
import { BrowserRouter } from 'react-router-dom';
import App from './App';

const theme = extendTheme({
  config: { initialColorMode: 'dark', useSystemColorMode: false },
  colors: {
    brand: {
      50: '#f5e6ff', 100: '#dbb8ff', 200: '#c18aff', 300: '#a75cff',
      400: '#8d2eff', 500: '#7415e6', 600: '#5a0fb4', 700: '#400a82',
      800: '#270550', 900: '#100020'
    }
  },
  fonts: {
    heading: "'Inter', -apple-system, BlinkMacSystemFont, sans-serif",
    body: "'Inter', -apple-system, BlinkMacSystemFont, sans-serif"
  },
  styles: {
    global: {
      body: { bg: 'gray.900', color: 'white' }
    }
  }
});

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ChakraProvider theme={theme}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </ChakraProvider>
  </React.StrictMode>
);
