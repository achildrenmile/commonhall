import { lazy, type ComponentType } from 'react';
import type { WidgetProps, WidgetType } from './types';

// Lazy load widget components for better code splitting
const RichTextWidget = lazy(() => import('./components/rich-text-widget'));
const HeroImageWidget = lazy(() => import('./components/hero-image-widget'));
const InfoBoxWidget = lazy(() => import('./components/info-box-widget'));
const FileListWidget = lazy(() => import('./components/file-list-widget'));
const NewsFeedWidget = lazy(() => import('./components/news-feed-widget'));
const StaticContentWidget = lazy(() => import('./components/static-content-widget'));
const UserProfileWidget = lazy(() => import('./components/user-profile-widget'));
const ButtonWidget = lazy(() => import('./components/button-widget'));
const AccordionWidget = lazy(() => import('./components/accordion-widget'));

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type WidgetComponent = ComponentType<WidgetProps<any>>;

const widgetRegistry: Record<WidgetType, WidgetComponent> = {
  'rich-text': RichTextWidget,
  'hero-image': HeroImageWidget,
  'info-box': InfoBoxWidget,
  'file-list': FileListWidget,
  'news-feed': NewsFeedWidget,
  'static-content': StaticContentWidget,
  'user-profile': UserProfileWidget,
  'button': ButtonWidget,
  'accordion': AccordionWidget,
};

export function getWidgetComponent(type: string): WidgetComponent | null {
  return widgetRegistry[type as WidgetType] ?? null;
}

export function isValidWidgetType(type: string): type is WidgetType {
  return type in widgetRegistry;
}

export { widgetRegistry };
