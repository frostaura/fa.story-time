import { Box, Container, Flex, Icon, Text, VStack } from '@chakra-ui/react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { FaHome, FaPlus, FaBook, FaUsers, FaCog } from 'react-icons/fa';

const navItems = [
  { path: '/', icon: FaHome, label: 'Home' },
  { path: '/create', icon: FaPlus, label: 'Create' },
  { path: '/library', icon: FaBook, label: 'Library' },
  { path: '/profiles', icon: FaUsers, label: 'Profiles' },
  { path: '/settings', icon: FaCog, label: 'Settings' },
];

export default function Layout() {
  const navigate = useNavigate();
  const location = useLocation();

  return (
    <Flex direction="column" minH="100vh" bg="gray.900">
      {/* Header */}
      <Box
        bgGradient="linear(to-r, purple.600, purple.800)"
        px={4}
        py={4}
        boxShadow="md"
      >
        <Text fontSize="2xl" fontWeight="bold" textAlign="center" color="white">
          StoryTime!
        </Text>
      </Box>

      {/* Main Content */}
      <Container maxW="container.lg" flex="1" py={6} pb={24}>
        <Outlet />
      </Container>

      {/* Bottom Navigation */}
      <Box
        position="fixed"
        bottom={0}
        left={0}
        right={0}
        bg="gray.800"
        borderTop="1px"
        borderColor="gray.700"
        boxShadow="lg"
      >
        <Flex justify="space-around" align="center" py={2}>
          {navItems.map((item) => {
            const isActive = location.pathname === item.path;
            return (
              <VStack
                key={item.path}
                spacing={1}
                onClick={() => navigate(item.path)}
                cursor="pointer"
                color={isActive ? 'purple.400' : 'gray.400'}
                _hover={{ color: 'purple.300' }}
                transition="color 0.2s"
              >
                <Icon as={item.icon} boxSize={6} />
                <Text fontSize="xs" fontWeight="medium">
                  {item.label}
                </Text>
              </VStack>
            );
          })}
        </Flex>
      </Box>
    </Flex>
  );
}
