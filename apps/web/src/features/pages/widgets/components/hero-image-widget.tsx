'use client';

import Image from 'next/image';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import type { WidgetProps, HeroImageData } from '../types';

export default function HeroImageWidget({ data }: WidgetProps<HeroImageData>) {
  const overlayOpacity = data.overlayOpacity ?? 0.4;

  return (
    <div className="relative w-full h-[300px] sm:h-[400px] lg:h-[500px] rounded-xl overflow-hidden">
      {/* Background Image */}
      <Image
        src={data.imageUrl}
        alt={data.headline || 'Hero image'}
        fill
        className="object-cover"
        priority
        sizes="(max-width: 768px) 100vw, (max-width: 1200px) 80vw, 1200px"
      />

      {/* Overlay */}
      <div
        className="absolute inset-0 bg-black"
        style={{ opacity: overlayOpacity }}
      />

      {/* Content */}
      <div className="absolute inset-0 flex flex-col items-center justify-center text-center p-6">
        {data.headline && (
          <h1 className="text-3xl sm:text-4xl lg:text-5xl font-bold text-white mb-4 max-w-4xl">
            {data.headline}
          </h1>
        )}

        {data.subheadline && (
          <p className="text-lg sm:text-xl text-white/90 mb-6 max-w-2xl">
            {data.subheadline}
          </p>
        )}

        {data.buttonText && data.buttonUrl && (
          <Button
            asChild
            size="lg"
            className="bg-white text-slate-900 hover:bg-white/90"
          >
            <Link href={data.buttonUrl}>
              {data.buttonText}
            </Link>
          </Button>
        )}
      </div>
    </div>
  );
}
