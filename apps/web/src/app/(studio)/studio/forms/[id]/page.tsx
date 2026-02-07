import { FormBuilder } from '@/features/forms/components';

interface PageProps {
  params: Promise<{ id: string }>;
}

export const metadata = {
  title: 'Edit Form | Studio',
};

export default async function FormEditorPage({ params }: PageProps) {
  const { id } = await params;
  return <FormBuilder id={id} />;
}
