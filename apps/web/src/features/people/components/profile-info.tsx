'use client';

import { Mail, Phone, Calendar, UserCircle } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { formatDate } from '@/lib/utils';
import type { PersonProfile } from '../api';

interface ProfileInfoProps {
  profile: PersonProfile;
}

export function ProfileInfo({ profile }: ProfileInfoProps) {
  return (
    <div className="grid gap-6 md:grid-cols-2">
      {/* Contact Information */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Contact Information</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {profile.email && (
            <div className="flex items-start gap-3">
              <Mail className="h-5 w-5 text-slate-400 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-slate-500">Email</p>
                <a
                  href={`mailto:${profile.email}`}
                  className="text-slate-900 dark:text-slate-100 hover:text-blue-600 dark:hover:text-blue-400"
                >
                  {profile.email}
                </a>
              </div>
            </div>
          )}
          {profile.phoneNumber && (
            <div className="flex items-start gap-3">
              <Phone className="h-5 w-5 text-slate-400 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-slate-500">Phone</p>
                <a
                  href={`tel:${profile.phoneNumber}`}
                  className="text-slate-900 dark:text-slate-100 hover:text-blue-600 dark:hover:text-blue-400"
                >
                  {profile.phoneNumber}
                </a>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Account Information */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Account Information</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-start gap-3">
            <UserCircle className="h-5 w-5 text-slate-400 mt-0.5" />
            <div>
              <p className="text-sm font-medium text-slate-500">Role</p>
              <p className="text-slate-900 dark:text-slate-100 capitalize">
                {profile.role.toLowerCase()}
              </p>
            </div>
          </div>
          <div className="flex items-start gap-3">
            <Calendar className="h-5 w-5 text-slate-400 mt-0.5" />
            <div>
              <p className="text-sm font-medium text-slate-500">Joined</p>
              <p className="text-slate-900 dark:text-slate-100">
                {formatDate(profile.createdAt)}
              </p>
            </div>
          </div>
          {profile.lastLoginAt && (
            <div className="flex items-start gap-3">
              <Calendar className="h-5 w-5 text-slate-400 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-slate-500">Last Active</p>
                <p className="text-slate-900 dark:text-slate-100">
                  {formatDate(profile.lastLoginAt)}
                </p>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Groups */}
      {profile.groups.length > 0 && (
        <Card className="md:col-span-2">
          <CardHeader>
            <CardTitle className="text-lg">Groups</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              {profile.groups.map((group) => (
                <Badge key={group.id} variant="secondary" className="text-sm">
                  {group.name}
                </Badge>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
