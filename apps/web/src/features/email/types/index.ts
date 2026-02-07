export type NewsletterStatus = 'Draft' | 'Scheduled' | 'Sending' | 'Sent' | 'Failed';
export type DistributionType = 'AllUsers' | 'UserGroups' | 'CustomList';
export type EmailTemplateCategory = 'Newsletter' | 'Announcement' | 'Digest' | 'Alert' | 'Welcome' | 'Custom';

export type EmailBlockType =
  | 'header'
  | 'heading'
  | 'text'
  | 'image'
  | 'button'
  | 'divider'
  | 'columns'
  | 'spacer'
  | 'footer'
  | 'news-preview';

export interface EmailBlock {
  id: string;
  type: EmailBlockType;
  data: Record<string, unknown>;
}

export interface HeaderBlockData {
  logoUrl?: string;
  title?: string;
  backgroundColor?: string;
  textColor?: string;
}

export interface HeadingBlockData {
  text: string;
  level: 1 | 2 | 3 | 4;
  alignment?: 'left' | 'center' | 'right';
}

export interface TextBlockData {
  html: string;
}

export interface ImageBlockData {
  src: string;
  alt?: string;
  width?: string;
  alignment?: 'left' | 'center' | 'right';
  linkUrl?: string;
}

export interface ButtonBlockData {
  text: string;
  url: string;
  alignment?: 'left' | 'center' | 'right';
  backgroundColor?: string;
  textColor?: string;
}

export interface SpacerBlockData {
  height: number;
}

export interface FooterBlockData {
  text?: string;
  showUnsubscribe?: boolean;
  backgroundColor?: string;
}

export interface NewsPreviewBlockData {
  title: string;
  teaser?: string;
  imageUrl?: string;
  linkUrl: string;
}

export interface ColumnsBlockData {
  columns: Array<{
    blocks: EmailBlock[];
  }>;
}

export interface EmailTemplate {
  id: string;
  name: string;
  description?: string;
  thumbnailUrl?: string;
  isSystem: boolean;
  category: EmailTemplateCategory;
  createdAt: string;
  updatedAt: string;
}

export interface EmailTemplateDetail extends EmailTemplate {
  content: string;
}

export interface Newsletter {
  id: string;
  title: string;
  subject: string;
  status: NewsletterStatus;
  distributionType: DistributionType;
  scheduledAt?: string;
  sentAt?: string;
  createdAt: string;
  recipientCount: number;
  openCount: number;
  clickCount: number;
  openRate: number;
  clickRate: number;
}

export interface NewsletterDetail {
  id: string;
  title: string;
  subject: string;
  previewText?: string;
  content: string;
  templateId?: string;
  templateName?: string;
  status: NewsletterStatus;
  distributionType: DistributionType;
  targetGroupIds?: string;
  scheduledAt?: string;
  sentAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface NewsletterAnalytics {
  totalRecipients: number;
  sent: number;
  delivered: number;
  opened: number;
  clicked: number;
  bounced: number;
  openRate: number;
  clickRate: number;
  clickToOpenRate: number;
  topLinks: LinkAnalytics[];
  openTimeline: TimeSeriesPoint[];
  deviceBreakdown: DeviceStats[];
}

export interface LinkAnalytics {
  url: string;
  clicks: number;
  uniqueClicks: number;
}

export interface TimeSeriesPoint {
  time: string;
  value: number;
}

export interface DeviceStats {
  device: string;
  count: number;
  percentage: number;
}

export interface CreateNewsletterInput {
  title: string;
  subject: string;
  previewText?: string;
  content?: string;
  templateId?: string;
  distributionType?: DistributionType;
  targetGroupIds?: string;
}

export interface UpdateNewsletterInput {
  title?: string;
  subject?: string;
  previewText?: string;
  content?: string;
  templateId?: string;
  distributionType?: DistributionType;
  targetGroupIds?: string;
}
