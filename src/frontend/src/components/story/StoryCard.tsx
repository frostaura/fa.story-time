/* ───────────────────────────────────────────────
 * StoryCard – compact card in the library grid
 * ─────────────────────────────────────────────── */

import { Heart, Share2, Trash2 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import ContextMenu, { type ContextMenuItem } from '../common/ContextMenu';
import { useAppDispatch } from '../../store/hooks';
import { removeStory, toggleFavorite } from '../../store/slices/storiesSlice';
import type { Story } from '../../types';

interface StoryCardProps {
  story: Story;
}

export default function StoryCard({ story }: StoryCardProps) {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();

  const contextItems: ContextMenuItem[] = [
    {
      label: story.isFavorite ? 'Unfavorite' : 'Favorite',
      icon: <Heart size={16} />,
      onClick: () => dispatch(toggleFavorite(story.id)),
    },
    {
      label: 'Share',
      icon: <Share2 size={16} />,
      onClick: () => {
        navigator.share?.({ title: story.title, text: `Check out "${story.title}" on TaleWeaver!` }).catch(() => {});
      },
    },
    {
      label: 'Delete',
      icon: <Trash2 size={16} />,
      onClick: () => dispatch(removeStory(story.id)),
      destructive: true,
    },
  ];

  const poster = story.posterLayers.find((l) => l.role === 'BACKGROUND');
  const dateStr = new Date(story.createdAt).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
  });

  return (
    <ContextMenu items={contextItems}>
      <motion.div
        whileHover={{ y: -2 }}
        whileTap={{ scale: 0.97 }}
        onClick={() => navigate(`/story/${story.id}`)}
        role="button"
        tabIndex={0}
        aria-label={`Open story: ${story.title}`}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') navigate(`/story/${story.id}`);
        }}
        style={{
          width: 160,
          flexShrink: 0,
          cursor: 'pointer',
          borderRadius: 'var(--radius-md)',
          background: 'var(--color-surface)',
          border: '1px solid var(--color-border)',
          boxShadow: '0 2px 8px var(--color-shadow)',
          overflow: 'hidden',
        }}
      >
        {/* Poster thumbnail */}
        <div
          style={{
            width: '100%',
            height: 120,
            background: poster
              ? `url(data:image/png;base64,${poster.imageBase64}) center/cover`
              : 'linear-gradient(135deg, var(--color-accent) 0%, #818CF8 100%)',
          }}
        />

        {/* Info */}
        <div style={{ padding: '10px 12px' }}>
          <p
            style={{
              margin: 0,
              fontSize: 13,
              fontWeight: 600,
              color: 'var(--color-text)',
              whiteSpace: 'nowrap',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
            }}
          >
            {story.title}
          </p>
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              marginTop: 4,
            }}
          >
            <span
              style={{
                fontSize: 11,
                color: 'var(--color-text-secondary)',
              }}
            >
              {dateStr}
            </span>
            {story.isFavorite && (
              <Heart
                size={12}
                fill="var(--color-accent)"
                color="var(--color-accent)"
                aria-label="Favorited"
              />
            )}
          </div>
        </div>
      </motion.div>
    </ContextMenu>
  );
}
