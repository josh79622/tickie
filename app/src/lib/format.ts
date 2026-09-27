import type { IsoDateTime } from '@/types/models'

// e.g. "Sep 24, 19:00", in the viewer's own time zone
export const formatDateTime = (value: IsoDateTime): string =>
  new Date(value).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hourCycle: 'h23',
  })
