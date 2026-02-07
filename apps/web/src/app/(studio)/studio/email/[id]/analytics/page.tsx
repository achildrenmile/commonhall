import { NewsletterAnalytics } from '@/features/email/components';

interface PageProps {
  params: Promise<{ id: string }>;
}

export const metadata = {
  title: 'Newsletter Analytics | Studio',
};

export default async function NewsletterAnalyticsPage({ params }: PageProps) {
  const { id } = await params;
  return <NewsletterAnalytics id={id} />;
}
