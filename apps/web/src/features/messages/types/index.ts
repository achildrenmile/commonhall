export type ConversationType = 'Direct' | 'Group' | 'Managed';

export interface ConversationListItem {
  id: string;
  type: ConversationType;
  name?: string;
  otherParticipantId?: string;
  otherParticipantName?: string;
  otherParticipantProfilePhotoUrl?: string;
  participantCount: number;
  lastMessageAt?: string;
  lastMessagePreview?: string;
  lastMessageAuthor?: string;
  unreadCount: number;
}

export interface ConversationDetail {
  id: string;
  type: ConversationType;
  name?: string;
  createdBy: string;
  createdAt: string;
  participants: ConversationParticipant[];
}

export interface ConversationParticipant {
  id: string;
  conversationId: string;
  userId: string;
  firstName?: string;
  lastName?: string;
  email?: string;
  profilePhotoUrl?: string;
  joinedAt: string;
  lastReadAt?: string;
  isMuted: boolean;
}

export interface Message {
  id: string;
  conversationId: string;
  authorId: string;
  authorFirstName?: string;
  authorLastName?: string;
  authorProfilePhotoUrl?: string;
  body: string;
  attachments?: MessageAttachment[];
  createdAt: string;
  editedAt?: string;
  isDeleted: boolean;
}

export interface MessageAttachment {
  type: 'file' | 'image';
  url: string;
  name?: string;
  size?: number;
}

export interface CreateConversationInput {
  type: ConversationType;
  name?: string;
  participantUserIds: string[];
}

export interface SendMessageInput {
  body: string;
  attachments?: MessageAttachment[];
}

export interface UnreadCountResponse {
  totalUnread: number;
  conversationUnreads: Record<string, number>;
}
