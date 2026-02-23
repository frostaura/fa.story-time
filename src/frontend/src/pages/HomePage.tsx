import { Box, Button, Heading, Text, VStack, SimpleGrid, Icon, Flex } from '@chakra-ui/react';
import { useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { FaBook, FaShieldAlt, FaUserCheck } from 'react-icons/fa';
import StoryCard from '../components/StoryCard';
import { getStories, toggleFavorite as toggleFavoriteStorage } from '../services/storage';
import { StoryRecord } from '../types/story';

export default function HomePage() {
  const navigate = useNavigate();
  const [recentStories, setRecentStories] = useState<StoryRecord[]>([]);

  useEffect(() => {
    const stories = getStories().slice(-3).reverse();
    setRecentStories(stories);
  }, []);

  const handleToggleFavorite = (id: string) => {
    toggleFavoriteStorage(id);
    setRecentStories(getStories().slice(-3).reverse());
  };

  return (
    <VStack spacing={8} align="stretch">
      {/* Hero Section */}
      <Box
        bgGradient="linear(to-br, purple.600, purple.800)"
        borderRadius="2xl"
        p={8}
        textAlign="center"
        boxShadow="xl"
      >
        <Heading size="2xl" mb={4} color="white">
          Welcome to StoryTime!
        </Heading>
        <Text fontSize="lg" mb={6} color="whiteAlpha.900">
          Create personalized bedtime stories for your little ones
        </Text>
        <Button
          size="lg"
          colorScheme="whiteAlpha"
          onClick={() => navigate('/create')}
          leftIcon={<FaBook />}
        >
          Create a Bedtime Story
        </Button>
      </Box>

      {/* Recent Stories */}
      {recentStories.length > 0 && (
        <Box>
          <Heading size="lg" mb={4}>
            Recent Stories
          </Heading>
          <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} spacing={4}>
            {recentStories.map((story) => (
              <StoryCard
                key={story.id}
                story={story}
                onClick={() => navigate(`/story/${story.id}`)}
                onToggleFavorite={() => handleToggleFavorite(story.id)}
              />
            ))}
          </SimpleGrid>
        </Box>
      )}

      {/* Feature Highlights */}
      <Box>
        <Heading size="lg" mb={4}>
          Why StoryTime?
        </Heading>
        <SimpleGrid columns={{ base: 1, md: 3 }} spacing={6}>
          <FeatureCard
            icon={FaBook}
            title="Offline AI"
            description="Stories work offline once generated"
          />
          <FeatureCard
            icon={FaShieldAlt}
            title="Safe for Kids"
            description="Age-appropriate content for 4-8 year olds"
          />
          <FeatureCard
            icon={FaUserCheck}
            title="Personalized"
            description="Custom stories based on your child's interests"
          />
        </SimpleGrid>
      </Box>

      {/* Empty State for First Time Users */}
      {recentStories.length === 0 && (
        <Box textAlign="center" py={8}>
          <Heading size="md" mb={4} color="gray.400">
            No stories yet!
          </Heading>
          <Text color="gray.500" mb={6}>
            Create your first bedtime story to get started
          </Text>
        </Box>
      )}
    </VStack>
  );
}

interface FeatureCardProps {
  icon: any;
  title: string;
  description: string;
}

function FeatureCard({ icon, title, description }: FeatureCardProps) {
  return (
    <Box bg="gray.800" p={6} borderRadius="xl" borderWidth={1} borderColor="gray.700">
      <Flex direction="column" align="center" textAlign="center" gap={3}>
        <Icon as={icon} boxSize={10} color="purple.400" />
        <Heading size="md" color="white">
          {title}
        </Heading>
        <Text fontSize="sm" color="gray.400">
          {description}
        </Text>
      </Flex>
    </Box>
  );
}
