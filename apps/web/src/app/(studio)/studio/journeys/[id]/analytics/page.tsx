import { JourneyAnalytics } from '@/features/journeys/components';

interface PageProps {
  params: Promise<{ id: string }>;
}

export const metadata = {
  title: 'Journey Analytics | Studio',
};

export default async function JourneyAnalyticsPage({ params }: PageProps) {
  const { id } = await params;
  return <JourneyAnalytics id={id} />;
}
