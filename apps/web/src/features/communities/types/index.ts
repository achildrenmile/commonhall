export type CommunityType = 'Open' | 'Closed' | 'Assigned';
export type CommunityPostPermission = 'Anyone' | 'MembersOnly' | 'AdminsOnly';
export type CommunityMemberRole = 'Member' | 'Moderator' | 'Admin';

export interface CommunityListItem {
  id: string;
  name: string;
  slug: string;
  description?: string;
  coverImageUrl?: string;
  type: CommunityType;
  memberCount: number;
  isMember: boolean;
  myRole?: CommunityMemberRole;
}

export interface CommunityDetail {
  id: string;
  name: string;
  slug: string;
  description?: string;
  coverImageUrl?: string;
  type: CommunityType;
  postPermission: CommunityPostPermission;
  memberCount: number;
  isArchived: boolean;
  spaceId?: string;
  spaceName?: string;
  isMember: boolean;
  myRole?: CommunityMemberRole;
  canJoin: boolean;
  canPost: boolean;
  isRestricted?: boolean;
  createdAt: string;
}

export interface CommunityMember {
  id: string;
  userId: string;
  firstName?: string;
  lastName?: string;
  email?: string;
  profilePhotoUrl?: string;
  role: CommunityMemberRole;
  joinedAt: string;
}

export interface CommunityPost {
  id: string;
  authorId: string;
  authorFirstName?: string;
  authorLastName?: string;
  authorProfilePhotoUrl?: string;
  body: string;
  imageUrl?: string;
  isPinned: boolean;
  likeCount: number;
  commentCount: number;
  hasLiked: boolean;
  createdAt: string;
}

export interface CommunityComment {
  id: string;
  authorId: string;
  authorFirstName?: string;
  authorLastName?: string;
  authorProfilePhotoUrl?: string;
  body: string;
  createdAt: string;
}

export interface CreateCommunityInput {
  name: string;
  description?: string;
  coverImageUrl?: string;
  type?: CommunityType;
  postPermission?: CommunityPostPermission;
  spaceId?: string;
}
