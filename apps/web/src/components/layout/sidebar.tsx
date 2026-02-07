'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import {
  Home,
  Newspaper,
  FolderOpen,
  Users,
  MessageSquare,
  Palette,
  ChevronLeft,
  ChevronRight,
  Menu,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuthStore } from '@/lib/auth-store';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet';
import { ScrollArea } from '@/components/ui/scroll-area';

interface NavItem {
  name: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
  minRole?: 'User' | 'Editor' | 'Admin';
}

const navItems: NavItem[] = [
  { name: 'Home', href: '/', icon: Home },
  { name: 'News', href: '/news', icon: Newspaper },
  { name: 'Spaces', href: '/spaces', icon: FolderOpen },
  { name: 'People', href: '/people', icon: Users },
  { name: 'Messages', href: '/messages', icon: MessageSquare },
  { name: 'Studio', href: '/studio', icon: Palette, minRole: 'Editor' },
];

const roleHierarchy = { User: 0, Editor: 1, Admin: 2 };

interface SidebarContentProps {
  collapsed: boolean;
  onNavigate?: () => void;
}

function SidebarContent({ collapsed, onNavigate }: SidebarContentProps) {
  const pathname = usePathname();
  const user = useAuthStore((state) => state.user);

  const visibleNavItems = navItems.filter((item) => {
    if (!item.minRole) return true;
    if (!user) return false;
    return roleHierarchy[user.role] >= roleHierarchy[item.minRole];
  });

  return (
    <ScrollArea className="flex-1">
      <nav className="space-y-1 p-2">
        {visibleNavItems.map((item) => {
          const isActive = pathname === item.href ||
            (item.href !== '/' && pathname.startsWith(item.href));
          const Icon = item.icon;

          return (
            <Link
              key={item.name}
              href={item.href}
              onClick={onNavigate}
              className={cn(
                'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-slate-100 text-slate-900 dark:bg-slate-800 dark:text-slate-100'
                  : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-slate-100',
                collapsed && 'justify-center px-2'
              )}
            >
              <Icon className="h-5 w-5 shrink-0" />
              {!collapsed && <span>{item.name}</span>}
            </Link>
          );
        })}
      </nav>
    </ScrollArea>
  );
}

interface SidebarProps {
  className?: string;
}

export function Sidebar({ className }: SidebarProps) {
  const [collapsed, setCollapsed] = useState(false);

  return (
    <aside
      className={cn(
        'hidden lg:flex flex-col border-r border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-950 transition-all duration-300',
        collapsed ? 'w-16' : 'w-64',
        className
      )}
    >
      {/* Logo */}
      <div className={cn(
        'flex items-center border-b border-slate-200 dark:border-slate-800 h-16 px-4',
        collapsed && 'justify-center px-2'
      )}>
        <Link href="/" className="flex items-center gap-2">
          <div className="h-8 w-8 rounded-lg bg-primary flex items-center justify-center shrink-0">
            <span className="text-lg font-bold text-primary-foreground">C</span>
          </div>
          {!collapsed && (
            <span className="font-semibold text-slate-900 dark:text-slate-100">
              CommonHall
            </span>
          )}
        </Link>
      </div>

      {/* Navigation */}
      <SidebarContent collapsed={collapsed} />

      {/* Collapse toggle */}
      <div className="border-t border-slate-200 dark:border-slate-800 p-2">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setCollapsed(!collapsed)}
          className={cn('w-full', collapsed && 'px-2')}
        >
          {collapsed ? (
            <ChevronRight className="h-4 w-4" />
          ) : (
            <>
              <ChevronLeft className="h-4 w-4 mr-2" />
              <span>Collapse</span>
            </>
          )}
        </Button>
      </div>
    </aside>
  );
}

export function MobileSidebar() {
  const [open, setOpen] = useState(false);

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetTrigger asChild>
        <Button variant="ghost" size="icon" className="lg:hidden">
          <Menu className="h-5 w-5" />
          <span className="sr-only">Toggle menu</span>
        </Button>
      </SheetTrigger>
      <SheetContent side="left" className="w-64 p-0">
        {/* Logo */}
        <div className="flex items-center border-b border-slate-200 dark:border-slate-800 h-16 px-4">
          <Link href="/" className="flex items-center gap-2" onClick={() => setOpen(false)}>
            <div className="h-8 w-8 rounded-lg bg-primary flex items-center justify-center">
              <span className="text-lg font-bold text-primary-foreground">C</span>
            </div>
            <span className="font-semibold text-slate-900 dark:text-slate-100">
              CommonHall
            </span>
          </Link>
        </div>

        <SidebarContent collapsed={false} onNavigate={() => setOpen(false)} />
      </SheetContent>
    </Sheet>
  );
}
