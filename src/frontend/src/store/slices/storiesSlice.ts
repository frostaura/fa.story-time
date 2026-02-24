/* ───────────────────────────────────────────────
 * storiesSlice – story library state
 * ─────────────────────────────────────────────── */

import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type { Story } from '../../types';
import {
  getStories,
  saveStory as lsSaveStory,
  deleteStory as lsDeleteStory,
  toggleFavorite as lsToggleFavorite,
} from '../../services/localStorage';

interface StoriesState {
  items: Story[];
  loading: boolean;
}

const initialState: StoriesState = {
  items: getStories(),
  loading: false,
};

const storiesSlice = createSlice({
  name: 'stories',
  initialState,
  reducers: {
    setLoading(state, action: PayloadAction<boolean>) {
      state.loading = action.payload;
    },
    addStory(state, action: PayloadAction<Story>) {
      lsSaveStory(action.payload);
      const idx = state.items.findIndex((s) => s.id === action.payload.id);
      if (idx >= 0) {
        state.items[idx] = action.payload;
      } else {
        state.items.unshift(action.payload);
      }
    },
    removeStory(state, action: PayloadAction<string>) {
      lsDeleteStory(action.payload);
      state.items = state.items.filter((s) => s.id !== action.payload);
    },
    toggleFavorite(state, action: PayloadAction<string>) {
      lsToggleFavorite(action.payload);
      const story = state.items.find((s) => s.id === action.payload);
      if (story) story.isFavorite = !story.isFavorite;
    },
    updatePlaybackPosition(
      state,
      action: PayloadAction<{ id: string; position: number }>,
    ) {
      const story = state.items.find((s) => s.id === action.payload.id);
      if (story) {
        story.playbackPosition = action.payload.position;
        lsSaveStory(story);
      }
    },
    refreshFromStorage(state) {
      state.items = getStories();
    },
  },
});

export const {
  setLoading,
  addStory,
  removeStory,
  toggleFavorite,
  updatePlaybackPosition,
  refreshFromStorage,
} = storiesSlice.actions;
export default storiesSlice.reducer;
