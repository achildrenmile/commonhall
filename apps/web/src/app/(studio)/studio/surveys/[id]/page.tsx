import { SurveyBuilder } from '@/features/surveys/components';

interface PageProps {
  params: Promise<{ id: string }>;
}

export const metadata = {
  title: 'Edit Survey | Studio',
};

export default async function SurveyEditorPage({ params }: PageProps) {
  const { id } = await params;
  return <SurveyBuilder id={id} />;
}
