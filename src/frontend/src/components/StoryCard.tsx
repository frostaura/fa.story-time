import { Box, Badge, Heading, Text, IconButton, Flex } from '@chakra-ui/react';
import { FaStar, FaRegStar } from 'react-icons/fa';
import { StoryRecord } from '../types/story';

interface StoryCardProps {
  story: StoryRecord;
  onClick: () => void;
  onToggleFavorite: () => void;
}

export default function StoryCard({ story, onClick, onToggleFavorite }: StoryCardProps) {
  const gradients = [
    'linear(to-br, purple.500, pink.500)',
    'linear(to-br, blue.500, teal.500)',
    'linear(to-br, orange.500, red.500)',
    'linear(to-br, green.500, teal.500)',
    'linear(to-br, pink.500, purple.500)',
  ];

  const gradientIndex = story.id.charCodeAt(0) % gradients.length;

  return (
    <Box
      bgGradient={gradients[gradientIndex]}
      borderRadius="xl"
      p={6}
      cursor="pointer"
      onClick={onClick}
      position="relative"
      boxShadow="lg"
      _hover={{ transform: 'scale(1.02)', transition: 'transform 0.2s' }}
      transition="transform 0.2s"
    >
      {/* Favorite Icon */}
      <IconButton
        aria-label="Toggle favorite"
        icon={story.isFavorite ? <FaStar /> : <FaRegStar />}
        position="absolute"
        top={4}
        right={4}
        size="sm"
        colorScheme="whiteAlpha"
        onClick={(e) => {
          e.stopPropagation();
          onToggleFavorite();
        }}
      />

      {/* Content */}
      <Flex direction="column" gap={3}>
        <Heading size="md" color="white" noOfLines={2}>
          {story.title}
        </Heading>
        <Text fontSize="sm" color="whiteAlpha.900" noOfLines={3}>
          {story.summary}
        </Text>
        <Flex gap={2} flexWrap="wrap">
          <Badge colorScheme="whiteAlpha" fontSize="xs">
            Age {story.childProfileId ? 'Profile' : 'Custom'}
          </Badge>
          <Badge colorScheme="whiteAlpha" fontSize="xs">
            {story.tierSlug}
          </Badge>
        </Flex>
      </Flex>
    </Box>
  );
}
