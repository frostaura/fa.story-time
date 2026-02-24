/* ───────────────────────────────────────────────
 * QuickGenerateCard – hero CTA with cooldown
 * ─────────────────────────────────────────────── */

import { useEffect, useState, useCallback } from 'react';
import { Sparkles, Timer } from 'lucide-react';
import Button from '../common/Button';
import Spinner from '../common/Spinner';
import { useAppSelector, useAppDispatch } from '../../store/hooks';
import { startGeneration, setProgress, completeGeneration, failGeneration } from '../../store/slices/generationSlice';
import { addStory } from '../../store/slices/storiesSlice';
import { getCooldownState, saveCooldownState, getProfiles } from '../../services/localStorage';
import * as api from '../../services/api';

interface QuickGenerateCardProps {
  duration: number;
}

export default function QuickGenerateCard({ duration }: QuickGenerateCardProps) {
  const dispatch = useAppDispatch();
  const { isGenerating, progressMessage } = useAppSelector((s) => s.generation);
  const { currentProfileId } = useAppSelector((s) => s.app);
  const profile = getProfiles().find((p) => p.id === currentProfileId);

  const [cooldownRemaining, setCooldownRemaining] = useState(0);

  /* Cooldown timer */
  useEffect(() => {
    const tick = () => {
      const { lastGenerationAt, cooldownMinutes } = getCooldownState();
      if (!lastGenerationAt) {
        setCooldownRemaining(0);
        return;
      }
      const elapsed = Date.now() - new Date(lastGenerationAt).getTime();
      const remaining = cooldownMinutes * 60 * 1000 - elapsed;
      setCooldownRemaining(remaining > 0 ? remaining : 0);
    };
    tick();
    const id = setInterval(tick, 1000);
    return () => clearInterval(id);
  }, [isGenerating]);

  const cooldownActive = cooldownRemaining > 0;
  const cooldownFormatted = cooldownActive
    ? `${Math.floor(cooldownRemaining / 60000)}:${String(Math.floor((cooldownRemaining % 60000) / 1000)).padStart(2, '0')}`
    : '';

  /* Generation handler */
  const handleGenerate = useCallback(async () => {
    if (!profile || isGenerating || cooldownActive) return;

    try {
      const { correlationId } = await api.generate({
        childProfileId: profile.id,
        mode: profile.preferredMode,
        duration,
        themes: profile.themes,
        characters: profile.favoriteCharacters,
      });

      dispatch(startGeneration(correlationId));

      /* Poll for completion */
      const poll = async () => {
        const status = await api.getGenerationStatus(correlationId);
        if (status.status === 'complete' && status.story) {
          dispatch(addStory(status.story));
          dispatch(completeGeneration());
          saveCooldownState({
            ...getCooldownState(),
            lastGenerationAt: new Date().toISOString(),
          });
        } else if (status.status === 'failed') {
          dispatch(failGeneration(status.error ?? 'Generation failed'));
        } else {
          if (status.progress) dispatch(setProgress(status.progress));
          setTimeout(poll, 3000);
        }
      };
      poll();
    } catch (err) {
      dispatch(failGeneration(err instanceof Error ? err.message : 'Network error'));
    }
  }, [profile, isGenerating, cooldownActive, duration, dispatch]);

  return (
    <div
      style={{
        background: 'var(--color-surface)',
        borderRadius: 'var(--radius-md)',
        border: '1px solid var(--color-border)',
        padding: 'var(--spacing-lg)',
        boxShadow: '0 2px 8px var(--color-shadow)',
      }}
    >
      {/* Profile context */}
      {profile && (
        <p
          style={{
            margin: '0 0 var(--spacing-sm)',
            fontSize: 13,
            color: 'var(--color-text-secondary)',
          }}
        >
          Story for <strong>{profile.name}</strong> ·{' '}
          {profile.preferredMode === 'series' ? 'Series' : 'One-shot'} · {duration} min
        </p>
      )}

      {/* CTA or status */}
      {isGenerating ? (
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 12,
            padding: '12px 0',
          }}
        >
          <Spinner />
          <span style={{ fontSize: 14, color: 'var(--color-text-secondary)' }}>
            {progressMessage}
          </span>
        </div>
      ) : cooldownActive ? (
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 8,
            padding: '12px 0',
            color: 'var(--color-text-secondary)',
          }}
        >
          <Timer size={18} />
          <span style={{ fontSize: 14 }}>
            Next story available in <strong>{cooldownFormatted}</strong>
          </span>
        </div>
      ) : (
        <Button
          fullWidth
          onClick={handleGenerate}
          disabled={!profile}
          style={{ marginTop: 4 }}
        >
          <Sparkles size={18} />
          Generate Story
        </Button>
      )}
    </div>
  );
}
