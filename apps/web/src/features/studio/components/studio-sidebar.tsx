'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import {
  LayoutDashboard,
  Calendar,
  Newspaper,
  FileText,
  FolderTree,
  Files,
  MessageSquare,
  Users,
  Settings,
  ChevronDown,
  Mail,
  Send,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuthStore } from '@/lib/auth-store';
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
import { useState } from 'react';

interface NavItem {
  title: string;
  href: string;
  icon: React.ElementType;
  adminOnly?: boolean;
  children?: Omit<NavItem, 'children' | 'icon'>[];
}

const navigation: NavItem[] = [
  {
    title: 'Dashboard',
    href: '/studio',
    icon: LayoutDashboard,
  },
  {
    title: 'Planning',
    href: '/studio/calendar',
    icon: Calendar,
  },
  {
    title: 'Content',
    href: '/studio/content',
    icon: Newspaper,
    children: [
      { title: 'News', href: '/studio/news' },
      { title: 'Pages', href: '/studio/pages' },
      { title: 'Spaces', href: '/studio/spaces' },
      { title: 'Files', href: '/studio/files' },
      { title: 'Comments', href: '/studio/comments' },
    ],
  },
  {
    title: 'Communicate',
    href: '/studio/communicate',
    icon: Send,
    children: [
      { title: 'Newsletters', href: '/studio/email' },
    ],
  },
  {
    title: 'Users',
    href: '/studio/users',
    icon: Users,
    adminOnly: true,
  },
  {
    title: 'Settings',
    href: '/studio/settings',
    icon: Settings,
    adminOnly: true,
  },
];

function NavLink({
  item,
  isActive,
}: {
  item: NavItem;
  isActive: boolean;
}) {
  const Icon = item.icon;

  return (
    <Link
      href={item.href}
      className={cn(
        'flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors',
        isActive
          ? 'bg-slate-100 dark:bg-slate-800 text-slate-900 dark:text-slate-100 font-medium'
          : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-slate-900 dark:hover:text-slate-100'
      )}
    >
      <Icon className="h-4 w-4" />
      {item.title}
    </Link>
  );
}

function NavGroup({
  item,
  pathname,
}: {
  item: NavItem;
  pathname: string;
}) {
  const Icon = item.icon;
  const isGroupActive = item.children?.some((child) => pathname.startsWith(child.href));
  const [isOpen, setIsOpen] = useState(isGroupActive);

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <CollapsibleTrigger className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-slate-900 dark:hover:text-slate-100 transition-colors">
        <Icon className="h-4 w-4" />
        <span className="flex-1 text-left">{item.title}</span>
        <ChevronDown
          className={cn(
            'h-4 w-4 transition-transform',
            isOpen && 'rotate-180'
          )}
        />
      </CollapsibleTrigger>
      <CollapsibleContent className="pl-4 mt-1 space-y-1">
        {item.children?.map((child) => {
          const isActive = pathname === child.href || pathname.startsWith(`${child.href}/`);
          return (
            <Link
              key={child.href}
              href={child.href}
              className={cn(
                'flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors',
                isActive
                  ? 'bg-slate-100 dark:bg-slate-800 text-slate-900 dark:text-slate-100 font-medium'
                  : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-slate-900 dark:hover:text-slate-100'
              )}
            >
              {child.title}
            </Link>
          );
        })}
      </CollapsibleContent>
    </Collapsible>
  );
}

export function StudioSidebar() {
  const pathname = usePathname();
  const user = useAuthStore((state) => state.user);
  const isAdmin = user?.role === 'Admin';

  return (
    <aside className="fixed left-0 top-14 bottom-0 w-56 border-r border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950 overflow-y-auto">
      <nav className="p-3 space-y-1">
        {navigation.map((item) => {
          // Hide admin-only items for non-admins
          if (item.adminOnly && !isAdmin) return null;

          // Has children - render as group
          if (item.children) {
            return <NavGroup key={item.href} item={item} pathname={pathname} />;
          }

          // Simple link
          const isActive = pathname === item.href;
          return <NavLink key={item.href} item={item} isActive={isActive} />;
        })}
      </nav>
    </aside>
  );
}
