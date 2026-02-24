/* ───────────────────────────────────────────────
 * StoryPlayer – audio playback controls
 * ─────────────────────────────────────────────── */

import { useRef, useState, useEffect, useCallback } from 'react';
import { Play, Pause, Square, Volume2, VolumeX } from 'lucide-react';
import { useAppDispatch } from '../../store/hooks';
import { updatePlaybackPosition } from '../../store/slices/storiesSlice';
import type { Story } from '../../types';

interface StoryPlayerProps {
  story: Story;
}

function formatTime(sec: number): string {
  const m = Math.floor(sec / 60);
  const s = Math.floor(sec % 60);
  return `${m}:${s.toString().padStart(2, '0')}`;
}

export default function StoryPlayer({ story }: StoryPlayerProps) {
  const dispatch = useAppDispatch();
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const [playing, setPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(story.playbackPosition ?? 0);
  const [duration, setDuration] = useState(0);
  const [volume, setVolume] = useState(1);

  /* Create audio element from base64 */
  useEffect(() => {
    if (!story.audioBase64) return;

    const audio = new Audio(`data:audio/mp3;base64,${story.audioBase64}`);
    audio.currentTime = story.playbackPosition ?? 0;
    audioRef.current = audio;

    audio.addEventListener('loadedmetadata', () => setDuration(audio.duration));
    audio.addEventListener('timeupdate', () => setCurrentTime(audio.currentTime));
    audio.addEventListener('ended', () => setPlaying(false));

    return () => {
      audio.pause();
      audio.src = '';
    };
  }, [story.audioBase64, story.playbackPosition]);

  /* Media Session API for lock-screen controls */
  useEffect(() => {
    if (!('mediaSession' in navigator)) return;
    navigator.mediaSession.metadata = new MediaMetadata({
      title: story.title,
      artist: 'TaleWeaver',
      album: story.seriesId ? `Series ${story.seriesId}` : 'Story',
    });
    navigator.mediaSession.setActionHandler('play', () => handlePlay());
    navigator.mediaSession.setActionHandler('pause', () => handlePause());
    navigator.mediaSession.setActionHandler('stop', () => handleStop());
  });

  const handlePlay = useCallback(() => {
    audioRef.current?.play();
    setPlaying(true);
  }, []);

  const handlePause = useCallback(() => {
    audioRef.current?.pause();
    setPlaying(false);
    if (audioRef.current) {
      dispatch(
        updatePlaybackPosition({ id: story.id, position: audioRef.current.currentTime }),
      );
    }
  }, [dispatch, story.id]);

  const handleStop = useCallback(() => {
    if (audioRef.current) {
      audioRef.current.pause();
      audioRef.current.currentTime = 0;
    }
    setPlaying(false);
    setCurrentTime(0);
    dispatch(updatePlaybackPosition({ id: story.id, position: 0 }));
  }, [dispatch, story.id]);

  const handleSeek = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const t = Number(e.target.value);
      if (audioRef.current) audioRef.current.currentTime = t;
      setCurrentTime(t);
    },
    [],
  );

  const handleVolume = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const v = Number(e.target.value);
    setVolume(v);
    if (audioRef.current) audioRef.current.volume = v;
  }, []);

  const btnStyle: React.CSSProperties = {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: 44,
    height: 44,
    border: 'none',
    borderRadius: '50%',
    cursor: 'pointer',
    background: 'var(--color-bg)',
    color: 'var(--color-text)',
  };

  if (!story.audioBase64) {
    return (
      <p
        style={{
          textAlign: 'center',
          color: 'var(--color-text-secondary)',
          fontSize: 14,
          padding: 'var(--spacing-md)',
        }}
      >
        Audio not available yet.
      </p>
    );
  }

  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--spacing-md)',
        padding: 'var(--spacing-md) 0',
      }}
    >
      {/* Progress bar */}
      <div>
        <input
          type="range"
          min={0}
          max={duration || 1}
          step={0.1}
          value={currentTime}
          onChange={handleSeek}
          aria-label="Seek"
          style={{
            width: '100%',
            appearance: 'none',
            height: 4,
            borderRadius: 2,
            background: `linear-gradient(to right, var(--color-accent) ${(currentTime / (duration || 1)) * 100}%, var(--color-border) ${(currentTime / (duration || 1)) * 100}%)`,
            cursor: 'pointer',
            minHeight: 44,
          }}
        />
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            fontSize: 12,
            color: 'var(--color-text-secondary)',
          }}
        >
          <span>{formatTime(currentTime)}</span>
          <span>{formatTime(duration)}</span>
        </div>
      </div>

      {/* Controls */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 'var(--spacing-md)',
        }}
      >
        <button onClick={handleStop} style={btnStyle} aria-label="Stop">
          <Square size={18} />
        </button>
        <button
          onClick={playing ? handlePause : handlePlay}
          style={{
            ...btnStyle,
            width: 56,
            height: 56,
            background: 'var(--color-accent)',
            color: '#fff',
          }}
          aria-label={playing ? 'Pause' : 'Play'}
        >
          {playing ? <Pause size={22} /> : <Play size={22} />}
        </button>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          {volume === 0 ? <VolumeX size={16} /> : <Volume2 size={16} />}
          <input
            type="range"
            min={0}
            max={1}
            step={0.05}
            value={volume}
            onChange={handleVolume}
            aria-label="Volume"
            style={{ width: 60, minHeight: 44 }}
          />
        </div>
      </div>
    </div>
  );
}
