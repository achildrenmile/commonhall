export interface HeadlineRequest {
  articleBody: string;
  tone?: string;
}

export interface HeadlineResponse {
  headlines: string[];
}

export interface TeaserRequest {
  articleBody: string;
  maxLength?: number;
}

export interface TeaserResponse {
  teaser: string;
}

export interface ImproveRequest {
  text: string;
  instruction: string;
}

export interface SummarizeRequest {
  text: string;
  length?: 'short' | 'medium' | 'long';
}

export interface SummarizeResponse {
  summary: string;
}

export interface TranslateRequest {
  text: string;
  targetLanguage: string;
}

export interface TranslateResponse {
  translation: string;
}

export interface BriefingRequest {
  purpose: string;
  audience: string;
  tone: string;
  keyPoints: string[];
  attachmentTexts?: string[];
}

export interface AskRequest {
  question: string;
  conversationHistory?: ConversationMessage[];
}

export interface ConversationMessage {
  role: 'user' | 'assistant';
  content: string;
}

export interface SourceReference {
  title: string;
  type: string;
  url: string;
  excerpt?: string;
}

export interface ContentHealthReport {
  id: string;
  scanStartedAt: string;
  scanCompletedAt?: string;
  status: 'running' | 'completed' | 'failed';
  totalContentCount: number;
  staleContentCount: number;
  brokenLinkCount: number;
  unusedContentCount: number;
  lowEngagementCount: number;
  summary?: string;
  issues: ContentHealthIssue[];
}

export interface ContentHealthIssue {
  id: string;
  contentType: string;
  contentId: string;
  contentTitle: string;
  contentUrl: string;
  issueType: 'stale' | 'broken_link' | 'unused' | 'low_engagement';
  severity: 'low' | 'medium' | 'high';
  description: string;
  recommendation?: string;
  isResolved: boolean;
  resolvedAt?: string;
}

export interface ContentHealthReportSummary {
  id: string;
  scanStartedAt: string;
  scanCompletedAt?: string;
  status: string;
  totalIssueCount: number;
}
