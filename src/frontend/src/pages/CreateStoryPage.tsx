import {
  Box,
  Button,
  FormControl,
  FormLabel,
  Heading,
  Input,
  Select,
  VStack,
  useToast,
  SimpleGrid,
  Badge,
} from '@chakra-ui/react';
import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { generateStory } from '../services/api';
import { getSoftUserId, getProfiles, saveStory } from '../services/storage';
import { ChildProfile, StoryRecord } from '../types/story';

const THEMES = [
  'Adventure',
  'Fantasy',
  'Space',
  'Animals',
  'Underwater',
  'Friendship',
  'Custom',
];

export default function CreateStoryPage() {
  const navigate = useNavigate();
  const toast = useToast();
  const [profiles, setProfiles] = useState<ChildProfile[]>([]);
  const [selectedProfileId, setSelectedProfileId] = useState<string>('');
  const [age, setAge] = useState<number>(6);
  const [theme, setTheme] = useState<string>('Adventure');
  const [customTheme, setCustomTheme] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    const loadedProfiles = getProfiles();
    setProfiles(loadedProfiles);
    if (loadedProfiles.length > 0) {
      setSelectedProfileId(loadedProfiles[0].id);
      setAge(loadedProfiles[0].age);
    }
  }, []);

  const handleProfileChange = (profileId: string) => {
    setSelectedProfileId(profileId);
    const profile = profiles.find((p) => p.id === profileId);
    if (profile) {
      setAge(profile.age);
    }
  };

  const handleGenerate = async () => {
    setIsLoading(true);
    try {
      const softUserId = getSoftUserId();
      const themeToUse = theme === 'Custom' ? customTheme : theme;

      if (!themeToUse) {
        toast({
          title: 'Please provide a theme',
          status: 'warning',
          duration: 3000,
        });
        return;
      }

      const response = await generateStory({
        softUserId,
        childProfileId: selectedProfileId || undefined,
        age,
        theme: themeToUse,
        customTheme: theme === 'Custom' ? customTheme : undefined,
      });

      // Save to local storage
      const story: StoryRecord = {
        ...response,
        childProfileId: selectedProfileId || null,
        isFavorite: false,
      };
      saveStory(story);

      toast({
        title: 'Story created!',
        status: 'success',
        duration: 3000,
      });

      navigate(`/story/${response.id}`);
    } catch (error: any) {
      toast({
        title: 'Error',
        description: error.message || 'Failed to generate story',
        status: 'error',
        duration: 5000,
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <VStack spacing={6} align="stretch">
      <Heading size="xl">Create a Story</Heading>

      <Box bg="gray.800" p={6} borderRadius="xl" borderWidth={1} borderColor="gray.700">
        <VStack spacing={5} align="stretch">
          {/* Child Profile Selector */}
          {profiles.length > 0 && (
            <FormControl>
              <FormLabel>Child Profile</FormLabel>
              <Select
                value={selectedProfileId}
                onChange={(e) => handleProfileChange(e.target.value)}
                bg="gray.700"
                borderColor="gray.600"
              >
                <option value="">None (Custom)</option>
                {profiles.map((profile) => (
                  <option key={profile.id} value={profile.id}>
                    {profile.avatar} {profile.name} (Age {profile.age})
                  </option>
                ))}
              </Select>
            </FormControl>
          )}

          {/* Age Selector */}
          <FormControl>
            <FormLabel>Age</FormLabel>
            <Select
              value={age}
              onChange={(e) => setAge(Number(e.target.value))}
              bg="gray.700"
              borderColor="gray.600"
            >
              {[4, 5, 6, 7, 8].map((ageOption) => (
                <option key={ageOption} value={ageOption}>
                  {ageOption} years old
                </option>
              ))}
            </Select>
          </FormControl>

          {/* Theme Selector */}
          <FormControl>
            <FormLabel>Story Theme</FormLabel>
            <SimpleGrid columns={{ base: 2, md: 3 }} spacing={3} mb={3}>
              {THEMES.map((themeOption) => (
                <Badge
                  key={themeOption}
                  p={3}
                  fontSize="sm"
                  textAlign="center"
                  cursor="pointer"
                  colorScheme={theme === themeOption ? 'purple' : 'gray'}
                  onClick={() => setTheme(themeOption)}
                  borderRadius="md"
                  _hover={{ transform: 'scale(1.05)' }}
                  transition="transform 0.2s"
                >
                  {themeOption}
                </Badge>
              ))}
            </SimpleGrid>
          </FormControl>

          {/* Custom Theme Input */}
          {theme === 'Custom' && (
            <FormControl>
              <FormLabel>Custom Theme</FormLabel>
              <Input
                placeholder="e.g., dinosaurs and robots"
                value={customTheme}
                onChange={(e) => setCustomTheme(e.target.value)}
                bg="gray.700"
                borderColor="gray.600"
              />
            </FormControl>
          )}

          {/* Generate Button */}
          <Button
            colorScheme="purple"
            size="lg"
            onClick={handleGenerate}
            isLoading={isLoading}
            loadingText="Creating story..."
          >
            Generate Story
          </Button>
        </VStack>
      </Box>
    </VStack>
  );
}
