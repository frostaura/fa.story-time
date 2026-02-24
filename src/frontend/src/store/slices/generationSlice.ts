/* ───────────────────────────────────────────────
 * generationSlice – in-progress generation state
 * ─────────────────────────────────────────────── */

import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

interface GenerationState {
  isGenerating: boolean;
  correlationId: string | null;
  progressMessage: string | null;
  error: string | null;
}

const initialState: GenerationState = {
  isGenerating: false,
  correlationId: null,
  progressMessage: null,
  error: null,
};

const generationSlice = createSlice({
  name: 'generation',
  initialState,
  reducers: {
    startGeneration(state, action: PayloadAction<string>) {
      state.isGenerating = true;
      state.correlationId = action.payload;
      state.progressMessage = 'Starting story generation…';
      state.error = null;
    },
    setProgress(state, action: PayloadAction<string>) {
      state.progressMessage = action.payload;
    },
    completeGeneration(state) {
      state.isGenerating = false;
      state.progressMessage = null;
    },
    failGeneration(state, action: PayloadAction<string>) {
      state.isGenerating = false;
      state.progressMessage = null;
      state.error = action.payload;
    },
    resetGeneration(state) {
      state.isGenerating = false;
      state.correlationId = null;
      state.progressMessage = null;
      state.error = null;
    },
  },
});

export const {
  startGeneration,
  setProgress,
  completeGeneration,
  failGeneration,
  resetGeneration,
} = generationSlice.actions;
export default generationSlice.reducer;
