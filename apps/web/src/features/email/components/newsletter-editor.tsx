'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import {
  Save,
  Send,
  Clock,
  TestTube,
  Monitor,
  Smartphone,
  Loader2,
  ChevronLeft,
  X,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Separator } from '@/components/ui/separator';
import { useToast } from '@/hooks/use-toast';
import { EmailBlockEditor } from './email-block-editor';
import {
  useNewsletter,
  useCreateNewsletter,
  useUpdateNewsletter,
  useSendNewsletter,
  useScheduleNewsletter,
  useSendTestNewsletter,
  useNewsletterPreview,
  useEmailTemplates,
} from '../api';
import type { EmailBlock, DistributionType } from '../types';

interface NewsletterEditorProps {
  id: string;
}

export function NewsletterEditor({ id }: NewsletterEditorProps) {
  const router = useRouter();
  const { toast } = useToast();
  const isNew = id === 'new';

  const { data: newsletter, isLoading } = useNewsletter(id);
  const { data: templates } = useEmailTemplates();
  const createMutation = useCreateNewsletter();
  const updateMutation = useUpdateNewsletter();
  const sendMutation = useSendNewsletter();
  const scheduleMutation = useScheduleNewsletter();
  const testMutation = useSendTestNewsletter();

  // Form state
  const [title, setTitle] = useState('');
  const [subject, setSubject] = useState('');
  const [previewText, setPreviewText] = useState('');
  const [blocks, setBlocks] = useState<EmailBlock[]>([]);
  const [distributionType, setDistributionType] = useState<DistributionType>('AllUsers');
  const [templateId, setTemplateId] = useState<string | null>(null);

  // UI state
  const [previewOpen, setPreviewOpen] = useState(false);
  const [previewMode, setPreviewMode] = useState<'desktop' | 'mobile'>('desktop');
  const [testDialogOpen, setTestDialogOpen] = useState(false);
  const [testEmail, setTestEmail] = useState('');
  const [scheduleDialogOpen, setScheduleDialogOpen] = useState(false);
  const [scheduleDate, setScheduleDate] = useState('');
  const [scheduleTime, setScheduleTime] = useState('');
  const [sendDialogOpen, setSendDialogOpen] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);
  const [savedId, setSavedId] = useState<string | null>(null);

  const currentId = savedId || (isNew ? null : id);
  const { data: previewHtml, refetch: refetchPreview } = useNewsletterPreview(currentId || '');

  // Load existing newsletter data
  useEffect(() => {
    if (newsletter) {
      setTitle(newsletter.title);
      setSubject(newsletter.subject);
      setPreviewText(newsletter.previewText || '');
      setDistributionType(newsletter.distributionType);
      setTemplateId(newsletter.templateId || null);
      try {
        setBlocks(JSON.parse(newsletter.content || '[]'));
      } catch {
        setBlocks([]);
      }
    }
  }, [newsletter]);

  const handleSave = useCallback(async () => {
    if (!title.trim() || !subject.trim()) {
      toast({
        title: 'Validation Error',
        description: 'Title and subject are required',
        variant: 'destructive',
      });
      return;
    }

    const content = JSON.stringify(blocks);

    try {
      if (isNew || !currentId) {
        const result = await createMutation.mutateAsync({
          title,
          subject,
          previewText: previewText || undefined,
          content,
          distributionType,
          templateId: templateId || undefined,
        });
        setSavedId(result.id);
        setHasChanges(false);
        toast({ title: 'Newsletter created' });
        router.replace(`/studio/email/${result.id}`);
      } else {
        await updateMutation.mutateAsync({
          id: currentId,
          title,
          subject,
          previewText: previewText || undefined,
          content,
          distributionType,
          templateId: templateId || undefined,
        });
        setHasChanges(false);
        toast({ title: 'Newsletter saved' });
      }
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to save newsletter',
        variant: 'destructive',
      });
    }
  }, [
    title,
    subject,
    previewText,
    blocks,
    distributionType,
    templateId,
    isNew,
    currentId,
    createMutation,
    updateMutation,
    router,
    toast,
  ]);

  const handleSendTest = async () => {
    if (!testEmail.trim() || !currentId) return;

    try {
      await testMutation.mutateAsync({ id: currentId, email: testEmail });
      toast({ title: 'Test email sent', description: `Sent to ${testEmail}` });
      setTestDialogOpen(false);
      setTestEmail('');
    } catch {
      toast({
        title: 'Error',
        description: 'Failed to send test email',
        variant: 'destructive',
      });
    }
  };

  const handleSchedule = async () => {
    if (!scheduleDate || !scheduleTime || !currentId) return;

    const scheduledAt = new Date(`${scheduleDate}T${scheduleTime}`).toISOString();

    try {
      await scheduleMutation.mutateAsync({ id: currentId, scheduledAt });
      toast({ title: 'Newsletter scheduled' });
      setScheduleDialogOpen(false);
      router.push('/studio/email');
    } catch {
      toast({
        title: 'Error',
        description: 'Failed to schedule newsletter',
        variant: 'destructive',
      });
    }
  };

  const handleSend = async () => {
    if (!currentId) return;

    try {
      await sendMutation.mutateAsync(currentId);
      toast({ title: 'Newsletter queued for sending' });
      setSendDialogOpen(false);
      router.push('/studio/email');
    } catch {
      toast({
        title: 'Error',
        description: 'Failed to send newsletter',
        variant: 'destructive',
      });
    }
  };

  const handlePreviewOpen = () => {
    if (currentId) {
      refetchPreview();
    }
    setPreviewOpen(true);
  };

  // Track changes
  useEffect(() => {
    if (newsletter) {
      const currentContent = JSON.stringify(blocks);
      const hasContentChanges =
        title !== newsletter.title ||
        subject !== newsletter.subject ||
        previewText !== (newsletter.previewText || '') ||
        currentContent !== newsletter.content ||
        distributionType !== newsletter.distributionType;
      setHasChanges(hasContentChanges);
    } else if (isNew) {
      setHasChanges(title.trim() !== '' || subject.trim() !== '' || blocks.length > 0);
    }
  }, [title, subject, previewText, blocks, distributionType, newsletter, isNew]);

  if (isLoading && !isNew) {
    return (
      <div className="flex items-center justify-center h-96">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const isSaving = createMutation.isPending || updateMutation.isPending;
  const canSend = currentId && !isSaving && newsletter?.status === 'Draft';

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="border-b bg-background px-6 py-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Button variant="ghost" size="icon" onClick={() => router.push('/studio/email')}>
              <ChevronLeft className="h-5 w-5" />
            </Button>
            <div>
              <h1 className="text-lg font-semibold">
                {isNew ? 'New Newsletter' : 'Edit Newsletter'}
              </h1>
              {hasChanges && <p className="text-xs text-muted-foreground">Unsaved changes</p>}
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" onClick={handlePreviewOpen} disabled={blocks.length === 0}>
              Preview
            </Button>
            <Button variant="outline" onClick={() => setTestDialogOpen(true)} disabled={!currentId}>
              <TestTube className="h-4 w-4 mr-2" />
              Test
            </Button>
            <Button onClick={handleSave} disabled={isSaving}>
              {isSaving ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <Save className="h-4 w-4 mr-2" />
                  Save
                </>
              )}
            </Button>
            <Button
              variant="outline"
              onClick={() => setScheduleDialogOpen(true)}
              disabled={!canSend}
            >
              <Clock className="h-4 w-4 mr-2" />
              Schedule
            </Button>
            <Button onClick={() => setSendDialogOpen(true)} disabled={!canSend}>
              <Send className="h-4 w-4 mr-2" />
              Send Now
            </Button>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="flex-1 flex overflow-hidden">
        {/* Editor */}
        <div className="flex-1 overflow-auto p-6">
          <div className="max-w-2xl mx-auto">
            <EmailBlockEditor blocks={blocks} onChange={setBlocks} />
          </div>
        </div>

        {/* Settings Sidebar */}
        <div className="w-80 border-l bg-muted/30 overflow-auto p-6 space-y-6">
          <div className="space-y-4">
            <h2 className="font-semibold">Email Settings</h2>

            <div className="space-y-2">
              <Label htmlFor="title">Title (Internal)</Label>
              <Input
                id="title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="Newsletter title"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="subject">Subject Line</Label>
              <Input
                id="subject"
                value={subject}
                onChange={(e) => setSubject(e.target.value)}
                placeholder="Email subject"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="previewText">Preview Text</Label>
              <Textarea
                id="previewText"
                value={previewText}
                onChange={(e) => setPreviewText(e.target.value)}
                placeholder="Brief text shown in inbox preview"
                rows={2}
              />
            </div>
          </div>

          <Separator />

          <div className="space-y-4">
            <h2 className="font-semibold">Distribution</h2>

            <div className="space-y-2">
              <Label>Recipients</Label>
              <Select
                value={distributionType}
                onValueChange={(value) => setDistributionType(value as DistributionType)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="AllUsers">All Users</SelectItem>
                  <SelectItem value="UserGroups">User Groups</SelectItem>
                  <SelectItem value="CustomList">Custom List</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <Separator />

          <div className="space-y-4">
            <h2 className="font-semibold">Template</h2>

            <div className="space-y-2">
              <Label>Base Template</Label>
              <Select
                value={templateId || 'none'}
                onValueChange={(value) => setTemplateId(value === 'none' ? null : value)}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select a template" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">No Template</SelectItem>
                  {templates?.map((template) => (
                    <SelectItem key={template.id} value={template.id}>
                      {template.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </div>
      </div>

      {/* Preview Sheet */}
      <Sheet open={previewOpen} onOpenChange={setPreviewOpen}>
        <SheetContent side="right" className="w-full sm:max-w-xl md:max-w-2xl lg:max-w-4xl">
          <SheetHeader>
            <div className="flex items-center justify-between">
              <SheetTitle>Preview</SheetTitle>
              <div className="flex items-center gap-2">
                <Tabs value={previewMode} onValueChange={(v) => setPreviewMode(v as 'desktop' | 'mobile')}>
                  <TabsList>
                    <TabsTrigger value="desktop" className="gap-1">
                      <Monitor className="h-4 w-4" />
                      Desktop
                    </TabsTrigger>
                    <TabsTrigger value="mobile" className="gap-1">
                      <Smartphone className="h-4 w-4" />
                      Mobile
                    </TabsTrigger>
                  </TabsList>
                </Tabs>
              </div>
            </div>
            <SheetDescription>
              Preview how your newsletter will appear to recipients
            </SheetDescription>
          </SheetHeader>
          <div className="mt-6 flex justify-center">
            <div
              className={`border rounded-lg overflow-hidden transition-all ${
                previewMode === 'mobile' ? 'w-[375px]' : 'w-full'
              }`}
            >
              {previewHtml ? (
                <iframe
                  srcDoc={previewHtml}
                  className="w-full h-[70vh] bg-white"
                  title="Email Preview"
                />
              ) : (
                <div className="h-[70vh] flex items-center justify-center bg-muted">
                  <p className="text-muted-foreground">
                    {currentId ? 'Loading preview...' : 'Save newsletter to preview'}
                  </p>
                </div>
              )}
            </div>
          </div>
        </SheetContent>
      </Sheet>

      {/* Test Email Dialog */}
      <Dialog open={testDialogOpen} onOpenChange={setTestDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Send Test Email</DialogTitle>
            <DialogDescription>
              Send a test version of this newsletter to verify how it looks.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="testEmail">Email Address</Label>
              <Input
                id="testEmail"
                type="email"
                value={testEmail}
                onChange={(e) => setTestEmail(e.target.value)}
                placeholder="your@email.com"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setTestDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleSendTest} disabled={testMutation.isPending || !testEmail.trim()}>
              {testMutation.isPending ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Sending...
                </>
              ) : (
                <>
                  <TestTube className="h-4 w-4 mr-2" />
                  Send Test
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Schedule Dialog */}
      <Dialog open={scheduleDialogOpen} onOpenChange={setScheduleDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Schedule Newsletter</DialogTitle>
            <DialogDescription>
              Choose when you want this newsletter to be sent.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="scheduleDate">Date</Label>
                <Input
                  id="scheduleDate"
                  type="date"
                  value={scheduleDate}
                  onChange={(e) => setScheduleDate(e.target.value)}
                  min={new Date().toISOString().split('T')[0]}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="scheduleTime">Time</Label>
                <Input
                  id="scheduleTime"
                  type="time"
                  value={scheduleTime}
                  onChange={(e) => setScheduleTime(e.target.value)}
                />
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setScheduleDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleSchedule}
              disabled={scheduleMutation.isPending || !scheduleDate || !scheduleTime}
            >
              {scheduleMutation.isPending ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Scheduling...
                </>
              ) : (
                <>
                  <Clock className="h-4 w-4 mr-2" />
                  Schedule
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Send Confirmation Dialog */}
      <Dialog open={sendDialogOpen} onOpenChange={setSendDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Send Newsletter Now?</DialogTitle>
            <DialogDescription>
              This will immediately queue the newsletter for delivery to all recipients. This
              action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setSendDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleSend} disabled={sendMutation.isPending}>
              {sendMutation.isPending ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Sending...
                </>
              ) : (
                <>
                  <Send className="h-4 w-4 mr-2" />
                  Send Now
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
