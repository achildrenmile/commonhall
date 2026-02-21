'use client';

import { useEffect, useRef, useCallback, useState } from 'react';
import { HubConnectionBuilder, HubConnection, HubConnectionState } from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { getAccessToken } from '@/lib/api-client';
import type { Message } from '../types';

const API_URL = process.env.NEXT_PUBLIC_API_URL || '';

interface UseChatHubOptions {
  onReceiveMessage?: (message: Message) => void;
  onUserTyping?: (conversationId: string, userId: string, isTyping: boolean) => void;
  onMessageRead?: (conversationId: string, userId: string) => void;
  onUnreadCountUpdated?: (totalUnread: number, conversationUnreads: Record<string, number>) => void;
}

export function useChatHub(options: UseChatHubOptions = {}) {
  const connectionRef = useRef<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [connectionError, setConnectionError] = useState<string | null>(null);
  const queryClient = useQueryClient();
  const optionsRef = useRef(options);

  // Keep options ref updated
  useEffect(() => {
    optionsRef.current = options;
  }, [options]);

  // Build and start connection
  useEffect(() => {
    const token = getAccessToken();
    if (!token) {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/chat`, {
        accessTokenFactory: () => getAccessToken() || '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    connectionRef.current = connection;

    // Event handlers
    connection.on('ReceiveMessage', (message: Message) => {
      optionsRef.current.onReceiveMessage?.(message);
      // Invalidate queries to refresh data
      queryClient.invalidateQueries({ queryKey: ['messages', message.conversationId] });
      queryClient.invalidateQueries({ queryKey: ['conversations'] });
    });

    connection.on('UserTyping', (conversationId: string, userId: string, isTyping: boolean) => {
      optionsRef.current.onUserTyping?.(conversationId, userId, isTyping);
    });

    connection.on('MessageRead', (conversationId: string, userId: string) => {
      optionsRef.current.onMessageRead?.(conversationId, userId);
    });

    connection.on('UnreadCountUpdated', (totalUnread: number, conversationUnreads: Record<string, number>) => {
      optionsRef.current.onUnreadCountUpdated?.(totalUnread, conversationUnreads);
      queryClient.invalidateQueries({ queryKey: ['unread-count'] });
    });

    connection.onreconnecting(() => {
      setIsConnected(false);
      setConnectionError('Reconnecting...');
    });

    connection.onreconnected(() => {
      setIsConnected(true);
      setConnectionError(null);
    });

    connection.onclose(() => {
      setIsConnected(false);
    });

    // Start connection
    connection
      .start()
      .then(() => {
        setIsConnected(true);
        setConnectionError(null);
      })
      .catch((err) => {
        console.error('SignalR Connection Error:', err);
        setConnectionError(err.message || 'Failed to connect');
      });

    return () => {
      connection.stop();
    };
  }, [queryClient]);

  // Send message via SignalR (for real-time delivery)
  const sendMessage = useCallback(async (conversationId: string, body: string, attachments?: string) => {
    const connection = connectionRef.current;
    if (connection?.state === HubConnectionState.Connected) {
      await connection.invoke('SendMessage', conversationId, body, attachments);
    }
  }, []);

  // Start typing indicator
  const startTyping = useCallback(async (conversationId: string) => {
    const connection = connectionRef.current;
    if (connection?.state === HubConnectionState.Connected) {
      await connection.invoke('TypingStarted', conversationId);
    }
  }, []);

  // Stop typing indicator
  const stopTyping = useCallback(async (conversationId: string) => {
    const connection = connectionRef.current;
    if (connection?.state === HubConnectionState.Connected) {
      await connection.invoke('TypingStopped', conversationId);
    }
  }, []);

  // Mark conversation as read
  const markAsRead = useCallback(async (conversationId: string) => {
    const connection = connectionRef.current;
    if (connection?.state === HubConnectionState.Connected) {
      await connection.invoke('MarkAsRead', conversationId);
    }
  }, []);

  // Join a specific conversation group (for receiving messages)
  const joinConversation = useCallback(async (conversationId: string) => {
    const connection = connectionRef.current;
    if (connection?.state === HubConnectionState.Connected) {
      await connection.invoke('JoinConversation', conversationId);
    }
  }, []);

  return {
    isConnected,
    connectionError,
    sendMessage,
    startTyping,
    stopTyping,
    markAsRead,
    joinConversation,
  };
}
