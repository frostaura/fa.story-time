import {
  Box,
  Button,
  Heading,
  Text,
  VStack,
  IconButton,
  Flex,
  Divider,
  useToast,
} from '@chakra-ui/react';
import { useParams, useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { FaStar, FaRegStar, FaArrowLeft, FaShare } from 'react-icons/fa';
import { getStories, toggleFavorite as toggleFavoriteStorage } from '../services/storage';
import { StoryRecord } from '../types/story';

export default function StoryPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const toast = useToast();
  const [story, setStory] = useState<StoryRecord | null>(null);

  useEffect(() => {
    if (id) {
      const stories = getStories();
      const foundStory = stories.find((s) => s.id === id);
      if (foundStory) {
        setStory(foundStory);
      } else {
        toast({
          title: 'Story not found',
          status: 'error',
          duration: 3000,
        });
        navigate('/library');
      }
    }
  }, [id, navigate, toast]);

  const handleToggleFavorite = () => {
    if (id) {
      toggleFavoriteStorage(id);
      const stories = getStories();
      const updatedStory = stories.find((s) => s.id === id);
      if (updatedStory) {
        setStory(updatedStory);
      }
    }
  };

  const handleShare = () => {
    toast({
      title: 'Share feature coming soon!',
      status: 'info',
      duration: 3000,
    });
  };

  if (!story) {
    return (
      <Box textAlign="center" py={8}>
        <Text>Loading story...</Text>
      </Box>
    );
  }

  return (
    <VStack spacing={6} align="stretch">
      {/* Header Actions */}
      <Flex justify="space-between" align="center">
        <IconButton
          aria-label="Back to library"
          icon={<FaArrowLeft />}
          onClick={() => navigate('/library')}
          colorScheme="gray"
          variant="ghost"
        />
        <Flex gap={2}>
          <IconButton
            aria-label="Toggle favorite"
            icon={story.isFavorite ? <FaStar /> : <FaRegStar />}
            onClick={handleToggleFavorite}
            colorScheme={story.isFavorite ? 'yellow' : 'gray'}
            variant="ghost"
          />
          <IconButton
            aria-label="Share story"
            icon={<FaShare />}
            onClick={handleShare}
            colorScheme="blue"
            variant="ghost"
          />
        </Flex>
      </Flex>

      {/* Story Content */}
      <Box bg="gray.800" p={8} borderRadius="xl" borderWidth={1} borderColor="gray.700">
        <VStack spacing={6} align="stretch">
          <Heading size="2xl" color="purple.300">
            {story.title}
          </Heading>

          <Text fontSize="lg" color="gray.300" fontStyle="italic">
            {story.summary}
          </Text>

          <Divider borderColor="gray.600" />

          {/* Story Text with Scene Breaks */}
          <Box>
            {story.scenes && story.scenes.length > 0 ? (
              <VStack spacing={8} align="stretch">
                {story.scenes.map((scene, index) => (
                  <Box key={scene.id}>
                    <Heading size="md" color="purple.200" mb={3}>
                      Scene {index + 1}
                    </Heading>
                    <Text fontSize="md" lineHeight="tall" color="white" whiteSpace="pre-wrap">
                      {scene.text}
                    </Text>
                  </Box>
                ))}
              </VStack>
            ) : (
              <Text fontSize="md" lineHeight="tall" color="white" whiteSpace="pre-wrap">
                {story.text}
              </Text>
            )}
          </Box>

          <Divider borderColor="gray.600" />

          {/* Story Metadata */}
          <Flex gap={4} flexWrap="wrap" fontSize="sm" color="gray.400">
            <Text>Created: {new Date(story.createdAt).toLocaleDateString()}</Text>
            <Text>Tier: {story.tierSlug}</Text>
            {story.expiresAt && (
              <Text>Expires: {new Date(story.expiresAt).toLocaleDateString()}</Text>
            )}
          </Flex>
        </VStack>
      </Box>

      {/* Actions */}
      <Flex gap={4}>
        <Button
          flex={1}
          colorScheme="purple"
          onClick={() => navigate('/create')}
        >
          Create Another Story
        </Button>
      </Flex>
    </VStack>
  );
}
