import { Routes, Route } from 'react-router-dom';
import { Box } from '@chakra-ui/react';
import HomePage from './pages/HomePage';
import CreateStoryPage from './pages/CreateStoryPage';
import StoryPage from './pages/StoryPage';
import LibraryPage from './pages/LibraryPage';
import ProfilesPage from './pages/ProfilesPage';
import SettingsPage from './pages/SettingsPage';
import Layout from './components/Layout';

function App() {
  return (
    <Box minH="100vh">
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/create" element={<CreateStoryPage />} />
          <Route path="/story/:id" element={<StoryPage />} />
          <Route path="/library" element={<LibraryPage />} />
          <Route path="/profiles" element={<ProfilesPage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Route>
      </Routes>
    </Box>
  );
}

export default App;
