/* ───────────────────────────────────────────────
 * StoryPage – /story/:id
 * ─────────────────────────────────────────────── */

import { useParams, useNavigate } from 'react-router-dom';
import { useAppSelector } from '../store/hooks';
import StoryModal from '../components/story/StoryModal';

export default function StoryPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const story = useAppSelector((s) =>
    s.stories.items.find((item) => item.id === id),
  );

  if (!story) {
    return (
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '50vh',
          gap: 'var(--spacing-md)',
        }}
      >
        <p style={{ fontSize: 16, color: 'var(--color-text-secondary)' }}>
          Story not found.
        </p>
        <button
          onClick={() => navigate('/')}
          style={{
            background: 'none',
            border: 'none',
            color: 'var(--color-accent)',
            fontSize: 14,
            fontWeight: 600,
            cursor: 'pointer',
            minHeight: 44,
            fontFamily: 'var(--font-sans)',
          }}
        >
          ← Back to home
        </button>
      </div>
    );
  }

  return (
    <StoryModal
      story={story}
      open={true}
      onClose={() => navigate('/')}
    />
  );
}
