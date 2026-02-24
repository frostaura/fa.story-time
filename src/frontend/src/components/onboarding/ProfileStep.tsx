/* ───────────────────────────────────────────────
 * ProfileStep – child name, age, themes, characters
 * ─────────────────────────────────────────────── */

import { useState, type CSSProperties } from 'react';
import Button from '../common/Button';

interface ProfileStepProps {
  onNext: (data: {
    name: string;
    age: number;
    themes: string[];
    favoriteCharacters: string[];
  }) => void;
}

const THEME_OPTIONS = [
  'Adventure', 'Fantasy', 'Space', 'Animals', 'Friendship',
  'Mystery', 'Pirates', 'Dinosaurs', 'Magic', 'Ocean',
  'Superheroes', 'Nature',
];

const inputStyle: CSSProperties = {
  width: '100%',
  padding: '12px 16px',
  border: '1.5px solid var(--color-border)',
  borderRadius: 'var(--radius-sm)',
  fontSize: 15,
  fontFamily: 'var(--font-sans)',
  background: 'var(--color-bg)',
  color: 'var(--color-text)',
  outline: 'none',
  minHeight: 44,
  boxSizing: 'border-box',
};

export default function ProfileStep({ onNext }: ProfileStepProps) {
  const [name, setName] = useState('');
  const [age, setAge] = useState(5);
  const [themes, setThemes] = useState<string[]>([]);
  const [charInput, setCharInput] = useState('');
  const [characters, setCharacters] = useState<string[]>([]);

  const toggleTheme = (t: string) =>
    setThemes((prev) =>
      prev.includes(t) ? prev.filter((x) => x !== t) : [...prev, t],
    );

  const addCharacter = () => {
    const trimmed = charInput.trim();
    if (trimmed && !characters.includes(trimmed)) {
      setCharacters([...characters, trimmed]);
      setCharInput('');
    }
  };

  const valid = name.trim().length > 0 && age >= 1 && age <= 16;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--spacing-lg)' }}>
      <h2 style={{ margin: 0, fontSize: 22, fontWeight: 700 }}>
        Who's the story for?
      </h2>

      {/* Name */}
      <div>
        <label
          style={{
            display: 'block',
            fontSize: 13,
            fontWeight: 500,
            color: 'var(--color-text-secondary)',
            marginBottom: 6,
          }}
        >
          Child's name
        </label>
        <input
          style={inputStyle}
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="e.g. Emma"
          autoFocus
        />
      </div>

      {/* Age */}
      <div>
        <label
          style={{
            display: 'block',
            fontSize: 13,
            fontWeight: 500,
            color: 'var(--color-text-secondary)',
            marginBottom: 6,
          }}
        >
          Age
        </label>
        <input
          type="number"
          style={{ ...inputStyle, width: 80 }}
          value={age}
          min={1}
          max={16}
          onChange={(e) => setAge(Number(e.target.value))}
        />
      </div>

      {/* Themes */}
      <div>
        <label
          style={{
            display: 'block',
            fontSize: 13,
            fontWeight: 500,
            color: 'var(--color-text-secondary)',
            marginBottom: 8,
          }}
        >
          Favorite themes
        </label>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
          {THEME_OPTIONS.map((t) => (
            <button
              key={t}
              onClick={() => toggleTheme(t)}
              style={{
                padding: '8px 14px',
                borderRadius: 20,
                fontSize: 13,
                fontWeight: 500,
                border: '1.5px solid',
                borderColor: themes.includes(t) ? 'var(--color-accent)' : 'var(--color-border)',
                background: themes.includes(t) ? 'var(--color-accent)' : 'transparent',
                color: themes.includes(t) ? '#fff' : 'var(--color-text)',
                cursor: 'pointer',
                minHeight: 36,
                fontFamily: 'var(--font-sans)',
              }}
            >
              {t}
            </button>
          ))}
        </div>
      </div>

      {/* Characters */}
      <div>
        <label
          style={{
            display: 'block',
            fontSize: 13,
            fontWeight: 500,
            color: 'var(--color-text-secondary)',
            marginBottom: 6,
          }}
        >
          Favorite characters (optional)
        </label>
        <div style={{ display: 'flex', gap: 8 }}>
          <input
            style={{ ...inputStyle, flex: 1 }}
            value={charInput}
            onChange={(e) => setCharInput(e.target.value)}
            placeholder="e.g. Luna the cat"
            onKeyDown={(e) => { if (e.key === 'Enter') addCharacter(); }}
          />
          <Button variant="secondary" onClick={addCharacter}>
            Add
          </Button>
        </div>
        {characters.length > 0 && (
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginTop: 8 }}>
            {characters.map((c) => (
              <span
                key={c}
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  gap: 6,
                  padding: '4px 12px',
                  fontSize: 13,
                  borderRadius: 16,
                  background: 'var(--color-bg)',
                  border: '1px solid var(--color-border)',
                }}
              >
                {c}
                <button
                  onClick={() => setCharacters(characters.filter((x) => x !== c))}
                  style={{
                    background: 'none',
                    border: 'none',
                    cursor: 'pointer',
                    fontSize: 14,
                    color: 'var(--color-text-secondary)',
                    padding: 0,
                    lineHeight: 1,
                  }}
                  aria-label={`Remove ${c}`}
                >
                  ×
                </button>
              </span>
            ))}
          </div>
        )}
      </div>

      <Button
        fullWidth
        disabled={!valid}
        onClick={() =>
          onNext({ name: name.trim(), age, themes, favoriteCharacters: characters })
        }
      >
        Next
      </Button>
    </div>
  );
}
