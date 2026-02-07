import { FormSubmissions } from '@/features/forms/components';

interface PageProps {
  params: Promise<{ id: string }>;
}

export const metadata = {
  title: 'Form Submissions | Studio',
};

export default async function FormSubmissionsPage({ params }: PageProps) {
  const { id } = await params;
  return <FormSubmissions id={id} />;
}
