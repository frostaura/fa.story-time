# StoryTime! Frontend

React + TypeScript PWA for generating personalized bedtime stories for children ages 4-8.

## Tech Stack

- **React 18** - UI framework
- **TypeScript** - Type safety
- **Chakra UI v2** - Component library
- **React Router** - Client-side routing
- **Vite** - Build tool
- **vite-plugin-pwa** - Progressive Web App support
- **Framer Motion** - Animations
- **uuid** - Unique ID generation

## Project Structure

```
src/frontend/
├── src/
│   ├── components/        # Reusable UI components
│   │   ├── Layout.tsx              # Main layout with navigation
│   │   ├── StoryCard.tsx           # Story display card
│   │   └── ChildProfileCard.tsx    # Profile display card
│   ├── pages/             # Route pages
│   │   ├── HomePage.tsx            # Landing page
│   │   ├── CreateStoryPage.tsx     # Story generation
│   │   ├── StoryPage.tsx           # Story reader
│   │   ├── LibraryPage.tsx         # Story library
│   │   ├── ProfilesPage.tsx        # Profile management
│   │   └── SettingsPage.tsx        # App settings
│   ├── services/          # Business logic
│   │   ├── api.ts                  # Backend API client
│   │   └── storage.ts              # LocalStorage service
│   ├── types/             # TypeScript types
│   │   └── story.ts                # Data models
│   ├── App.tsx            # Root component
│   └── main.tsx           # Entry point
├── public/                # Static assets
├── index.html             # HTML template
├── vite.config.ts         # Vite configuration
├── Dockerfile             # Production container
└── nginx.conf             # Production server config
```

## Development

### Prerequisites

- Node.js 20+
- npm

### Setup

```bash
cd src/frontend
npm install
```

### Run Development Server

```bash
npm run dev
```

The app will be available at http://localhost:5173

The Vite dev server proxies `/api` requests to `http://localhost:5000` (backend).

### Build for Production

```bash
npm run build
```

Output will be in `dist/` directory.

### Preview Production Build

```bash
npm run preview
```

## Environment Variables

Create a `.env` file in the frontend directory:

```env
VITE_API_URL=http://localhost:5000  # Backend API URL (optional, defaults to relative path)
```

## Features

### PWA Support
- Offline functionality via Service Worker
- Installable on mobile devices
- App manifest with icons and theme

### LocalStorage Management
- Auto-generated SoftUserId (UUID)
- Child profiles with CRUD operations
- Story storage with favorites
- Settings persistence
- Automatic cleanup of expired stories

### API Integration
- Story generation
- Image generation
- Text-to-speech
- Push notification subscription
- Health checks

### UI/UX
- Mobile-first responsive design
- Dark theme with purple accents
- Bottom navigation bar
- Colorful gradient story cards
- Smooth animations with Framer Motion
- Empty states and loading indicators

## Docker Deployment

Build the Docker image:

```bash
docker build -t storytime-frontend .
```

Run the container:

```bash
docker run -p 80:80 storytime-frontend
```

The container uses nginx to serve the static files and proxy API requests to the backend.

## Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint (if configured)

## Code Quality

- TypeScript strict mode enabled
- All imports type-checked
- React strict mode enabled
- PWA best practices followed

## Browser Support

- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)
- Mobile browsers (iOS Safari, Chrome Mobile)

## License

See LICENSE file in root directory.
