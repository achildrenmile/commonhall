'use client';

import { useState, useCallback, useRef, useEffect } from 'react';
import { Bot, Send, Loader2, ExternalLink, RotateCcw, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { askAI } from '../api';
import type { ConversationMessage, SourceReference } from '../types';
import ReactMarkdown from 'react-markdown';

interface AiSearchProps {
  initialQuestion?: string;
  onClose?: () => void;
  className?: string;
}

export function AiSearch({ initialQuestion, onClose, className }: AiSearchProps) {
  const [question, setQuestion] = useState(initialQuestion || '');
  const [conversation, setConversation] = useState<ConversationMessage[]>([]);
  const [currentResponse, setCurrentResponse] = useState('');
  const [sources, setSources] = useState<SourceReference[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const scrollRef = useRef<HTMLDivElement>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  // Auto-scroll to bottom when new content arrives
  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [currentResponse, conversation]);

  // Handle initial question
  useEffect(() => {
    if (initialQuestion && conversation.length === 0) {
      handleAsk(initialQuestion);
    }
  }, [initialQuestion]);

  const handleAsk = useCallback(async (q?: string) => {
    const questionToAsk = q || question.trim();
    if (!questionToAsk || isLoading) return;

    setIsLoading(true);
    setError(null);
    setCurrentResponse('');
    setSources([]);

    // Add user message to conversation
    const userMessage: ConversationMessage = { role: 'user', content: questionToAsk };
    setConversation(prev => [...prev, userMessage]);
    setQuestion('');

    // Create abort controller for this request
    abortControllerRef.current = new AbortController();

    try {
      const result = await askAI(
        {
          question: questionToAsk,
          conversationHistory: conversation,
        },
        abortControllerRef.current.signal
      );

      result.onChunk((chunk) => {
        setCurrentResponse(prev => prev + chunk);
      });

      result.onDone(() => {
        setConversation(prev => [
          ...prev,
          { role: 'assistant', content: currentResponse }
        ]);
        setCurrentResponse('');
        setIsLoading(false);
      });

      result.onError((err) => {
        setError(err.message);
        setIsLoading(false);
      });

      // Sources are populated synchronously after the fetch
      setTimeout(() => {
        if (result.sources.length > 0) {
          setSources(result.sources);
        }
      }, 100);
    } catch (err) {
      if ((err as Error).name !== 'AbortError') {
        setError((err as Error).message);
      }
      setIsLoading(false);
    }
  }, [question, conversation, isLoading, currentResponse]);

  const handleStop = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
    }
    setIsLoading(false);
  }, []);

  const handleReset = useCallback(() => {
    setConversation([]);
    setCurrentResponse('');
    setSources([]);
    setError(null);
    setQuestion('');
  }, []);

  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleAsk();
    }
  }, [handleAsk]);

  return (
    <div className={cn('flex flex-col h-full', className)}>
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b">
        <div className="flex items-center gap-2">
          <Bot className="h-5 w-5 text-purple-500" />
          <h2 className="font-semibold">Ask AI</h2>
        </div>
        <div className="flex items-center gap-2">
          {conversation.length > 0 && (
            <Button variant="ghost" size="sm" onClick={handleReset}>
              <RotateCcw className="h-4 w-4" />
            </Button>
          )}
          {onClose && (
            <Button variant="ghost" size="sm" onClick={onClose}>
              <X className="h-4 w-4" />
            </Button>
          )}
        </div>
      </div>

      {/* Conversation */}
      <ScrollArea className="flex-1 p-4" ref={scrollRef}>
        {conversation.length === 0 && !currentResponse && !isLoading && (
          <div className="flex flex-col items-center justify-center h-full text-center text-muted-foreground">
            <Bot className="h-12 w-12 mb-4 text-purple-500/50" />
            <h3 className="font-medium mb-2">Ask anything about your content</h3>
            <p className="text-sm max-w-sm">
              I'll search through your news articles, pages, and documents to find relevant information.
            </p>
          </div>
        )}

        <div className="space-y-4">
          {conversation.map((msg, i) => (
            <div
              key={i}
              className={cn(
                'flex',
                msg.role === 'user' ? 'justify-end' : 'justify-start'
              )}
            >
              <div
                className={cn(
                  'max-w-[80%] rounded-lg px-4 py-2',
                  msg.role === 'user'
                    ? 'bg-primary text-primary-foreground'
                    : 'bg-muted'
                )}
              >
                {msg.role === 'assistant' ? (
                  <div className="prose prose-sm dark:prose-invert max-w-none">
                    <ReactMarkdown>{msg.content}</ReactMarkdown>
                  </div>
                ) : (
                  <p>{msg.content}</p>
                )}
              </div>
            </div>
          ))}

          {/* Streaming response */}
          {currentResponse && (
            <div className="flex justify-start">
              <div className="max-w-[80%] rounded-lg px-4 py-2 bg-muted">
                <div className="prose prose-sm dark:prose-invert max-w-none">
                  <ReactMarkdown>{currentResponse}</ReactMarkdown>
                </div>
              </div>
            </div>
          )}

          {/* Loading indicator */}
          {isLoading && !currentResponse && (
            <div className="flex justify-start">
              <div className="max-w-[80%] rounded-lg px-4 py-2 bg-muted">
                <div className="flex items-center gap-2">
                  <Loader2 className="h-4 w-4 animate-spin" />
                  <span className="text-sm text-muted-foreground">Searching and thinking...</span>
                </div>
              </div>
            </div>
          )}

          {/* Error */}
          {error && (
            <div className="flex justify-center">
              <div className="rounded-lg px-4 py-2 bg-destructive/10 text-destructive text-sm">
                {error}
              </div>
            </div>
          )}
        </div>

        {/* Sources */}
        {sources.length > 0 && (
          <div className="mt-6 pt-4 border-t">
            <h4 className="text-sm font-medium mb-3">Sources</h4>
            <div className="space-y-2">
              {sources.map((source, i) => (
                <a
                  key={i}
                  href={source.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-start gap-3 p-2 rounded hover:bg-muted transition-colors"
                >
                  <Badge variant="outline" className="shrink-0 mt-0.5">
                    {source.type}
                  </Badge>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium truncate">{source.title}</p>
                    {source.excerpt && (
                      <p className="text-xs text-muted-foreground line-clamp-2 mt-0.5">
                        {source.excerpt}
                      </p>
                    )}
                  </div>
                  <ExternalLink className="h-4 w-4 text-muted-foreground shrink-0" />
                </a>
              ))}
            </div>
          </div>
        )}
      </ScrollArea>

      {/* Input */}
      <div className="p-4 border-t">
        <div className="flex gap-2">
          <Input
            placeholder="Ask a question..."
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
            onKeyDown={handleKeyDown}
            disabled={isLoading}
          />
          {isLoading ? (
            <Button variant="outline" onClick={handleStop}>
              Stop
            </Button>
          ) : (
            <Button onClick={() => handleAsk()} disabled={!question.trim()}>
              <Send className="h-4 w-4" />
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
