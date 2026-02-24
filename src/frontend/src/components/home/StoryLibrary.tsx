/* ───────────────────────────────────────────────
 * StoryLibrary – horizontal scroll rows
 * ─────────────────────────────────────────────── */

import { useAppSelector } from '../../store/hooks';
import StoryCard from '../story/StoryCard';

export default function StoryLibrary() {
  const stories = useAppSelector((s) => s.stories.items);
  const { currentProfileId } = useAppSelector((s) => s.app);

  const profileStories = stories.filter(
    (s) => s.childProfileId === currentProfileId,
  );

  const recent = [...profileStories]
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 20);

  const favorites = profileStories.filter((s) => s.isFavorite);

  const sectionTitle: React.CSSProperties = {
    margin: 0,
    fontSize: 16,
    fontWeight: 700,
    color: 'var(--color-text)',
    letterSpacing: '-0.01em',
  };

  const scrollRow: React.CSSProperties = {
    display: 'flex',
    gap: 'var(--spacing-md)',
    overflowX: 'auto',
    paddingBottom: 'var(--spacing-sm)',
    scrollbarWidth: 'none',
    WebkitOverflowScrolling: 'touch',
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--spacing-xl)' }}>
      {/* Recent */}
      <section>
        <h2 style={sectionTitle}>Recent Stories</h2>
        {recent.length > 0 ? (
          <div style={{ ...scrollRow, marginTop: 'var(--spacing-md)' }}>
            {recent.map((s) => (
              <StoryCard key={s.id} story={s} />
            ))}
          </div>
        ) : (
          <p
            style={{
              fontSize: 14,
              color: 'var(--color-text-secondary)',
              marginTop: 'var(--spacing-md)',
            }}
          >
            No stories yet. Generate your first one above!
          </p>
        )}
      </section>

      {/* Favorites */}
      {favorites.length > 0 && (
        <section>
          <h2 style={sectionTitle}>Favorites</h2>
          <div style={{ ...scrollRow, marginTop: 'var(--spacing-md)' }}>
            {favorites.map((s) => (
              <StoryCard key={s.id} story={s} />
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
