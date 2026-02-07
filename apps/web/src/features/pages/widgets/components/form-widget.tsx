'use client';

import { useState } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Loader2, CheckCircle2, AlertCircle, FileText } from 'lucide-react';
import { apiClient } from '@/lib/api-client';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import type { WidgetProps, FormWidgetData } from '../types';

interface FormField {
  name: string;
  label?: string;
  type: 'text' | 'textarea' | 'email' | 'phone' | 'number' | 'date' | 'dropdown' | 'radio' | 'checkbox' | 'file';
  required: boolean;
  placeholder?: string;
  options?: string[];
}

interface FormDetail {
  id: string;
  title: string;
  description?: string;
  fields: string; // JSON
  confirmationMessage?: string;
}

interface SubmitResult {
  message: string;
  submissionId: string;
  confirmationMessage?: string;
}

async function fetchForm(formId: string): Promise<FormDetail> {
  return apiClient.get<FormDetail>(`/forms/${formId}/public`);
}

async function submitForm(
  formId: string,
  data: Record<string, unknown>
): Promise<SubmitResult> {
  return apiClient.post<SubmitResult>(`/forms/${formId}/submit`, { data });
}

function parseFields(fieldsJson?: string): FormField[] {
  if (!fieldsJson) return [];
  try {
    return JSON.parse(fieldsJson);
  } catch {
    return [];
  }
}

export default function FormWidget({ data, id }: WidgetProps<FormWidgetData>) {
  const [formData, setFormData] = useState<Record<string, unknown>>({});
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitted, setSubmitted] = useState(false);
  const [confirmationMessage, setConfirmationMessage] = useState<string | null>(null);

  const { data: form, isLoading } = useQuery({
    queryKey: ['widget-form', data.formId],
    queryFn: () => fetchForm(data.formId),
    enabled: !!data.formId,
  });

  const submitMutation = useMutation({
    mutationFn: () => submitForm(data.formId, formData),
    onSuccess: (result) => {
      setSubmitted(true);
      setConfirmationMessage(result.confirmationMessage || 'Form submitted successfully!');
    },
    onError: (err: Error) => {
      setErrors({ _form: err.message || 'Failed to submit form' });
    },
  });

  const setValue = (name: string, value: unknown) => {
    setFormData((prev) => ({ ...prev, [name]: value }));
    setErrors((prev) => {
      const next = { ...prev };
      delete next[name];
      delete next._form;
      return next;
    });
  };

  const validateForm = (): boolean => {
    const fields = parseFields(form?.fields);
    const newErrors: Record<string, string> = {};

    for (const field of fields) {
      if (field.required) {
        const value = formData[field.name];
        if (
          value === undefined ||
          value === null ||
          value === '' ||
          (Array.isArray(value) && value.length === 0)
        ) {
          newErrors[field.name] = `${field.label || field.name} is required`;
        }
      }

      // Email validation
      if (field.type === 'email' && formData[field.name]) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(formData[field.name] as string)) {
          newErrors[field.name] = 'Please enter a valid email address';
        }
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      submitMutation.mutate();
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-6 w-6 animate-spin text-slate-400" />
      </div>
    );
  }

  if (!form) {
    return (
      <div className="rounded-lg border border-slate-200 dark:border-slate-800 p-6 text-center">
        <AlertCircle className="h-8 w-8 text-slate-300 dark:text-slate-600 mx-auto mb-2" />
        <p className="text-sm text-slate-500">Form not found or not available</p>
      </div>
    );
  }

  if (submitted) {
    return (
      <div className="rounded-lg border border-green-200 dark:border-green-800 bg-green-50 dark:bg-green-950 p-6 text-center">
        <CheckCircle2 className="h-8 w-8 text-green-500 mx-auto mb-2" />
        <p className="font-medium text-green-800 dark:text-green-200">Thank you!</p>
        <p className="text-sm text-green-600 dark:text-green-400 mt-1">
          {confirmationMessage}
        </p>
      </div>
    );
  }

  const fields = parseFields(form.fields);

  return (
    <div className="rounded-lg border border-slate-200 dark:border-slate-800 p-6">
      <h3 className="font-semibold text-lg mb-2">{form.title}</h3>
      {form.description && (
        <p className="text-sm text-slate-600 dark:text-slate-400 mb-6">{form.description}</p>
      )}

      <form onSubmit={handleSubmit} className="space-y-4">
        {fields.map((field) => (
          <FieldInput
            key={field.name}
            field={field}
            value={formData[field.name]}
            onChange={(value) => setValue(field.name, value)}
            error={errors[field.name]}
          />
        ))}

        {errors._form && (
          <p className="text-sm text-red-500">{errors._form}</p>
        )}

        <Button
          type="submit"
          className="w-full"
          disabled={submitMutation.isPending}
        >
          {submitMutation.isPending ? (
            <>
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              Submitting...
            </>
          ) : (
            'Submit'
          )}
        </Button>
      </form>
    </div>
  );
}

interface FieldInputProps {
  field: FormField;
  value: unknown;
  onChange: (value: unknown) => void;
  error?: string;
}

function FieldInput({ field, value, onChange, error }: FieldInputProps) {
  const labelElement = (
    <Label htmlFor={field.name}>
      {field.label || field.name}
      {field.required && <span className="text-red-500 ml-1">*</span>}
    </Label>
  );

  const errorElement = error && (
    <p className="text-sm text-red-500 mt-1">{error}</p>
  );

  switch (field.type) {
    case 'text':
    case 'email':
    case 'phone':
    case 'number':
    case 'date':
      return (
        <div className="space-y-2">
          {labelElement}
          <Input
            id={field.name}
            type={field.type === 'phone' ? 'tel' : field.type}
            value={(value as string) || ''}
            onChange={(e) => onChange(e.target.value)}
            placeholder={field.placeholder}
          />
          {errorElement}
        </div>
      );

    case 'textarea':
      return (
        <div className="space-y-2">
          {labelElement}
          <Textarea
            id={field.name}
            value={(value as string) || ''}
            onChange={(e) => onChange(e.target.value)}
            placeholder={field.placeholder}
            rows={4}
          />
          {errorElement}
        </div>
      );

    case 'dropdown':
      return (
        <div className="space-y-2">
          {labelElement}
          <Select
            value={(value as string) || ''}
            onValueChange={onChange}
          >
            <SelectTrigger id={field.name}>
              <SelectValue placeholder={field.placeholder || 'Select an option'} />
            </SelectTrigger>
            <SelectContent>
              {field.options?.map((option, idx) => (
                <SelectItem key={idx} value={option}>
                  {option}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errorElement}
        </div>
      );

    case 'radio':
      return (
        <div className="space-y-2">
          {labelElement}
          <RadioGroup
            value={(value as string) || ''}
            onValueChange={onChange}
          >
            {field.options?.map((option, idx) => (
              <div key={idx} className="flex items-center space-x-2">
                <RadioGroupItem value={option} id={`${field.name}-${idx}`} />
                <Label htmlFor={`${field.name}-${idx}`} className="font-normal">
                  {option}
                </Label>
              </div>
            ))}
          </RadioGroup>
          {errorElement}
        </div>
      );

    case 'checkbox':
      return (
        <div className="space-y-2">
          {labelElement}
          <div className="space-y-2">
            {field.options?.map((option, idx) => {
              const selected = Array.isArray(value) ? value.includes(option) : false;
              return (
                <div key={idx} className="flex items-center space-x-2">
                  <Checkbox
                    id={`${field.name}-${idx}`}
                    checked={selected}
                    onCheckedChange={(checked) => {
                      const current = Array.isArray(value) ? value : [];
                      if (checked) {
                        onChange([...current, option]);
                      } else {
                        onChange(current.filter((v: string) => v !== option));
                      }
                    }}
                  />
                  <Label htmlFor={`${field.name}-${idx}`} className="font-normal">
                    {option}
                  </Label>
                </div>
              );
            })}
          </div>
          {errorElement}
        </div>
      );

    case 'file':
      return (
        <div className="space-y-2">
          {labelElement}
          <Input
            id={field.name}
            type="file"
            onChange={(e) => {
              // For now, just store the filename - actual file upload would need more handling
              const file = e.target.files?.[0];
              if (file) {
                onChange(file.name);
              }
            }}
          />
          {errorElement}
        </div>
      );

    default:
      return null;
  }
}
