/* ───────────────────────────────────────────────
 * Redux store
 * ─────────────────────────────────────────────── */

import { configureStore } from '@reduxjs/toolkit';
import appReducer from './slices/appSlice';
import storiesReducer from './slices/storiesSlice';
import generationReducer from './slices/generationSlice';

export const store = configureStore({
  reducer: {
    app: appReducer,
    stories: storiesReducer,
    generation: generationReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
