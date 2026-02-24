/* ───────────────────────────────────────────────
 * ParallaxPoster – multi-layer gyro/pointer parallax
 * ─────────────────────────────────────────────── */

import { useEffect, useRef, useState, useCallback, useMemo } from 'react';
import type { PosterLayer } from '../../types';
import { generateProceduralLayers, type FallbackProps } from './ProceduralPoster';

interface ParallaxPosterProps {
  layers: PosterLayer[];
  width?: number | string;
  height?: number | string;
  reducedMotion?: boolean;
  /** When provided and `layers` is empty, procedural SVG layers are generated as fallback. */
  fallbackProps?: FallbackProps;
}

const SPEED: Record<string, number> = {
  BACKGROUND: 0.2,
  MIDGROUND_1: 0.5,
  MIDGROUND_2: 0.65,
  FOREGROUND: 1.0,
  PARTICLES: 1.3,
};

const MAX_OFFSET = 20; // px

export default function ParallaxPoster({
  layers,
  width = '100%',
  height = 320,
  reducedMotion = false,
  fallbackProps,
}: ParallaxPosterProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  const rafRef = useRef(0);
  const targetRef = useRef({ x: 0, y: 0 });

  /* Smooth interpolation loop */
  const animate = useCallback(() => {
    setOffset((prev) => ({
      x: prev.x + (targetRef.current.x - prev.x) * 0.08,
      y: prev.y + (targetRef.current.y - prev.y) * 0.08,
    }));
    rafRef.current = requestAnimationFrame(animate);
  }, []);

  useEffect(() => {
    if (reducedMotion) return;
    rafRef.current = requestAnimationFrame(animate);
    return () => cancelAnimationFrame(rafRef.current);
  }, [animate, reducedMotion]);

  /* Device orientation (gyroscope) */
  useEffect(() => {
    if (reducedMotion) return;

    const handler = (e: DeviceOrientationEvent) => {
      const x = (e.gamma ?? 0) / 45; // -1..1
      const y = (e.beta ?? 0) / 45;
      targetRef.current = {
        x: Math.max(-1, Math.min(1, x)) * MAX_OFFSET,
        y: Math.max(-1, Math.min(1, y - 0.5)) * MAX_OFFSET,
      };
    };

    window.addEventListener('deviceorientation', handler);
    return () => window.removeEventListener('deviceorientation', handler);
  }, [reducedMotion]);

  /* Pointer/mouse fallback */
  const handlePointerMove = useCallback(
    (e: React.PointerEvent) => {
      if (reducedMotion || !containerRef.current) return;
      const rect = containerRef.current.getBoundingClientRect();
      const x = ((e.clientX - rect.left) / rect.width - 0.5) * 2; // -1..1
      const y = ((e.clientY - rect.top) / rect.height - 0.5) * 2;
      targetRef.current = {
        x: x * MAX_OFFSET,
        y: y * MAX_OFFSET,
      };
    },
    [reducedMotion],
  );

  /* Resolve effective layers: use procedural fallback when empty */
  const effectiveLayers = useMemo(() => {
    if (layers.length > 0) return layers;
    if (fallbackProps) return generateProceduralLayers(fallbackProps);
    return [];
  }, [layers, fallbackProps]);

  const sorted = [...effectiveLayers].sort((a, b) => {
    const order = ['BACKGROUND', 'MIDGROUND_1', 'MIDGROUND_2', 'FOREGROUND', 'PARTICLES'];
    return order.indexOf(a.role) - order.indexOf(b.role);
  });

  return (
    <div
      ref={containerRef}
      onPointerMove={handlePointerMove}
      style={{
        position: 'relative',
        width,
        height,
        overflow: 'hidden',
        borderRadius: 'var(--radius-md)',
        background: 'linear-gradient(135deg, var(--color-accent) 0%, #818CF8 100%)',
      }}
    >
      {sorted.map((layer, i) => {
        const speed = SPEED[layer.role] ?? 0.5;
        const tx = reducedMotion ? 0 : offset.x * speed;
        const ty = reducedMotion ? 0 : offset.y * speed;

        return (
          <div
            key={`${layer.role}-${i}`}
            style={{
              position: 'absolute',
              inset: -MAX_OFFSET,
              backgroundImage: layer.imageBase64
                ? `url(${layer.imageBase64.startsWith('data:') ? layer.imageBase64 : `data:image/png;base64,${layer.imageBase64}`})`
                : undefined,
              backgroundSize: 'cover',
              backgroundPosition: 'center',
              transform: `translate3d(${tx}px, ${ty}px, 0)`,
              willChange: reducedMotion ? undefined : 'transform',
              zIndex: i,
            }}
          />
        );
      })}
    </div>
  );
}
