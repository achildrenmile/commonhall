'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import {
  Save,
  ChevronLeft,
  Plus,
  GripVertical,
  Trash2,
  ChevronDown,
  ChevronUp,
  Loader2,
  Type,
  AlignLeft,
  Mail,
  Phone,
  Hash,
  Calendar,
  ChevronDown as Select,
  ListChecks,
  CheckSquare,
  Paperclip,
  Eye,
  EyeOff,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import {
  Select as SelectComponent,
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
import { Separator } from '@/components/ui/separator';
import { useToast } from '@/hooks/use-toast';
import { useForm, useCreateForm, useUpdateForm } from '../api';
import type { FormFieldType, FormField } from '../types';

interface FormBuilderProps {
  id: string;
}

const fieldTypeConfig: Record<FormFieldType, { label: string; icon: React.ComponentType<{ className?: string }> }> = {
  text: { label: 'Text', icon: Type },
  textarea: { label: 'Long Text', icon: AlignLeft },
  email: { label: 'Email', icon: Mail },
  phone: { label: 'Phone', icon: Phone },
  number: { label: 'Number', icon: Hash },
  date: { label: 'Date', icon: Calendar },
  dropdown: { label: 'Dropdown', icon: Select },
  radio: { label: 'Radio', icon: ListChecks },
  checkbox: { label: 'Checkboxes', icon: CheckSquare },
  file: { label: 'File Upload', icon: Paperclip },
};

function generateId(): string {
  return `field_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;
}

interface FieldEditorProps {
  field: FormField;
  index: number;
  totalFields: number;
  isExpanded: boolean;
  onToggle: () => void;
  onUpdate: (updates: Partial<FormField>) => void;
  onDelete: () => void;
  onMoveUp: () => void;
  onMoveDown: () => void;
}

function FieldEditor({
  field,
  index,
  totalFields,
  isExpanded,
  onToggle,
  onUpdate,
  onDelete,
  onMoveUp,
  onMoveDown,
}: FieldEditorProps) {
  const [options, setOptions] = useState<string[]>(field.options || []);
  const TypeIcon = fieldTypeConfig[field.type]?.icon || Type;

  useEffect(() => {
    if (['dropdown', 'radio', 'checkbox'].includes(field.type)) {
      onUpdate({ options: options.filter(Boolean) });
    }
  }, [options, field.type]);

  const handleAddOption = () => {
    setOptions([...options, '']);
  };

  const handleUpdateOption = (idx: number, value: string) => {
    const newOptions = [...options];
    newOptions[idx] = value;
    setOptions(newOptions);
  };

  const handleRemoveOption = (idx: number) => {
    setOptions(options.filter((_, i) => i !== idx));
  };

  return (
    <Collapsible open={isExpanded} onOpenChange={onToggle}>
      <div className="border rounded-lg bg-white dark:bg-slate-950">
        <div className="flex items-center gap-2 p-3 group">
          <button className="cursor-grab text-slate-400 hover:text-slate-600">
            <GripVertical className="h-4 w-4" />
          </button>
          <div className="flex-1">
            <CollapsibleTrigger className="flex items-center gap-2 w-full text-left">
              <TypeIcon className="h-4 w-4 text-muted-foreground" />
              <span className="text-slate-600 dark:text-slate-400 flex-1 truncate">
                {field.label || field.name || 'Untitled Field'}
              </span>
              {field.required && (
                <span className="text-xs text-red-500">Required</span>
              )}
              {isExpanded ? (
                <ChevronUp className="h-4 w-4 text-slate-400" />
              ) : (
                <ChevronDown className="h-4 w-4 text-slate-400" />
              )}
            </CollapsibleTrigger>
          </div>
          <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7"
              onClick={onMoveUp}
              disabled={index === 0}
            >
              <ChevronUp className="h-4 w-4" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7"
              onClick={onMoveDown}
              disabled={index === totalFields - 1}
            >
              <ChevronDown className="h-4 w-4" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7 text-red-500 hover:text-red-600"
              onClick={onDelete}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        </div>

        <CollapsibleContent>
          <Separator />
          <div className="p-4 space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Field Type</Label>
                <SelectComponent
                  value={field.type}
                  onValueChange={(value) => onUpdate({ type: value as FormFieldType })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {Object.entries(fieldTypeConfig).map(([type, config]) => (
                      <SelectItem key={type} value={type}>
                        <div className="flex items-center gap-2">
                          <config.icon className="h-4 w-4" />
                          {config.label}
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </SelectComponent>
              </div>
              <div className="space-y-2">
                <Label>Required?</Label>
                <div className="flex items-center gap-2 pt-2">
                  <Switch
                    checked={field.required}
                    onCheckedChange={(checked) => onUpdate({ required: checked })}
                  />
                  <span className="text-sm text-slate-600">
                    {field.required ? 'Required' : 'Optional'}
                  </span>
                </div>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Label</Label>
                <Input
                  value={field.label || ''}
                  onChange={(e) => onUpdate({ label: e.target.value })}
                  placeholder="Full Name"
                />
              </div>
              <div className="space-y-2">
                <Label>Field Name (ID)</Label>
                <Input
                  value={field.name}
                  onChange={(e) => onUpdate({ name: e.target.value.replace(/\s+/g, '_').toLowerCase() })}
                  placeholder="full_name"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label>Placeholder</Label>
              <Input
                value={field.placeholder || ''}
                onChange={(e) => onUpdate({ placeholder: e.target.value })}
                placeholder="Enter placeholder text..."
              />
            </div>

            {['dropdown', 'radio', 'checkbox'].includes(field.type) && (
              <div className="space-y-2">
                <Label>Options</Label>
                <div className="space-y-2">
                  {options.map((option, idx) => (
                    <div key={idx} className="flex items-center gap-2">
                      <Input
                        value={option}
                        onChange={(e) => handleUpdateOption(idx, e.target.value)}
                        placeholder={`Option ${idx + 1}`}
                      />
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-red-500"
                        onClick={() => handleRemoveOption(idx)}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  ))}
                  <Button variant="outline" size="sm" onClick={handleAddOption}>
                    <Plus className="h-4 w-4 mr-2" />
                    Add Option
                  </Button>
                </div>
              </div>
            )}
          </div>
        </CollapsibleContent>
      </div>
    </Collapsible>
  );
}

export function FormBuilder({ id }: FormBuilderProps) {
  const router = useRouter();
  const { toast } = useToast();
  const isNew = id === 'new';

  const { data: form, isLoading } = useForm(id);
  const createMutation = useCreateForm();
  const updateMutation = useUpdateForm();

  // Form state
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [notificationEmail, setNotificationEmail] = useState('');
  const [confirmationMessage, setConfirmationMessage] = useState('');
  const [isActive, setIsActive] = useState(false);
  const [fields, setFields] = useState<FormField[]>([]);
  const [expandedField, setExpandedField] = useState<number | null>(null);
  const [savedId, setSavedId] = useState<string | null>(null);
  const [hasChanges, setHasChanges] = useState(false);

  // Load existing form data
  useEffect(() => {
    if (form) {
      setTitle(form.title);
      setDescription(form.description || '');
      setNotificationEmail(form.notificationEmail || '');
      setConfirmationMessage(form.confirmationMessage || '');
      setIsActive(form.isActive);
      try {
        setFields(JSON.parse(form.fields));
      } catch {
        setFields([]);
      }
    }
  }, [form]);

  const currentId = savedId || (isNew ? null : id);

  const handleAddField = (type: FormFieldType = 'text') => {
    const newField: FormField = {
      name: generateId(),
      label: '',
      type,
      required: false,
      placeholder: '',
      options: ['dropdown', 'radio', 'checkbox'].includes(type) ? [] : undefined,
    };
    setFields([...fields, newField]);
    setExpandedField(fields.length);
    setHasChanges(true);
  };

  const handleUpdateField = (index: number, updates: Partial<FormField>) => {
    const newFields = [...fields];
    newFields[index] = { ...newFields[index], ...updates };
    setFields(newFields);
    setHasChanges(true);
  };

  const handleDeleteField = (index: number) => {
    setFields(fields.filter((_, i) => i !== index));
    if (expandedField === index) {
      setExpandedField(null);
    } else if (expandedField !== null && expandedField > index) {
      setExpandedField(expandedField - 1);
    }
    setHasChanges(true);
  };

  const handleMoveField = (fromIndex: number, toIndex: number) => {
    if (toIndex < 0 || toIndex >= fields.length) return;
    const newFields = [...fields];
    const [removed] = newFields.splice(fromIndex, 1);
    newFields.splice(toIndex, 0, removed);
    setFields(newFields);
    setExpandedField(toIndex);
    setHasChanges(true);
  };

  const handleSave = async () => {
    if (!title.trim()) {
      toast({
        title: 'Validation Error',
        description: 'Form title is required',
        variant: 'destructive',
      });
      return;
    }

    const fieldsJson = JSON.stringify(fields);

    try {
      if (isNew || !currentId) {
        const result = await createMutation.mutateAsync({
          title,
          description: description || undefined,
          fields: fieldsJson,
          notificationEmail: notificationEmail || undefined,
          confirmationMessage: confirmationMessage || undefined,
        });
        setSavedId(result.id);
        toast({ title: 'Form created' });
        router.replace(`/studio/forms/${result.id}`);
      } else {
        await updateMutation.mutateAsync({
          id: currentId,
          title,
          description: description || undefined,
          fields: fieldsJson,
          notificationEmail: notificationEmail || undefined,
          confirmationMessage: confirmationMessage || undefined,
          isActive,
        });
        toast({ title: 'Form saved' });
      }

      setHasChanges(false);
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to save form',
        variant: 'destructive',
      });
    }
  };

  const handleToggleActive = async () => {
    if (!currentId) return;

    try {
      await updateMutation.mutateAsync({
        id: currentId,
        isActive: !isActive,
      });
      setIsActive(!isActive);
      toast({ title: isActive ? 'Form deactivated' : 'Form activated' });
    } catch {
      toast({
        title: 'Error',
        description: 'Failed to update form status',
        variant: 'destructive',
      });
    }
  };

  if (isLoading && !isNew) {
    return (
      <div className="flex items-center justify-center h-96">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const isSaving = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="border-b bg-background px-6 py-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Button variant="ghost" size="icon" onClick={() => router.push('/studio/forms')}>
              <ChevronLeft className="h-5 w-5" />
            </Button>
            <div>
              <h1 className="text-lg font-semibold">
                {isNew ? 'New Form' : 'Edit Form'}
              </h1>
              {hasChanges && <p className="text-xs text-muted-foreground">Unsaved changes</p>}
            </div>
            {form && (
              <Badge variant={isActive ? 'default' : 'secondary'}>
                {isActive ? 'Active' : 'Inactive'}
              </Badge>
            )}
          </div>
          <div className="flex items-center gap-2">
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
            {currentId && (
              <Button
                variant={isActive ? 'outline' : 'default'}
                onClick={handleToggleActive}
                disabled={updateMutation.isPending}
              >
                {isActive ? (
                  <>
                    <EyeOff className="h-4 w-4 mr-2" />
                    Deactivate
                  </>
                ) : (
                  <>
                    <Eye className="h-4 w-4 mr-2" />
                    Activate
                  </>
                )}
              </Button>
            )}
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="flex-1 flex overflow-hidden">
        {/* Fields Editor */}
        <div className="flex-1 overflow-auto p-6">
          <div className="max-w-3xl mx-auto space-y-4">
            <h2 className="font-semibold mb-4">Form Fields</h2>

            {fields.length === 0 ? (
              <div className="text-center py-12 border-2 border-dashed border-slate-200 dark:border-slate-800 rounded-lg">
                <p className="text-slate-500 mb-4">No fields yet. Add your first field to get started.</p>
                <div className="flex flex-wrap gap-2 justify-center">
                  {Object.entries(fieldTypeConfig).slice(0, 6).map(([type, config]) => (
                    <Button
                      key={type}
                      variant="outline"
                      size="sm"
                      onClick={() => handleAddField(type as FormFieldType)}
                    >
                      <config.icon className="h-4 w-4 mr-2" />
                      {config.label}
                    </Button>
                  ))}
                </div>
              </div>
            ) : (
              <div className="space-y-3">
                {fields.map((field, index) => (
                  <div key={field.name}>
                    <FieldEditor
                      field={field}
                      index={index}
                      totalFields={fields.length}
                      isExpanded={expandedField === index}
                      onToggle={() => setExpandedField(expandedField === index ? null : index)}
                      onUpdate={(updates) => handleUpdateField(index, updates)}
                      onDelete={() => handleDeleteField(index)}
                      onMoveUp={() => handleMoveField(index, index - 1)}
                      onMoveDown={() => handleMoveField(index, index + 1)}
                    />
                  </div>
                ))}
                <div className="flex flex-wrap gap-2">
                  {Object.entries(fieldTypeConfig).map(([type, config]) => (
                    <Button
                      key={type}
                      variant="outline"
                      size="sm"
                      onClick={() => handleAddField(type as FormFieldType)}
                    >
                      <config.icon className="h-4 w-4 mr-2" />
                      {config.label}
                    </Button>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Settings Sidebar */}
        <div className="w-80 border-l bg-muted/30 overflow-auto p-6 space-y-6">
          <div className="space-y-4">
            <h2 className="font-semibold">Form Settings</h2>

            <div className="space-y-2">
              <Label htmlFor="title">Title</Label>
              <Input
                id="title"
                value={title}
                onChange={(e) => {
                  setTitle(e.target.value);
                  setHasChanges(true);
                }}
                placeholder="Contact Us"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={description}
                onChange={(e) => {
                  setDescription(e.target.value);
                  setHasChanges(true);
                }}
                placeholder="Get in touch with our team..."
                rows={3}
              />
            </div>
          </div>

          <Separator />

          <div className="space-y-4">
            <h2 className="font-semibold">Notifications</h2>

            <div className="space-y-2">
              <Label htmlFor="notificationEmail">Notification Email</Label>
              <Input
                id="notificationEmail"
                type="email"
                value={notificationEmail}
                onChange={(e) => {
                  setNotificationEmail(e.target.value);
                  setHasChanges(true);
                }}
                placeholder="team@company.com"
              />
              <p className="text-xs text-muted-foreground">
                Receive email when form is submitted
              </p>
            </div>
          </div>

          <Separator />

          <div className="space-y-4">
            <h2 className="font-semibold">Confirmation</h2>

            <div className="space-y-2">
              <Label htmlFor="confirmationMessage">Confirmation Message</Label>
              <Textarea
                id="confirmationMessage"
                value={confirmationMessage}
                onChange={(e) => {
                  setConfirmationMessage(e.target.value);
                  setHasChanges(true);
                }}
                placeholder="Thank you for your submission!"
                rows={3}
              />
            </div>
          </div>

          {currentId && (
            <>
              <Separator />
              <div className="space-y-2">
                <h2 className="font-semibold">Stats</h2>
                <div className="text-sm">
                  <p className="text-muted-foreground">Submissions</p>
                  <p className="font-medium">{form?.submissionCount || 0}</p>
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
