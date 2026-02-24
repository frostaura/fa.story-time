/* ───────────────────────────────────────────────
 * AppLayout – top bar + main content
 * ─────────────────────────────────────────────── */

import { Outlet } from 'react-router-dom';
import TopBar from './TopBar';

export default function AppLayout() {
  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        minHeight: '100dvh',
      }}
    >
      <TopBar />
      <main
        style={{
          flex: 1,
          width: '100%',
          maxWidth: 640,
          margin: '0 auto',
          padding: 'var(--spacing-lg)',
        }}
      >
        <Outlet />
      </main>
    </div>
  );
}
