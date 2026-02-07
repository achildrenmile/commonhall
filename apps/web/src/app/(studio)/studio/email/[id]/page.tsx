import { NewsletterEditor } from '@/features/email/components';

interface PageProps {
  params: Promise<{ id: string }>;
}

export const metadata = {
  title: 'Edit Newsletter | Studio',
};

export default async function NewsletterEditorPage({ params }: PageProps) {
  const { id } = await params;
  return <NewsletterEditor id={id} />;
}
