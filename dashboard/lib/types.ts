export type BackupRun = {
  id: string;
  status: string;
  backupType: string;
  fileName: string | null;
  sizeBytes: number | null;
  sha256: string | null;
  durationMs: number;
  verificationStatus: string;
  errorMessage: string | null;
  startedAt: string;
  finishedAt: string;
};

export type SystemHealth = {
  apiHealthy: boolean;
  databaseHealthy: boolean;
  diskFreeBytes: number | null;
  postgresDataSizeBytes: number | null;
  backendVersion: string | null;
  androidVersion: string | null;
  lastDeployAt: string | null;
};

export type Overview = {
  usersTotal: number;
  householdsTotal: number;
  householdsActive7d: number;
  householdsActive30d: number;
  listsTotal: number;
  itemsCreated7d: number;
  itemsCompleted7d: number;
  tasksCreated7d: number;
  tasksCompleted7d: number;
  voiceParseRequests7d: number;
  voiceParseSuccess7d: number;
  voiceParseFailures7d: number;
  voiceItemsCreated7d: number;
  estimatedOpenAiCostMicros7d: number | null;
  lastBackup: BackupRun | null;
  health: SystemHealth;
};

export type UsageMetric = {
  date: string;
  householdsActive: number;
  itemsCreated: number;
  itemsCompleted: number;
  tasksCreated: number;
  tasksCompleted: number;
};

export type UsageMetrics = {
  from: string;
  to: string;
  days: UsageMetric[];
};

export type VoiceMetric = {
  date: string;
  requests: number;
  successes: number;
  failures: number;
  parsedItems: number;
  voiceItemsCreated: number;
  estimatedCostMicros: number | null;
};

export type VoiceMetrics = {
  from: string;
  to: string;
  requests: number;
  successes: number;
  failures: number;
  parsedItems: number;
  voiceItemsCreated: number;
  estimatedCostMicros: number | null;
  days: VoiceMetric[];
};
