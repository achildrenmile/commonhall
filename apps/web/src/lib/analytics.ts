'use client';

import { apiClient } from './api-client';

// Event types supported by the analytics system
export type AnalyticsEventType =
  | 'page_view'
  | 'article_view'
  | 'article_like'
  | 'article_comment'
  | 'search_query'
  | 'file_download'
  | 'survey_response'
  | 'email_open'
  | 'email_click'
  | 'login'
  | 'community_post'
  | 'journey_step_view';

interface AnalyticsEvent {
  eventType: AnalyticsEventType;
  targetType?: string;
  targetId?: string;
  metadata?: Record<string, unknown>;
  channel?: string;
  deviceType?: string;
  sessionId?: string;
  timestamp?: string;
}

interface AnalyticsConfig {
  bufferSize: number;
  flushIntervalMs: number;
  enabled: boolean;
}

const DEFAULT_CONFIG: AnalyticsConfig = {
  bufferSize: 20,
  flushIntervalMs: 10000,
  enabled: true,
};

class AnalyticsClient {
  private buffer: AnalyticsEvent[] = [];
  private flushTimer: NodeJS.Timeout | null = null;
  private sessionId: string | null = null;
  private deviceType: string | null = null;
  private config: AnalyticsConfig = DEFAULT_CONFIG;
  private initialized = false;

  constructor() {
    if (typeof window !== 'undefined') {
      this.initialize();
    }
  }

  private initialize(): void {
    if (this.initialized) return;
    this.initialized = true;

    // Get or create session ID
    this.sessionId = this.getOrCreateSessionId();
    this.deviceType = this.detectDeviceType();

    // Set up periodic flush
    this.startFlushTimer();

    // Flush on page unload
    if (typeof window !== 'undefined') {
      window.addEventListener('beforeunload', () => this.flush(true));
      window.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'hidden') {
          this.flush(true);
        }
      });
    }
  }

  private getOrCreateSessionId(): string {
    if (typeof window === 'undefined') return '';

    let sessionId = sessionStorage.getItem('analytics_session_id');
    if (!sessionId) {
      sessionId = `${Date.now()}-${Math.random().toString(36).substring(2, 15)}`;
      sessionStorage.setItem('analytics_session_id', sessionId);

      // Track session start
      this.track('page_view', { isSessionStart: true });
    }
    return sessionId;
  }

  private detectDeviceType(): string {
    if (typeof window === 'undefined') return 'unknown';

    const ua = navigator.userAgent.toLowerCase();

    if (/mobile|android|iphone|ipad|ipod|blackberry|iemobile|opera mini/i.test(ua)) {
      if (/ipad/i.test(ua)) return 'tablet';
      return 'mobile';
    }

    return 'desktop';
  }

  private startFlushTimer(): void {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
    }

    this.flushTimer = setInterval(() => {
      this.flush();
    }, this.config.flushIntervalMs);
  }

  /**
   * Track an analytics event
   */
  track(
    eventType: AnalyticsEventType,
    data?: {
      targetType?: string;
      targetId?: string;
      metadata?: Record<string, unknown>;
    }
  ): void {
    if (!this.config.enabled || typeof window === 'undefined') return;

    const event: AnalyticsEvent = {
      eventType,
      targetType: data?.targetType,
      targetId: data?.targetId,
      metadata: data?.metadata,
      channel: 'web',
      deviceType: this.deviceType || undefined,
      sessionId: this.sessionId || undefined,
      timestamp: new Date().toISOString(),
    };

    this.buffer.push(event);

    // Flush if buffer is full
    if (this.buffer.length >= this.config.bufferSize) {
      this.flush();
    }
  }

  /**
   * Track a page view
   */
  trackPageView(path: string, title?: string): void {
    this.track('page_view', {
      metadata: {
        path,
        title: title || document.title,
        referrer: document.referrer,
      },
    });
  }

  /**
   * Track an article view
   */
  trackArticleView(articleId: string, slug: string, title: string): void {
    this.track('article_view', {
      targetType: 'article',
      targetId: articleId,
      metadata: { slug, title },
    });
  }

  /**
   * Track article like
   */
  trackArticleLike(articleId: string): void {
    this.track('article_like', {
      targetType: 'article',
      targetId: articleId,
    });
  }

  /**
   * Track article comment
   */
  trackArticleComment(articleId: string): void {
    this.track('article_comment', {
      targetType: 'article',
      targetId: articleId,
    });
  }

  /**
   * Track a search query
   */
  trackSearch(query: string, resultCount: number, clicked = false): void {
    this.track('search_query', {
      metadata: { query, resultCount, clicked },
    });
  }

  /**
   * Track file download
   */
  trackFileDownload(fileId: string, fileName: string): void {
    this.track('file_download', {
      targetType: 'file',
      targetId: fileId,
      metadata: { fileName },
    });
  }

  /**
   * Track survey response
   */
  trackSurveyResponse(surveyId: string): void {
    this.track('survey_response', {
      targetType: 'survey',
      targetId: surveyId,
    });
  }

  /**
   * Track email open (called from email tracking pixel)
   */
  trackEmailOpen(newsletterId: string): void {
    this.track('email_open', {
      targetType: 'newsletter',
      targetId: newsletterId,
    });
  }

  /**
   * Track email click
   */
  trackEmailClick(newsletterId: string, linkUrl: string): void {
    this.track('email_click', {
      targetType: 'newsletter',
      targetId: newsletterId,
      metadata: { linkUrl },
    });
  }

  /**
   * Track login event
   */
  trackLogin(): void {
    this.track('login');
  }

  /**
   * Track community post
   */
  trackCommunityPost(communityId: string): void {
    this.track('community_post', {
      targetType: 'community',
      targetId: communityId,
    });
  }

  /**
   * Track journey step view
   */
  trackJourneyStepView(journeyId: string, stepId: string): void {
    this.track('journey_step_view', {
      targetType: 'journey',
      targetId: journeyId,
      metadata: { stepId },
    });
  }

  /**
   * Flush buffered events to the server
   */
  async flush(sync = false): Promise<void> {
    if (this.buffer.length === 0) return;

    const events = [...this.buffer];
    this.buffer = [];

    try {
      if (sync && typeof navigator !== 'undefined' && 'sendBeacon' in navigator) {
        // Use sendBeacon for synchronous flush (page unload)
        const blob = new Blob([JSON.stringify({ events })], {
          type: 'application/json',
        });
        const apiUrl = process.env.NEXT_PUBLIC_API_URL || '';
        navigator.sendBeacon(`${apiUrl}/api/v1/analytics/track`, blob);
      } else {
        // Use regular API client for async flush
        await apiClient.post('/api/v1/analytics/track', { events });
      }
    } catch (error) {
      // Re-add events to buffer on failure (up to limit)
      if (this.buffer.length + events.length <= 100) {
        this.buffer = [...events, ...this.buffer];
      }
      console.error('Failed to flush analytics events:', error);
    }
  }

  /**
   * Configure analytics client
   */
  configure(config: Partial<AnalyticsConfig>): void {
    this.config = { ...this.config, ...config };

    if (config.flushIntervalMs) {
      this.startFlushTimer();
    }
  }

  /**
   * Enable or disable analytics
   */
  setEnabled(enabled: boolean): void {
    this.config.enabled = enabled;
  }
}

// Export singleton instance
export const analytics = new AnalyticsClient();

// Export convenience functions
export const track = analytics.track.bind(analytics);
export const trackPageView = analytics.trackPageView.bind(analytics);
export const trackArticleView = analytics.trackArticleView.bind(analytics);
export const trackArticleLike = analytics.trackArticleLike.bind(analytics);
export const trackArticleComment = analytics.trackArticleComment.bind(analytics);
export const trackSearch = analytics.trackSearch.bind(analytics);
export const trackFileDownload = analytics.trackFileDownload.bind(analytics);
export const trackSurveyResponse = analytics.trackSurveyResponse.bind(analytics);
export const trackEmailOpen = analytics.trackEmailOpen.bind(analytics);
export const trackEmailClick = analytics.trackEmailClick.bind(analytics);
export const trackLogin = analytics.trackLogin.bind(analytics);
export const trackCommunityPost = analytics.trackCommunityPost.bind(analytics);
export const trackJourneyStepView = analytics.trackJourneyStepView.bind(analytics);
