export type FormFieldType = 'text' | 'textarea' | 'email' | 'phone' | 'number' | 'date' | 'dropdown' | 'radio' | 'checkbox' | 'file';

export interface FormField {
  name: string;
  label?: string;
  type: FormFieldType;
  required: boolean;
  placeholder?: string;
  options?: string[];
}

export interface FormListItem {
  id: string;
  title: string;
  description?: string;
  isActive: boolean;
  submissionCount: number;
  createdAt: string;
}

export interface FormDetail {
  id: string;
  title: string;
  description?: string;
  fields: string; // JSON
  notificationEmail?: string;
  confirmationMessage?: string;
  isActive: boolean;
  spaceId?: string;
  spaceName?: string;
  submissionCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface FormSubmission {
  id: string;
  userId?: string;
  userName?: string;
  userEmail?: string;
  data: string; // JSON
  attachments?: string;
  createdAt: string;
}

export interface CreateFormInput {
  title: string;
  description?: string;
  fields?: string;
  notificationEmail?: string;
  confirmationMessage?: string;
  spaceId?: string;
}

export interface UpdateFormInput {
  title?: string;
  description?: string;
  fields?: string;
  notificationEmail?: string;
  confirmationMessage?: string;
  isActive?: boolean;
  spaceId?: string;
}

export interface SubmissionsResponse {
  items: FormSubmission[];
  nextCursor?: string;
  hasMore: boolean;
}
