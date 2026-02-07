'use client';

import Image from 'next/image';

interface SpaceCoverProps {
  imageUrl?: string;
  name: string;
  description?: string;
}

export function SpaceCover({ imageUrl, name, description }: SpaceCoverProps) {
  if (!imageUrl) {
    return (
      <div className="relative w-full h-48 sm:h-64 bg-gradient-to-br from-slate-800 to-slate-900 rounded-xl overflow-hidden mb-8">
        <div className="absolute inset-0 flex flex-col items-center justify-center text-center p-6">
          <h1 className="text-2xl sm:text-3xl lg:text-4xl font-bold text-white mb-2">
            {name}
          </h1>
          {description && (
            <p className="text-base sm:text-lg text-white/80 max-w-2xl">
              {description}
            </p>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="relative w-full h-48 sm:h-64 rounded-xl overflow-hidden mb-8">
      <Image
        src={imageUrl}
        alt={name}
        fill
        className="object-cover"
        priority
        sizes="(max-width: 1200px) 100vw, 1200px"
      />
      <div className="absolute inset-0 bg-black/40" />
      <div className="absolute inset-0 flex flex-col items-center justify-center text-center p-6">
        <h1 className="text-2xl sm:text-3xl lg:text-4xl font-bold text-white mb-2">
          {name}
        </h1>
        {description && (
          <p className="text-base sm:text-lg text-white/90 max-w-2xl">
            {description}
          </p>
        )}
      </div>
    </div>
  );
}
