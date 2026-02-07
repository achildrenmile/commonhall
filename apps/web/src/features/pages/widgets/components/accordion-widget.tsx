'use client';

import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion';
import type { WidgetProps, AccordionData } from '../types';

export default function AccordionWidget({ data }: WidgetProps<AccordionData>) {
  if (!data.items || data.items.length === 0) {
    return null;
  }

  return (
    <Accordion type="single" collapsible className="w-full">
      {data.items.map((item, index) => (
        <AccordionItem key={index} value={`item-${index}`}>
          <AccordionTrigger className="text-left">
            {item.title}
          </AccordionTrigger>
          <AccordionContent>
            <div className="prose prose-slate dark:prose-invert prose-sm max-w-none">
              {item.content}
            </div>
          </AccordionContent>
        </AccordionItem>
      ))}
    </Accordion>
  );
}
