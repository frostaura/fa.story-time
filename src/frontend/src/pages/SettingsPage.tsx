import {
  Box,
  Button,
  Heading,
  VStack,
  FormControl,
  FormLabel,
  Switch,
  Text,
  Divider,
  Code,
  useToast,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
  useDisclosure,
} from '@chakra-ui/react';
import { useState, useEffect, useRef } from 'react';
import { getSoftUserId, getSettings, saveSettings } from '../services/storage';
import { AppSettings } from '../types/story';

export default function SettingsPage() {
  const toast = useToast();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);

  const [settings, setSettings] = useState<AppSettings>({
    clarityEnabled: false,
    notificationsEnabled: false,
    theme: 'dark',
  });
  const [softUserId, setSoftUserId] = useState('');

  useEffect(() => {
    setSettings(getSettings());
    setSoftUserId(getSoftUserId());
  }, []);

  const handleToggle = (key: keyof AppSettings) => {
    const newSettings = { ...settings, [key]: !settings[key] };
    setSettings(newSettings);
    saveSettings(newSettings);
    toast({
      title: 'Settings updated',
      status: 'success',
      duration: 2000,
    });
  };

  const handleThemeToggle = () => {
    const newSettings = { ...settings, theme: settings.theme === 'dark' ? 'light' : 'dark' } as AppSettings;
    setSettings(newSettings);
    saveSettings(newSettings);
    toast({
      title: 'Theme updated',
      status: 'success',
      duration: 2000,
    });
  };

  const handleClearData = () => {
    localStorage.clear();
    toast({
      title: 'Data cleared',
      description: 'All local data has been removed',
      status: 'success',
      duration: 3000,
    });
    onClose();
    window.location.reload();
  };

  const maskUserId = (id: string) => {
    if (id.length <= 8) return id;
    return `${id.slice(0, 4)}...${id.slice(-4)}`;
  };

  return (
    <Box>
      <Heading size="xl" mb={6}>
        Settings
      </Heading>

      <VStack spacing={6} align="stretch">
        {/* Privacy & Analytics */}
        <Box bg="gray.800" p={6} borderRadius="xl" borderWidth={1} borderColor="gray.700">
          <Heading size="md" mb={4}>
            Privacy & Analytics
          </Heading>
          <VStack spacing={4} align="stretch">
            <FormControl display="flex" alignItems="center" justifyContent="space-between">
              <Box>
                <FormLabel mb={0}>Microsoft Clarity</FormLabel>
                <Text fontSize="sm" color="gray.400">
                  Help improve the app with anonymous usage data
                </Text>
              </Box>
              <Switch
                colorScheme="purple"
                isChecked={settings.clarityEnabled}
                onChange={() => handleToggle('clarityEnabled')}
              />
            </FormControl>

            <Divider borderColor="gray.600" />

            <FormControl display="flex" alignItems="center" justifyContent="space-between">
              <Box>
                <FormLabel mb={0}>Push Notifications</FormLabel>
                <Text fontSize="sm" color="gray.400">
                  Receive reminders for bedtime stories
                </Text>
              </Box>
              <Switch
                colorScheme="purple"
                isChecked={settings.notificationsEnabled}
                onChange={() => handleToggle('notificationsEnabled')}
              />
            </FormControl>
          </VStack>
        </Box>

        {/* Appearance */}
        <Box bg="gray.800" p={6} borderRadius="xl" borderWidth={1} borderColor="gray.700">
          <Heading size="md" mb={4}>
            Appearance
          </Heading>
          <FormControl display="flex" alignItems="center" justifyContent="space-between">
            <Box>
              <FormLabel mb={0}>Dark Theme</FormLabel>
              <Text fontSize="sm" color="gray.400">
                Currently using dark mode
              </Text>
            </Box>
            <Switch
              colorScheme="purple"
              isChecked={settings.theme === 'dark'}
              onChange={handleThemeToggle}
            />
          </FormControl>
        </Box>

        {/* Account */}
        <Box bg="gray.800" p={6} borderRadius="xl" borderWidth={1} borderColor="gray.700">
          <Heading size="md" mb={4}>
            Account
          </Heading>
          <VStack spacing={3} align="stretch">
            <Box>
              <Text fontSize="sm" color="gray.400" mb={1}>
                User ID
              </Text>
              <Code p={2} borderRadius="md" bg="gray.700" fontSize="xs">
                {maskUserId(softUserId)}
              </Code>
            </Box>
            <Text fontSize="xs" color="gray.500">
              Your user ID is stored locally and used to sync your data
            </Text>
          </VStack>
        </Box>

        {/* About */}
        <Box bg="gray.800" p={6} borderRadius="xl" borderWidth={1} borderColor="gray.700">
          <Heading size="md" mb={4}>
            About
          </Heading>
          <VStack spacing={2} align="stretch">
            <Text fontSize="sm">
              <strong>Version:</strong> 1.0.0
            </Text>
            <Text fontSize="sm" color="gray.400">
              StoryTime! - Personalized bedtime stories for children ages 4-8
            </Text>
            <Text fontSize="xs" color="gray.500" mt={2}>
              By using this app, you agree to our privacy policy. All stories are generated locally
              and stored on your device.
            </Text>
          </VStack>
        </Box>

        {/* Danger Zone */}
        <Box bg="red.900" p={6} borderRadius="xl" borderWidth={1} borderColor="red.700">
          <Heading size="md" mb={4} color="red.200">
            Danger Zone
          </Heading>
          <Text fontSize="sm" color="red.100" mb={4}>
            This will permanently delete all your stories, profiles, and settings from this device.
          </Text>
          <Button colorScheme="red" onClick={onOpen}>
            Clear All Data
          </Button>
        </Box>
      </VStack>

      {/* Clear Data Confirmation Dialog */}
      <AlertDialog isOpen={isOpen} leastDestructiveRef={cancelRef} onClose={onClose}>
        <AlertDialogOverlay>
          <AlertDialogContent bg="gray.800">
            <AlertDialogHeader fontSize="lg" fontWeight="bold">
              Clear All Data
            </AlertDialogHeader>

            <AlertDialogBody>
              Are you sure? This will permanently delete all your stories, profiles, and settings.
              This action cannot be undone.
            </AlertDialogBody>

            <AlertDialogFooter>
              <Button ref={cancelRef} onClick={onClose}>
                Cancel
              </Button>
              <Button colorScheme="red" onClick={handleClearData} ml={3}>
                Clear Data
              </Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialogOverlay>
      </AlertDialog>
    </Box>
  );
}
