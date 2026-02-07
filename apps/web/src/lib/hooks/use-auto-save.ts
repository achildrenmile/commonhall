import { useEffect, useRef, useState, useCallback } from 'react';

export type AutoSaveStatus = 'idle' | 'saving' | 'saved' | 'error';

interface UseAutoSaveOptions<T> {
  data: T;
  onSave: (data: T) => Promise<void>;
  interval?: number; // milliseconds, default 30000 (30 seconds)
  debounce?: number; // milliseconds, default 2000 (2 seconds)
  enabled?: boolean;
}

interface UseAutoSaveResult {
  status: AutoSaveStatus;
  lastSavedAt: Date | null;
  error: Error | null;
  save: () => Promise<void>;
  isDirty: boolean;
}

export function useAutoSave<T>({
  data,
  onSave,
  interval = 30000,
  debounce = 2000,
  enabled = true,
}: UseAutoSaveOptions<T>): UseAutoSaveResult {
  const [status, setStatus] = useState<AutoSaveStatus>('idle');
  const [lastSavedAt, setLastSavedAt] = useState<Date | null>(null);
  const [error, setError] = useState<Error | null>(null);
  const [isDirty, setIsDirty] = useState(false);

  const lastSavedDataRef = useRef<string | null>(null);
  const debounceTimerRef = useRef<NodeJS.Timeout | null>(null);
  const intervalTimerRef = useRef<NodeJS.Timeout | null>(null);

  // Serialize data for comparison
  const serializedData = JSON.stringify(data);

  // Check if data has changed since last save
  useEffect(() => {
    if (lastSavedDataRef.current !== null && lastSavedDataRef.current !== serializedData) {
      setIsDirty(true);
    }
  }, [serializedData]);

  // Save function
  const save = useCallback(async () => {
    if (!enabled) return;

    // Don't save if nothing changed
    if (lastSavedDataRef.current === serializedData) {
      return;
    }

    setStatus('saving');
    setError(null);

    try {
      await onSave(data);
      lastSavedDataRef.current = serializedData;
      setLastSavedAt(new Date());
      setStatus('saved');
      setIsDirty(false);

      // Reset status after a brief moment
      setTimeout(() => {
        setStatus('idle');
      }, 2000);
    } catch (err) {
      setError(err instanceof Error ? err : new Error('Save failed'));
      setStatus('error');
    }
  }, [data, serializedData, onSave, enabled]);

  // Debounced save on data change
  useEffect(() => {
    if (!enabled) return;

    // Skip initial render - wait until we have a baseline
    if (lastSavedDataRef.current === null) {
      lastSavedDataRef.current = serializedData;
      return;
    }

    // Only trigger debounce if data has changed
    if (lastSavedDataRef.current === serializedData) {
      return;
    }

    // Clear existing debounce timer
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }

    // Set up debounce timer
    debounceTimerRef.current = setTimeout(() => {
      save();
    }, debounce);

    return () => {
      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current);
      }
    };
  }, [serializedData, debounce, enabled, save]);

  // Periodic save interval
  useEffect(() => {
    if (!enabled) return;

    intervalTimerRef.current = setInterval(() => {
      if (isDirty) {
        save();
      }
    }, interval);

    return () => {
      if (intervalTimerRef.current) {
        clearInterval(intervalTimerRef.current);
      }
    };
  }, [interval, isDirty, enabled, save]);

  // Save on unmount if dirty
  useEffect(() => {
    return () => {
      if (isDirty && enabled) {
        // Fire and forget - can't await in cleanup
        onSave(data).catch(console.error);
      }
    };
  }, [isDirty, enabled, data, onSave]);

  return {
    status,
    lastSavedAt,
    error,
    save,
    isDirty,
  };
}
