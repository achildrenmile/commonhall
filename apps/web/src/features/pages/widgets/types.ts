export type WidgetType =
  | 'rich-text'
  | 'hero-image'
  | 'info-box'
  | 'file-list'
  | 'news-feed'
  | 'static-content'
  | 'user-profile'
  | 'button'
  | 'accordion';

export type VisibilityCondition = {
  type: 'role' | 'group' | 'authenticated';
  value?: string;
};

export interface WidgetBlock<T = Record<string, unknown>> {
  id: string;
  type: WidgetType | string;
  data: T;
  visibility?: VisibilityCondition;
}

// Widget-specific data types
export interface RichTextData {
  html: string;
}

export interface HeroImageData {
  imageUrl: string;
  headline?: string;
  subheadline?: string;
  buttonText?: string;
  buttonUrl?: string;
  overlayOpacity?: number;
}

export interface InfoBoxData {
  variant: 'info' | 'warning' | 'success' | 'error' | 'tip';
  title?: string;
  body: string;
}

export interface FileListData {
  fileIds: string[];
  title?: string;
}

export interface NewsFeedData {
  channelSlug?: string;
  spaceSlug?: string;
  limit?: number;
  showImages?: boolean;
}

export interface StaticContentData {
  html: string;
  backgroundColor?: string;
  textColor?: string;
}

export interface UserProfileData {
  userId: string;
}

export interface ButtonData {
  text: string;
  url: string;
  variant?: 'default' | 'secondary' | 'outline' | 'ghost';
  alignment?: 'left' | 'center' | 'right';
}

export interface AccordionData {
  items: Array<{
    title: string;
    content: string;
  }>;
}

// Widget component props
export interface WidgetProps<T = Record<string, unknown>> {
  data: T;
  id: string;
}
