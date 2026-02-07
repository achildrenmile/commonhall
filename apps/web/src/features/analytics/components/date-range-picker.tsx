'use client';

import { useState } from 'react';
import { Calendar, ChevronDown } from 'lucide-react';
import { format, subDays } from 'date-fns';
import { Button } from '@/components/ui/button';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import type { DateRangePreset, DateRange } from '../types';

interface DateRangePickerProps {
  value: DateRange;
  onChange: (range: DateRange) => void;
}

const presets: { label: string; value: DateRangePreset; getDates: () => DateRange }[] = [
  {
    label: 'Last 7 days',
    value: '7d',
    getDates: () => ({ from: subDays(new Date(), 7), to: new Date() }),
  },
  {
    label: 'Last 30 days',
    value: '30d',
    getDates: () => ({ from: subDays(new Date(), 30), to: new Date() }),
  },
  {
    label: 'Last 90 days',
    value: '90d',
    getDates: () => ({ from: subDays(new Date(), 90), to: new Date() }),
  },
];

export function DateRangePicker({ value, onChange }: DateRangePickerProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [customFrom, setCustomFrom] = useState(format(value.from, 'yyyy-MM-dd'));
  const [customTo, setCustomTo] = useState(format(value.to, 'yyyy-MM-dd'));

  const handlePresetClick = (preset: typeof presets[number]) => {
    onChange(preset.getDates());
    setIsOpen(false);
  };

  const handleCustomApply = () => {
    const from = new Date(customFrom);
    const to = new Date(customTo);
    if (!isNaN(from.getTime()) && !isNaN(to.getTime()) && from <= to) {
      onChange({ from, to });
      setIsOpen(false);
    }
  };

  const displayText = `${format(value.from, 'MMM d, yyyy')} - ${format(value.to, 'MMM d, yyyy')}`;

  return (
    <Popover open={isOpen} onOpenChange={setIsOpen}>
      <PopoverTrigger asChild>
        <Button variant="outline" className="gap-2">
          <Calendar className="h-4 w-4" />
          <span className="hidden sm:inline">{displayText}</span>
          <span className="sm:hidden">Date Range</span>
          <ChevronDown className="h-4 w-4" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-80" align="end">
        <div className="space-y-4">
          <div className="space-y-2">
            <p className="text-sm font-medium">Quick Select</p>
            <div className="flex flex-wrap gap-2">
              {presets.map((preset) => (
                <Button
                  key={preset.value}
                  variant="outline"
                  size="sm"
                  onClick={() => handlePresetClick(preset)}
                >
                  {preset.label}
                </Button>
              ))}
            </div>
          </div>

          <div className="border-t pt-4 space-y-3">
            <p className="text-sm font-medium">Custom Range</p>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <Label htmlFor="from" className="text-xs">From</Label>
                <Input
                  id="from"
                  type="date"
                  value={customFrom}
                  onChange={(e) => setCustomFrom(e.target.value)}
                />
              </div>
              <div className="space-y-1">
                <Label htmlFor="to" className="text-xs">To</Label>
                <Input
                  id="to"
                  type="date"
                  value={customTo}
                  onChange={(e) => setCustomTo(e.target.value)}
                />
              </div>
            </div>
            <Button size="sm" className="w-full" onClick={handleCustomApply}>
              Apply
            </Button>
          </div>
        </div>
      </PopoverContent>
    </Popover>
  );
}
