'use client';

import { useParams } from 'next/navigation';
import { useNewsArticle } from '@/features/news';
import {
  ArticleHero,
  ArticleInteractionBar,
  ArticleSkeleton,
  CommentsSection,
  RelatedArticles,
} from '@/features/news';
import { WidgetRenderer } from '@/features/pages/widgets';

export default function ArticleDetailPage() {
  const params = useParams<{ slug: string }>();
  const { data: article, isLoading, error } = useNewsArticle(params.slug);

  if (isLoading) {
    return <ArticleSkeleton />;
  }

  if (error || !article) {
    return (
      <div className="max-w-4xl mx-auto">
        <div className="flex flex-col items-center justify-center min-h-[400px] text-center">
          <div className="text-6xl mb-4">📰</div>
          <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100 mb-2">
            Article not found
          </h2>
          <p className="text-slate-500">
            The article you're looking for doesn't exist or has been removed.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto">
      {/* Hero Section */}
      <ArticleHero article={article} />

      {/* Article Content */}
      {article.content && article.content.length > 0 && (
        <div className="mb-8">
          <WidgetRenderer widgets={article.content} />
        </div>
      )}

      {/* Interaction Bar (Floating) */}
      <ArticleInteractionBar
        articleId={article.id}
        articleSlug={article.slug}
        likeCount={article.likeCount}
        commentCount={article.commentCount}
        hasLiked={article.hasLiked || false}
      />

      {/* Comments Section */}
      <CommentsSection
        articleId={article.id}
        commentCount={article.commentCount}
      />

      {/* Related Articles */}
      {article.channel && (
        <RelatedArticles
          channelSlug={article.channel.slug}
          channelName={article.channel.name}
          excludeArticleId={article.id}
        />
      )}
    </div>
  );
}
