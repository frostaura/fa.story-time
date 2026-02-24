/* ───────────────────────────────────────────────
 * StoryModal – full story view with poster + player
 * ─────────────────────────────────────────────── */

import Modal from '../common/Modal';
import ParallaxPoster from './ParallaxPoster';
import StoryPlayer from './StoryPlayer';
import Button from '../common/Button';
import { useAppSelector } from '../../store/hooks';
import type { Story } from '../../types';

interface StoryModalProps {
  story: Story;
  open: boolean;
  onClose: () => void;
}

export default function StoryModal({ story, open, onClose }: StoryModalProps) {
  const reducedMotion = useAppSelector((s) => s.app.settings.reducedMotion);

  return (
    <Modal open={open} onClose={onClose} title={story.title}>
      {/* Poster */}
      {story.posterLayers.length > 0 && (
        <div style={{ margin: '0 -24px', marginTop: -12 }}>
          <ParallaxPoster
            layers={story.posterLayers}
            height={280}
            reducedMotion={reducedMotion}
          />
        </div>
      )}

      {/* Player */}
      <StoryPlayer story={story} />

      {/* Scene text */}
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--spacing-lg)',
          marginTop: 'var(--spacing-md)',
        }}
      >
        {story.scenes.map((scene) => (
          <div key={scene.index}>
            <p
              style={{
                margin: 0,
                fontSize: 11,
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '0.05em',
                color: 'var(--color-text-secondary)',
                marginBottom: 6,
              }}
            >
              {scene.setting} · {scene.mood}
            </p>
            <p
              style={{
                margin: 0,
                fontFamily: 'var(--font-serif)',
                fontSize: 17,
                lineHeight: 1.7,
                color: 'var(--color-text)',
              }}
            >
              {scene.narrationText}
            </p>
          </div>
        ))}
      </div>

      {/* Continue series CTA */}
      {story.mode === 'series' && (
        <div style={{ marginTop: 'var(--spacing-xl)' }}>
          <Button fullWidth variant="secondary">
            Continue Series →
          </Button>
        </div>
      )}
    </Modal>
  );
}
