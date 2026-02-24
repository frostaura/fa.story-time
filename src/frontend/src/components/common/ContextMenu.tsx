/* ───────────────────────────────────────────────
 * ContextMenu – long-press / right-click menu
 * ─────────────────────────────────────────────── */

import { type CSSProperties, type ReactNode, useEffect, useRef, useState } from 'react';
import { AnimatePresence, motion } from 'framer-motion';

export interface ContextMenuItem {
  label: string;
  icon?: ReactNode;
  onClick: () => void;
  destructive?: boolean;
}

interface ContextMenuProps {
  items: ContextMenuItem[];
  children: ReactNode;
}

const LONG_PRESS_MS = 500;

export default function ContextMenu({ items, children }: ContextMenuProps) {
  const [open, setOpen] = useState(false);
  const [position, setPosition] = useState({ x: 0, y: 0 });
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  /* Long-press detection */
  const startPress = (clientX: number, clientY: number) => {
    timerRef.current = setTimeout(() => {
      setPosition({ x: clientX, y: clientY });
      setOpen(true);
    }, LONG_PRESS_MS);
  };

  const cancelPress = () => {
    if (timerRef.current) clearTimeout(timerRef.current);
  };

  /* Close on outside click */
  useEffect(() => {
    if (!open) return;
    const handler = () => setOpen(false);
    document.addEventListener('click', handler);
    document.addEventListener('scroll', handler, true);
    return () => {
      document.removeEventListener('click', handler);
      document.removeEventListener('scroll', handler, true);
    };
  }, [open]);

  const menuStyle: CSSProperties = {
    position: 'fixed',
    top: position.y,
    left: position.x,
    minWidth: 180,
    background: 'var(--color-surface)',
    borderRadius: 'var(--radius-md)',
    boxShadow: '0 8px 32px var(--color-shadow)',
    border: '1px solid var(--color-border)',
    padding: '6px 0',
    zIndex: 2000,
    overflow: 'hidden',
  };

  return (
    <div
      ref={containerRef}
      onPointerDown={(e) => startPress(e.clientX, e.clientY)}
      onPointerUp={cancelPress}
      onPointerLeave={cancelPress}
      onContextMenu={(e) => {
        e.preventDefault();
        setPosition({ x: e.clientX, y: e.clientY });
        setOpen(true);
      }}
      style={{ touchAction: 'none' }}
    >
      {children}

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, scale: 0.92 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.92 }}
            transition={{ duration: 0.12 }}
            style={menuStyle}
            role="menu"
          >
            {items.map((item) => (
              <button
                key={item.label}
                role="menuitem"
                onClick={(e) => {
                  e.stopPropagation();
                  setOpen(false);
                  item.onClick();
                }}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 10,
                  width: '100%',
                  padding: '10px 16px',
                  background: 'none',
                  border: 'none',
                  fontSize: 14,
                  fontWeight: 500,
                  color: item.destructive
                    ? '#EF4444'
                    : 'var(--color-text)',
                  cursor: 'pointer',
                  minHeight: 44,
                  fontFamily: 'var(--font-sans)',
                }}
              >
                {item.icon}
                {item.label}
              </button>
            ))}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
