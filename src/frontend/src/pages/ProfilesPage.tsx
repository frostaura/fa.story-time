import {
  Box,
  Button,
  FormControl,
  FormLabel,
  Heading,
  Input,
  VStack,
  useToast,
  Select,
  SimpleGrid,
  Text,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalCloseButton,
  useDisclosure,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
} from '@chakra-ui/react';
import { useState, useEffect, useRef } from 'react';
import { v4 as uuidv4 } from 'uuid';
import ChildProfileCard from '../components/ChildProfileCard';
import { getProfiles, saveProfile, deleteProfile as deleteProfileStorage } from '../services/storage';
import { ChildProfile } from '../types/story';

const AVATARS = ['👦', '👧', '🧒', '👶', '🧑', '👨', '👩', '🦁', '🐻', '🐼', '🦊', '🐸'];
const THEMES = ['Adventure', 'Fantasy', 'Space', 'Animals', 'Underwater', 'Friendship'];

export default function ProfilesPage() {
  const toast = useToast();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const { isOpen: isDeleteOpen, onOpen: onDeleteOpen, onClose: onDeleteClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);

  const [profiles, setProfiles] = useState<ChildProfile[]>([]);
  const [editingProfile, setEditingProfile] = useState<ChildProfile | null>(null);
  const [profileToDelete, setProfileToDelete] = useState<string | null>(null);
  
  const [name, setName] = useState('');
  const [age, setAge] = useState(6);
  const [avatar, setAvatar] = useState(AVATARS[0]);
  const [selectedThemes, setSelectedThemes] = useState<string[]>([]);

  useEffect(() => {
    loadProfiles();
  }, []);

  const loadProfiles = () => {
    setProfiles(getProfiles());
  };

  const handleOpenModal = (profile?: ChildProfile) => {
    if (profile) {
      setEditingProfile(profile);
      setName(profile.name);
      setAge(profile.age);
      setAvatar(profile.avatar);
      setSelectedThemes(profile.favoriteThemes);
    } else {
      setEditingProfile(null);
      setName('');
      setAge(6);
      setAvatar(AVATARS[0]);
      setSelectedThemes([]);
    }
    onOpen();
  };

  const handleSave = () => {
    if (!name.trim()) {
      toast({
        title: 'Name is required',
        status: 'warning',
        duration: 3000,
      });
      return;
    }

    const profile: ChildProfile = {
      id: editingProfile?.id || uuidv4(),
      name: name.trim(),
      age,
      avatar,
      favoriteThemes: selectedThemes,
      createdAt: editingProfile?.createdAt || new Date().toISOString(),
    };

    saveProfile(profile);
    loadProfiles();
    onClose();
    toast({
      title: editingProfile ? 'Profile updated' : 'Profile created',
      status: 'success',
      duration: 3000,
    });
  };

  const handleDeleteClick = (id: string) => {
    setProfileToDelete(id);
    onDeleteOpen();
  };

  const handleDeleteConfirm = () => {
    if (profileToDelete) {
      deleteProfileStorage(profileToDelete);
      loadProfiles();
      toast({
        title: 'Profile deleted',
        status: 'success',
        duration: 3000,
      });
    }
    setProfileToDelete(null);
    onDeleteClose();
  };

  const toggleTheme = (theme: string) => {
    setSelectedThemes((prev) =>
      prev.includes(theme) ? prev.filter((t) => t !== theme) : [...prev, theme]
    );
  };

  return (
    <Box>
      <Heading size="xl" mb={6}>
        Child Profiles
      </Heading>

      <Button colorScheme="purple" mb={6} onClick={() => handleOpenModal()}>
        Add Profile
      </Button>

      {profiles.length > 0 ? (
        <VStack spacing={4} align="stretch">
          {profiles.map((profile) => (
            <ChildProfileCard
              key={profile.id}
              profile={profile}
              onEdit={() => handleOpenModal(profile)}
              onDelete={() => handleDeleteClick(profile.id)}
            />
          ))}
        </VStack>
      ) : (
        <Box textAlign="center" py={8} bg="gray.800" borderRadius="xl">
          <Text color="gray.400">
            No profiles yet. Add your first child profile to get started!
          </Text>
        </Box>
      )}

      {/* Edit/Add Profile Modal */}
      <Modal isOpen={isOpen} onClose={onClose} size="lg">
        <ModalOverlay />
        <ModalContent bg="gray.800">
          <ModalHeader>{editingProfile ? 'Edit Profile' : 'Add Profile'}</ModalHeader>
          <ModalCloseButton />
          <ModalBody pb={6}>
            <VStack spacing={4} align="stretch">
              <FormControl isRequired>
                <FormLabel>Name</FormLabel>
                <Input
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="Child's name"
                  bg="gray.700"
                />
              </FormControl>

              <FormControl isRequired>
                <FormLabel>Age</FormLabel>
                <Select value={age} onChange={(e) => setAge(Number(e.target.value))} bg="gray.700">
                  {[4, 5, 6, 7, 8].map((ageOption) => (
                    <option key={ageOption} value={ageOption}>
                      {ageOption} years old
                    </option>
                  ))}
                </Select>
              </FormControl>

              <FormControl>
                <FormLabel>Avatar</FormLabel>
                <SimpleGrid columns={6} spacing={2}>
                  {AVATARS.map((emojiAvatar) => (
                    <Box
                      key={emojiAvatar}
                      fontSize="3xl"
                      textAlign="center"
                      cursor="pointer"
                      p={2}
                      borderRadius="md"
                      bg={avatar === emojiAvatar ? 'purple.600' : 'gray.700'}
                      onClick={() => setAvatar(emojiAvatar)}
                      _hover={{ bg: 'purple.500' }}
                    >
                      {emojiAvatar}
                    </Box>
                  ))}
                </SimpleGrid>
              </FormControl>

              <FormControl>
                <FormLabel>Favorite Themes</FormLabel>
                <SimpleGrid columns={2} spacing={2}>
                  {THEMES.map((theme) => (
                    <Button
                      key={theme}
                      size="sm"
                      colorScheme={selectedThemes.includes(theme) ? 'purple' : 'gray'}
                      onClick={() => toggleTheme(theme)}
                    >
                      {theme}
                    </Button>
                  ))}
                </SimpleGrid>
              </FormControl>

              <Button colorScheme="purple" onClick={handleSave}>
                {editingProfile ? 'Update' : 'Create'} Profile
              </Button>
            </VStack>
          </ModalBody>
        </ModalContent>
      </Modal>

      {/* Delete Confirmation Dialog */}
      <AlertDialog
        isOpen={isDeleteOpen}
        leastDestructiveRef={cancelRef}
        onClose={onDeleteClose}
      >
        <AlertDialogOverlay>
          <AlertDialogContent bg="gray.800">
            <AlertDialogHeader fontSize="lg" fontWeight="bold">
              Delete Profile
            </AlertDialogHeader>

            <AlertDialogBody>
              Are you sure? This will not delete stories associated with this profile.
            </AlertDialogBody>

            <AlertDialogFooter>
              <Button ref={cancelRef} onClick={onDeleteClose}>
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
