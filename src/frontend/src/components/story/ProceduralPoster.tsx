/* ───────────────────────────────────────────────
 * ProceduralPoster – deterministic SVG fallback
 *
 * Generates 3-5 low-poly SVG layers from scene
 * metadata when AI image generation fails.
 * Renders in < 200 ms with seeded randomness.
 * ─────────────────────────────────────────────── */

import { useMemo } from 'react';
import type { PosterLayer } from '../../types';

/* ── Scene taxonomy (~20 types) ─────────────── */

const SCENE_TAXONOMY = [
  'forest', 'space', 'underwater', 'castle', 'mountains', 'city', 'beach',
  'arctic', 'desert', 'jungle', 'farm', 'meadow', 'cave', 'sky', 'village',
  'garden', 'river', 'volcano', 'island', 'library',
] as const;

type SceneType = typeof SCENE_TAXONOMY[number];

/* ── Color palettes (5 colours: bg → accent) ── */

const PALETTES: Record<SceneType, string[]> = {
  forest:     ['#1a2a1a', '#2d4a2d', '#3d6b3d', '#5a8a5a', '#8fbc8f'],
  space:      ['#0a0a2e', '#1a1a4e', '#2d2d6e', '#4444aa', '#6666cc'],
  underwater: ['#0a2a3a', '#1a4a5a', '#2d6a7a', '#4a8a9a', '#6abacc'],
  castle:     ['#2a2a3a', '#4a4a5a', '#6a6a7a', '#8a8a9a', '#aaaacc'],
  mountains:  ['#2a3a4a', '#4a5a6a', '#6a7a8a', '#8a9aaa', '#aabbcc'],
  city:       ['#1a1a2a', '#3a3a4a', '#5a5a6a', '#7a7a8a', '#9a9aaa'],
  beach:      ['#2a4a6a', '#4a7a9a', '#8ab4d0', '#d4a574', '#f0d4a4'],
  arctic:     ['#c8d8e8', '#d8e8f0', '#e0f0f8', '#e8f4fc', '#f0f8ff'],
  desert:     ['#6a4a2a', '#8a6a3a', '#aa8a4a', '#ccaa6a', '#eeccaa'],
  jungle:     ['#0a2a0a', '#1a4a1a', '#2a6a2a', '#3a8a3a', '#5aaa5a'],
  farm:       ['#3a5a2a', '#5a7a3a', '#7a9a4a', '#aaba6a', '#ccdd8a'],
  meadow:     ['#2a5a2a', '#4a8a4a', '#6aaa6a', '#9acc9a', '#cceecc'],
  cave:       ['#1a1a1a', '#2a2a2a', '#3a3a3a', '#4a4a4a', '#5a5a5a'],
  sky:        ['#4a8acc', '#6aaadd', '#8accee', '#aaddff', '#cceeff'],
  village:    ['#4a3a2a', '#6a5a3a', '#8a7a4a', '#aa9a6a', '#ccbb8a'],
  garden:     ['#2a4a2a', '#4a7a3a', '#6a9a4a', '#8aba6a', '#aadd8a'],
  river:      ['#1a3a5a', '#2a5a7a', '#4a7a9a', '#6a9aba', '#8abada'],
  volcano:    ['#3a1a0a', '#5a2a1a', '#8a3a1a', '#cc4a1a', '#ff6a2a'],
  island:     ['#2a6a8a', '#4a8aaa', '#6aaacc', '#aaccdd', '#d4ddaa'],
  library:    ['#3a2a1a', '#5a4a2a', '#7a6a3a', '#9a8a4a', '#baba6a'],
};

/* ── Seeded PRNG (deterministic from string) ── */

function seededRandom(seed: string): () => number {
  let hash = 0;
  for (let i = 0; i < seed.length; i++) {
    hash = ((hash << 5) - hash + seed.charCodeAt(i)) | 0;
  }
  return () => {
    hash = (hash * 16807) % 2147483647;
    return (hash & 0x7fffffff) / 0x7fffffff;
  };
}

/* ── Keyword → scene type mapping ─────────────
 * First: exact taxonomy match in combined text.
 * Then:  regex-based keyword fallback.
 * Default: 'meadow' (calm neutral).
 * ──────────────────────────────────────────── */

const KEYWORD_MAP: Array<[RegExp, SceneType]> = [
  [/tree|wood|leaf/,                     'forest'],
  [/star|planet|galaxy|rocket|moon/,     'space'],
  [/ocean|sea|fish|coral|whale/,         'underwater'],
  [/king|queen|knight|tower|throne/,     'castle'],
  [/peak|cliff|alpine|snow.*mountain/,   'mountains'],
  [/building|skyscraper|street|urban/,   'city'],
  [/sand|wave|coast|shore|surf/,         'beach'],
  [/ice|frozen|polar|penguin/,           'arctic'],
  [/dry|cactus|dune|arid/,              'desert'],
  [/tropical|vine|parrot|monkey/,        'jungle'],
  [/barn|cow|chicken|crop|harvest/,      'farm'],
  [/flower|grass|butterfly/,             'meadow'],
  [/dark|underground|tunnel|crystal/,    'cave'],
  [/cloud|fly|bird|wind|balloon/,        'sky'],
  [/house|cottage|market|town/,          'village'],
  [/rose|bloom|petal|plant/,             'garden'],
  [/stream|creek|waterfall|bridge/,      'river'],
  [/lava|eruption|magma|fire/,           'volcano'],
  [/palm|treasure|pirate/,              'island'],
  [/book|shelf|reading|story|wisdom/,    'library'],
];

function mapToSceneType(setting: string, mood: string): SceneType {
  const text = `${setting} ${mood}`.toLowerCase();

  // Direct taxonomy match
  for (const scene of SCENE_TAXONOMY) {
    if (text.includes(scene)) return scene;
  }

  // Keyword-based fallback
  for (const [re, scene] of KEYWORD_MAP) {
    if (re.test(text)) return scene;
  }

  return 'meadow';
}

/* ── Low-poly polygon generator ───────────── */

function generatePolygons(
  rng: () => number,
  count: number,
  width: number,
  height: number,
): string[] {
  const polygons: string[] = [];
  for (let i = 0; i < count; i++) {
    const cx = rng() * width;
    const cy = rng() * height;
    const r = 30 + rng() * 80;
    const sides = 3 + Math.floor(rng() * 4); // 3-6 sides
    const points: string[] = [];
    for (let j = 0; j < sides; j++) {
      const angle = (j / sides) * Math.PI * 2 + rng() * 0.5;
      const pr = r * (0.6 + rng() * 0.4);
      points.push(
        `${(cx + Math.cos(angle) * pr).toFixed(1)},${(cy + Math.sin(angle) * pr).toFixed(1)}`,
      );
    }
    polygons.push(points.join(' '));
  }
  return polygons;
}

/* ── Public interfaces ────────────────────── */

export interface ProceduralPosterProps {
  storyId: string;
  seriesId?: string;
  setting: string;
  mood: string;
  paletteSeed?: string;
  width?: number;
  height?: number;
}

/** Props needed by ParallaxPoster for the procedural fallback. */
export type FallbackProps = ProceduralPosterProps;

/* ── Layer definitions (role → depth config) ─ */

const LAYER_DEFS = [
  { role: 'BACKGROUND'  as const, polygonCount: 8,  paletteIdx: 0, opacity: 1    },
  { role: 'MIDGROUND_1' as const, polygonCount: 6,  paletteIdx: 1, opacity: 0.85 },
  { role: 'MIDGROUND_2' as const, polygonCount: 5,  paletteIdx: 2, opacity: 0.75 },
  { role: 'FOREGROUND'  as const, polygonCount: 4,  paletteIdx: 3, opacity: 0.9  },
  { role: 'PARTICLES'   as const, polygonCount: 12, paletteIdx: 4, opacity: 0.5  },
];

/* ── generateProceduralLayers ─────────────────
 * Returns PosterLayer[] compatible with ParallaxPoster.
 * Each layer is an SVG encoded as a data-URI in imageBase64.
 * ──────────────────────────────────────────── */

export function generateProceduralLayers(props: ProceduralPosterProps): PosterLayer[] {
  const { storyId, seriesId, setting, mood, paletteSeed, width = 1024, height = 1024 } = props;
  const sceneType = mapToSceneType(setting, mood);
  const palette = PALETTES[sceneType];
  const seed = `${seriesId ?? 'oneshot'}-${storyId}-${paletteSeed ?? sceneType}`;
  const rng = seededRandom(seed);

  return LAYER_DEFS.map((def) => {
    const polygons = generatePolygons(rng, def.polygonCount, width, height);

    const svgParts = [
      `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">`,
    ];

    // Background layer gets a full rect fill
    if (def.role === 'BACKGROUND') {
      svgParts.push(`<rect width="${width}" height="${height}" fill="${palette[0]}"/>`);
    }

    svgParts.push(`<g opacity="${def.opacity}">`);
    for (const pts of polygons) {
      const fill = palette[Math.floor(rng() * palette.length)];
      svgParts.push(`<polygon points="${pts}" fill="${fill}"/>`);
    }
    svgParts.push('</g></svg>');

    const svgStr = svgParts.join('');
    const base64 = btoa(svgStr);

    return {
      role: def.role,
      imageBase64: `data:image/svg+xml;base64,${base64}`,
      width,
      height,
    };
  });
}

/* ── React component (inline SVG, no base64) ─ */

export default function ProceduralPoster({
  storyId,
  seriesId,
  setting,
  mood,
  paletteSeed,
  width = 400,
  height = 400,
}: ProceduralPosterProps) {
  const sceneType = useMemo(() => mapToSceneType(setting, mood), [setting, mood]);
  const palette = PALETTES[sceneType];
  const seed = `${seriesId ?? 'oneshot'}-${storyId}-${paletteSeed ?? sceneType}`;

  const layers = useMemo(() => {
    const rng = seededRandom(seed);

    return LAYER_DEFS.map((def) => ({
      ...def,
      polygons: generatePolygons(rng, def.polygonCount, width, height),
      fills: Array.from({ length: def.polygonCount }, () =>
        palette[def.paletteIdx],
      ),
    }));
  }, [seed, palette, width, height]);

  return (
    <svg
      width={width}
      height={height}
      viewBox={`0 0 ${width} ${height}`}
      role="img"
      aria-label={`Procedural poster: ${sceneType}`}
      style={{ borderRadius: 'var(--radius-md)', display: 'block' }}
    >
      {/* Background fill */}
      <rect width={width} height={height} fill={palette[0]} />

      {layers.map((layer) => (
        <g key={layer.role} opacity={layer.opacity}>
          {layer.polygons.map((pts, pi) => (
            <polygon key={pi} points={pts} fill={layer.fills[pi]} />
          ))}
        </g>
      ))}
    </svg>
  );
}

export { mapToSceneType, SCENE_TAXONOMY, PALETTES };
export type { SceneType };
