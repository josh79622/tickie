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

// Each status gets its own hue for its badge; null means a neutral grey.
export const statusHue: Record<TaskStatus, number | null> = {
  Todo: null,
  TestCases: 200,
  WritingTests: 225,
  Working: 255,
  Testing: 180,
  Fixing: 25,
  NeedsDecision: 0,
  AwaitingConfirmation: 45,
  Done: 140,
  Cancelled: null,
  Demo: 285,
  Adopted: 140,
  Rejected: 340,
  WaitingForDev: 210,
  Running: 165,
  Analysing: 310,
}

