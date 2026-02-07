'use client';

import { useState, useCallback } from 'react';
import {
  Sparkles,
  Wand2,
  Languages,
  FileText,
  Heading,
  MessageSquare,
  Loader2,
  Copy,
  Check,
  ChevronDown,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
import { cn } from '@/lib/utils';
import { useGenerateHeadlines, useGenerateTeaser, useSummarize, useTranslate, streamImproveText } from '../api';

interface AiCompanionPanelProps {
  content: string;
  selectedText?: string;
  onInsert: (text: string) => void;
  onReplace?: (text: string) => void;
  className?: string;
}

type Tool = 'headlines' | 'teaser' | 'improve' | 'summarize' | 'translate';

const languages = [
  { value: 'Spanish', label: 'Spanish' },
  { value: 'French', label: 'French' },
  { value: 'German', label: 'German' },
  { value: 'Italian', label: 'Italian' },
  { value: 'Portuguese', label: 'Portuguese' },
  { value: 'Dutch', label: 'Dutch' },
  { value: 'Polish', label: 'Polish' },
  { value: 'Japanese', label: 'Japanese' },
  { value: 'Chinese', label: 'Chinese' },
  { value: 'Korean', label: 'Korean' },
];

const tones = [
  { value: 'professional', label: 'Professional' },
  { value: 'friendly', label: 'Friendly' },
  { value: 'casual', label: 'Casual' },
  { value: 'urgent', label: 'Urgent' },
  { value: 'inspirational', label: 'Inspirational' },
];

export function AiCompanionPanel({
  content,
  selectedText,
  onInsert,
  onReplace,
  className,
}: AiCompanionPanelProps) {
  const [activeTool, setActiveTool] = useState<Tool | null>(null);
  const [result, setResult] = useState<string | string[]>('');
  const [copied, setCopied] = useState(false);
  const [isStreaming, setIsStreaming] = useState(false);

  // Tool-specific state
  const [tone, setTone] = useState('professional');
  const [instruction, setInstruction] = useState('');
  const [targetLanguage, setTargetLanguage] = useState('Spanish');
  const [summaryLength, setSummaryLength] = useState<'short' | 'medium' | 'long'>('medium');

  const generateHeadlines = useGenerateHeadlines();
  const generateTeaser = useGenerateTeaser();
  const summarize = useSummarize();
  const translate = useTranslate();

  const sourceText = selectedText || content;

  const handleGenerateHeadlines = async () => {
    setActiveTool('headlines');
    setResult([]);
    const data = await generateHeadlines.mutateAsync({
      articleBody: sourceText,
      tone,
    });
    setResult(data.headlines);
  };

  const handleGenerateTeaser = async () => {
    setActiveTool('teaser');
    setResult('');
    const data = await generateTeaser.mutateAsync({
      articleBody: sourceText,
      maxLength: 200,
    });
    setResult(data.teaser);
  };

  const handleImprove = async () => {
    if (!instruction.trim()) return;
    setActiveTool('improve');
    setResult('');
    setIsStreaming(true);

    try {
      let fullResult = '';
      for await (const chunk of streamImproveText({
        text: sourceText,
        instruction,
      })) {
        fullResult += chunk;
        setResult(fullResult);
      }
    } finally {
      setIsStreaming(false);
    }
  };

  const handleSummarize = async () => {
    setActiveTool('summarize');
    setResult('');
    const data = await summarize.mutateAsync({
      text: sourceText,
      length: summaryLength,
    });
    setResult(data.summary);
  };

  const handleTranslate = async () => {
    setActiveTool('translate');
    setResult('');
    const data = await translate.mutateAsync({
      text: sourceText,
      targetLanguage,
    });
    setResult(data.translation);
  };

  const handleCopy = useCallback((text: string) => {
    navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }, []);

  const handleInsertResult = useCallback((text: string) => {
    onInsert(text);
    setResult('');
    setActiveTool(null);
  }, [onInsert]);

  const handleReplaceResult = useCallback((text: string) => {
    if (onReplace && selectedText) {
      onReplace(text);
      setResult('');
      setActiveTool(null);
    }
  }, [onReplace, selectedText]);

  const isLoading = generateHeadlines.isPending || generateTeaser.isPending ||
    summarize.isPending || translate.isPending || isStreaming;

  const tools = [
    {
      id: 'headlines' as Tool,
      icon: Heading,
      label: 'Headlines',
      description: 'Generate headline variations',
      action: handleGenerateHeadlines,
    },
    {
      id: 'teaser' as Tool,
      icon: MessageSquare,
      label: 'Teaser',
      description: 'Create a compelling summary',
      action: handleGenerateTeaser,
    },
    {
      id: 'improve' as Tool,
      icon: Wand2,
      label: 'Improve',
      description: 'Enhance text with instructions',
      action: handleImprove,
    },
    {
      id: 'summarize' as Tool,
      icon: FileText,
      label: 'Summarize',
      description: 'Create a condensed version',
      action: handleSummarize,
    },
    {
      id: 'translate' as Tool,
      icon: Languages,
      label: 'Translate',
      description: 'Convert to another language',
      action: handleTranslate,
    },
  ];

  return (
    <div className={cn('bg-slate-50 dark:bg-slate-900 rounded-lg border p-4', className)}>
      <div className="flex items-center gap-2 mb-4">
        <Sparkles className="h-5 w-5 text-purple-500" />
        <h3 className="font-semibold">AI Companion</h3>
      </div>

      {selectedText && (
        <div className="mb-4 p-3 bg-white dark:bg-slate-800 rounded border text-sm">
          <p className="text-xs text-muted-foreground mb-1">Selected text:</p>
          <p className="line-clamp-3">{selectedText}</p>
        </div>
      )}

      <div className="space-y-3">
        {tools.map((tool) => (
          <Collapsible key={tool.id} open={activeTool === tool.id}>
            <CollapsibleTrigger asChild>
              <Button
                variant="ghost"
                className="w-full justify-between"
                onClick={() => setActiveTool(activeTool === tool.id ? null : tool.id)}
              >
                <div className="flex items-center gap-2">
                  <tool.icon className="h-4 w-4" />
                  <span>{tool.label}</span>
                </div>
                <ChevronDown className={cn(
                  'h-4 w-4 transition-transform',
                  activeTool === tool.id && 'rotate-180'
                )} />
              </Button>
            </CollapsibleTrigger>
            <CollapsibleContent className="pt-2 pb-4 px-2">
              <p className="text-sm text-muted-foreground mb-3">{tool.description}</p>

              {/* Tool-specific options */}
              {tool.id === 'headlines' && (
                <div className="mb-3">
                  <label className="text-sm font-medium">Tone</label>
                  <Select value={tone} onValueChange={setTone}>
                    <SelectTrigger className="mt-1">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {tones.map((t) => (
                        <SelectItem key={t.value} value={t.value}>
                          {t.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}

              {tool.id === 'improve' && (
                <div className="mb-3">
                  <label className="text-sm font-medium">Instruction</label>
                  <Textarea
                    className="mt-1"
                    placeholder="e.g., Make it more concise, Add bullet points, Fix grammar..."
                    value={instruction}
                    onChange={(e) => setInstruction(e.target.value)}
                    rows={2}
                  />
                </div>
              )}

              {tool.id === 'summarize' && (
                <div className="mb-3">
                  <label className="text-sm font-medium">Length</label>
                  <Select value={summaryLength} onValueChange={(v) => setSummaryLength(v as 'short' | 'medium' | 'long')}>
                    <SelectTrigger className="mt-1">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="short">Short (1-2 sentences)</SelectItem>
                      <SelectItem value="medium">Medium (3-4 sentences)</SelectItem>
                      <SelectItem value="long">Long (paragraph)</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              )}

              {tool.id === 'translate' && (
                <div className="mb-3">
                  <label className="text-sm font-medium">Target Language</label>
                  <Select value={targetLanguage} onValueChange={setTargetLanguage}>
                    <SelectTrigger className="mt-1">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {languages.map((lang) => (
                        <SelectItem key={lang.value} value={lang.value}>
                          {lang.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}

              <Button
                onClick={tool.action}
                disabled={isLoading || (tool.id === 'improve' && !instruction.trim())}
                className="w-full"
              >
                {isLoading && activeTool === tool.id ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Generating...
                  </>
                ) : (
                  <>
                    <Sparkles className="mr-2 h-4 w-4" />
                    Generate
                  </>
                )}
              </Button>
            </CollapsibleContent>
          </Collapsible>
        ))}
      </div>

      {/* Results */}
      {result && (typeof result === 'string' ? result.length > 0 : result.length > 0) && (
        <div className="mt-4 p-3 bg-white dark:bg-slate-800 rounded border">
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm font-medium">Result</span>
            <div className="flex gap-1">
              <Button
                size="sm"
                variant="ghost"
                onClick={() => handleCopy(Array.isArray(result) ? result.join('\n') : result)}
              >
                {copied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
              </Button>
            </div>
          </div>

          {Array.isArray(result) ? (
            <div className="space-y-2">
              {result.map((item, i) => (
                <div
                  key={i}
                  className="p-2 bg-slate-50 dark:bg-slate-900 rounded text-sm cursor-pointer hover:bg-slate-100 dark:hover:bg-slate-800"
                  onClick={() => handleInsertResult(item)}
                >
                  {item}
                </div>
              ))}
            </div>
          ) : (
            <div className="text-sm whitespace-pre-wrap">{result}</div>
          )}

          <div className="flex gap-2 mt-3">
            <Button
              size="sm"
              variant="outline"
              className="flex-1"
              onClick={() => handleInsertResult(Array.isArray(result) ? result[0] : result)}
            >
              Insert
            </Button>
            {selectedText && onReplace && (
              <Button
                size="sm"
                className="flex-1"
                onClick={() => handleReplaceResult(Array.isArray(result) ? result[0] : result)}
              >
                Replace
              </Button>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
