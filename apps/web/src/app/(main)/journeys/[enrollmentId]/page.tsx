import { MyJourneyDetail } from '@/features/journeys/components';

interface PageProps {
  params: Promise<{ enrollmentId: string }>;
}

export const metadata = {
  title: 'Journey Details',
};

export default async function MyJourneyDetailPage({ params }: PageProps) {
  const { enrollmentId } = await params;
  return <MyJourneyDetail enrollmentId={enrollmentId} />;
}
