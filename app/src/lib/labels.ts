import type { TaskStatus } from '@/types/models'

// Status values are stored in PascalCase; this is how they read on screen.
export const statusLabel: Record<TaskStatus, string> = {
  Todo: 'Todo',
  TestCases: 'Test cases',
  WritingTests: 'Writing tests',
  Working: 'Working',
  Testing: 'Testing',
  Fixing: 'Fixing',
  NeedsDecision: 'Needs decision',
  AwaitingConfirmation: 'Awaiting confirmation',
  Done: 'Done',
  Cancelled: 'Cancelled',
  Demo: 'Demo',
  Adopted: 'Adopted',
  Rejected: 'Rejected',
  WaitingForDev: 'Waiting for dev',
  Running: 'Running',
  Analysing: 'Analysing',
}
