import { Box, Flex, Heading, Text, IconButton, Badge, VStack } from '@chakra-ui/react';
import { FaEdit, FaTrash } from 'react-icons/fa';
import { ChildProfile } from '../types/story';

interface ChildProfileCardProps {
  profile: ChildProfile;
  onEdit: () => void;
  onDelete: () => void;
}

export default function ChildProfileCard({ profile, onEdit, onDelete }: ChildProfileCardProps) {
  return (
    <Box
      bg="gray.800"
      borderRadius="xl"
      p={6}
      boxShadow="md"
      borderWidth={1}
      borderColor="gray.700"
    >
      <Flex justify="space-between" align="start">
        <Flex gap={4} align="center" flex={1}>
          {/* Avatar */}
          <Box fontSize="4xl">{profile.avatar}</Box>

          {/* Profile Info */}
          <VStack align="start" spacing={2} flex={1}>
            <Heading size="md" color="white">
              {profile.name}
            </Heading>
            <Text fontSize="sm" color="gray.400">
              Age {profile.age}
            </Text>
            <Flex gap={2} flexWrap="wrap">
              {profile.favoriteThemes.map((theme) => (
                <Badge key={theme} colorScheme="purple" fontSize="xs">
                  {theme}
                </Badge>
              ))}
            </Flex>
          </VStack>
        </Flex>

        {/* Action Buttons */}
        <Flex gap={2}>
          <IconButton
            aria-label="Edit profile"
            icon={<FaEdit />}
            size="sm"
            colorScheme="blue"
            variant="ghost"
            onClick={onEdit}
          />
          <IconButton
            aria-label="Delete profile"
            icon={<FaTrash />}
            size="sm"
            colorScheme="red"
            variant="ghost"
            onClick={onDelete}
          />
        </Flex>
      </Flex>
    </Box>
  );
}
