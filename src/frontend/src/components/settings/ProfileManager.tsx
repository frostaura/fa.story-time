/* ───────────────────────────────────────────────
 * ProfileManager – list, edit, delete child profiles
 * ─────────────────────────────────────────────── */

import { useState } from 'react';
import { Plus, Trash2, Edit3 } from 'lucide-react';
import Button from '../common/Button';
import { getProfiles, saveProfile, deleteProfile } from '../../services/localStorage';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { setCurrentProfile } from '../../store/slices/appSlice';
import type { ChildProfile } from '../../types';

export default function ProfileManager() {
  const dispatch = useAppDispatch();
  const { currentProfileId } = useAppSelector((s) => s.app);
  const [profiles, setProfiles] = useState(getProfiles());
  const [editing, setEditing] = useState<string | null>(null);
  const [editName, setEditName] = useState('');
  const [editAge, setEditAge] = useState(5);

  const refresh = () => setProfiles(getProfiles());

  const startEdit = (p: ChildProfile) => {
    setEditing(p.id);
    setEditName(p.name);
    setEditAge(p.age);
  };

  const saveEdit = (p: ChildProfile) => {
    saveProfile({ ...p, name: editName, age: editAge });
    setEditing(null);
    refresh();
  };

  const handleDelete = (id: string) => {
    deleteProfile(id);
    refresh();
    if (id === currentProfileId && profiles.length > 1) {
      const remaining = profiles.filter((p) => p.id !== id);
      if (remaining[0]) dispatch(setCurrentProfile(remaining[0].id));
    }
  };

  const handleAdd = () => {
    const newProfile: ChildProfile = {
      id: crypto.randomUUID(),
      name: 'New Child',
      age: 5,
      themes: [],
      favoriteCharacters: [],
      narratorVoice: 'warm-female',
      defaultDuration: 5,
      preferredMode: 'oneshot',
    };
    saveProfile(newProfile);
    refresh();
    dispatch(setCurrentProfile(newProfile.id));
    startEdit(newProfile);
  };

  const inputStyle: React.CSSProperties = {
    padding: '8px 12px',
    border: '1.5px solid var(--color-border)',
    borderRadius: 'var(--radius-sm)',
    fontSize: 14,
    fontFamily: 'var(--font-sans)',
    background: 'var(--color-bg)',
    color: 'var(--color-text)',
    minHeight: 36,
  };

  return (
    <div>
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          marginBottom: 'var(--spacing-md)',
        }}
      >
        <h3 style={{ margin: 0, fontSize: 16, fontWeight: 700 }}>Profiles</h3>
        <Button variant="ghost" onClick={handleAdd}>
          <Plus size={16} /> Add
        </Button>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
        {profiles.map((p) => (
          <div
            key={p.id}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 12,
              padding: '10px 14px',
              borderRadius: 'var(--radius-sm)',
              border: '1px solid var(--color-border)',
              background:
                p.id === currentProfileId
                  ? 'rgba(99,102,241,0.04)'
                  : 'transparent',
            }}
          >
            {editing === p.id ? (
              <>
                <input
                  style={{ ...inputStyle, flex: 1 }}
                  value={editName}
                  onChange={(e) => setEditName(e.target.value)}
                  autoFocus
                />
                <input
                  type="number"
                  style={{ ...inputStyle, width: 50 }}
                  value={editAge}
                  min={1}
                  max={16}
                  onChange={(e) => setEditAge(Number(e.target.value))}
                />
                <Button
                  variant="secondary"
                  onClick={() => saveEdit(p)}
                  style={{ padding: '6px 12px', minHeight: 36 }}
                >
                  Save
                </Button>
              </>
            ) : (
              <>
                <span style={{ flex: 1, fontSize: 14, fontWeight: 500 }}>
                  {p.name} · {p.age}y
                </span>
                <button
                  onClick={() => startEdit(p)}
                  aria-label={`Edit ${p.name}`}
                  style={{
                    background: 'none',
                    border: 'none',
                    cursor: 'pointer',
                    color: 'var(--color-text-secondary)',
                    padding: 8,
                    minHeight: 36,
                  }}
                >
                  <Edit3 size={16} />
                </button>
                <button
                  onClick={() => handleDelete(p.id)}
                  aria-label={`Delete ${p.name}`}
                  style={{
                    background: 'none',
                    border: 'none',
                    cursor: 'pointer',
                    color: '#EF4444',
                    padding: 8,
                    minHeight: 36,
                  }}
                >
                  <Trash2 size={16} />
                </button>
              </>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
