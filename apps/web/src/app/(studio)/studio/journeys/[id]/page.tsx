import { JourneyBuilder } from '@/features/journeys/components';

interface PageProps {
  params: Promise<{ id: string }>;
}

export const metadata = {
  title: 'Edit Journey | Studio',
};

export default async function JourneyEditorPage({ params }: PageProps) {
  const { id } = await params;
  return <JourneyBuilder id={id} />;
}
