import { SurveyAnalytics } from '@/features/surveys/components';

interface PageProps {
  params: Promise<{ id: string }>;
}

export const metadata = {
  title: 'Survey Analytics | Studio',
};

export default async function SurveyAnalyticsPage({ params }: PageProps) {
  const { id } = await params;
  return <SurveyAnalytics id={id} />;
}
