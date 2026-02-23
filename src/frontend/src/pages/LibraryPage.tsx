import {
  Box,
  Button,
  Heading,
  SimpleGrid,
  Tabs,
  TabList,
  Tab,
  TabPanels,
  TabPanel,
  Text,
  useToast,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
  useDisclosure,
} from '@chakra-ui/react';
import { useEffect, useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import StoryCard from '../components/StoryCard';
import { getStories, getFavorites, toggleFavorite as toggleFavoriteStorage, deleteStory } from '../services/storage';
import { StoryRecord } from '../types/story';

export default function LibraryPage() {
  const navigate = useNavigate();
  const toast = useToast();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);
  
  const [allStories, setAllStories] = useState<StoryRecord[]>([]);
  const [favorites, setFavorites] = useState<StoryRecord[]>([]);
  const [storyToDelete, setStoryToDelete] = useState<string | null>(null);

  const loadStories = () => {
    setAllStories(getStories().reverse());
    setFavorites(getFavorites().reverse());
  };

  useEffect(() => {
    loadStories();
  }, []);

  const handleToggleFavorite = (id: string) => {
    toggleFavoriteStorage(id);
    loadStories();
  };

  const handleDeleteClick = (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setStoryToDelete(id);
    onOpen();
  };

  const handleDeleteConfirm = () => {
    if (storyToDelete) {
      deleteStory(storyToDelete);
      loadStories();
      toast({
        title: 'Story deleted',
        status: 'success',
        duration: 3000,
      });
    }
    setStoryToDelete(null);
    onClose();
  };

  return (
    <Box>
      <Heading size="xl" mb={6}>
        Your Library
      </Heading>

      <Tabs colorScheme="purple">
        <TabList>
          <Tab>All Stories ({allStories.length})</Tab>
          <Tab>Favorites ({favorites.length})</Tab>
        </TabList>

        <TabPanels>
          {/* All Stories Tab */}
          <TabPanel px={0} py={6}>
            {allStories.length > 0 ? (
              <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} spacing={4}>
                {allStories.map((story) => (
                  <Box key={story.id} position="relative">
                    <StoryCard
                      story={story}
                      onClick={() => navigate(`/story/${story.id}`)}
                      onToggleFavorite={() => handleToggleFavorite(story.id)}
                    />
                    <Button
                      size="xs"
                      colorScheme="red"
                      variant="ghost"
                      position="absolute"
                      bottom={2}
                      right={2}
                      onClick={(e) => handleDeleteClick(story.id, e)}
                    >
                      Delete
                    </Button>
                  </Box>
                ))}
              </SimpleGrid>
            ) : (
              <Box textAlign="center" py={8}>
                <Text color="gray.400" mb={4}>
                  No stories yet
                </Text>
                <Button colorScheme="purple" onClick={() => navigate('/create')}>
                  Create Your First Story
                </Button>
              </Box>
            )}
          </TabPanel>

          {/* Favorites Tab */}
          <TabPanel px={0} py={6}>
            {favorites.length > 0 ? (
              <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} spacing={4}>
                {favorites.map((story) => (
                  <Box key={story.id} position="relative">
                    <StoryCard
                      story={story}
                      onClick={() => navigate(`/story/${story.id}`)}
                      onToggleFavorite={() => handleToggleFavorite(story.id)}
                    />
                    <Button
                      size="xs"
                      colorScheme="red"
                      variant="ghost"
                      position="absolute"
                      bottom={2}
                      right={2}
                      onClick={(e) => handleDeleteClick(story.id, e)}
                    >
                      Delete
                    </Button>
                  </Box>
                ))}
              </SimpleGrid>
            ) : (
              <Box textAlign="center" py={8}>
                <Text color="gray.400">
                  No favorite stories yet. Star your favorites to see them here!
                </Text>
              </Box>
            )}
          </TabPanel>
        </TabPanels>
      </Tabs>

      {/* Delete Confirmation Dialog */}
      <AlertDialog
        isOpen={isOpen}
        leastDestructiveRef={cancelRef}
        onClose={onClose}
      >
        <AlertDialogOverlay>
          <AlertDialogContent bg="gray.800">
            <AlertDialogHeader fontSize="lg" fontWeight="bold">
              Delete Story
            </AlertDialogHeader>

            <AlertDialogBody>
              Are you sure? This action cannot be undone.
            </AlertDialogBody>

            <AlertDialogFooter>
              <Button ref={cancelRef} onClick={onClose}>
                Cancel
              </Button>
              <Button colorScheme="red" onClick={handleDeleteConfirm} ml={3}>
                Delete
              </Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialogOverlay>
      </AlertDialog>
    </Box>
  );
}
