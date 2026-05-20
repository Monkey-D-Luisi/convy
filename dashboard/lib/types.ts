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
  releaseSha: string | null;
  operatingSystem: string | null;
  architecture: string | null;
  processorCount: number;
  cpuModel: string | null;
  memoryTotalBytes: number | null;
  memoryAvailableBytes: number | null;
  diskTotalBytes: number | null;
  uptimeSeconds: number | null;
  loadAverage1m: number | null;
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
  aiRequests7d: number;
  aiSuccesses7d: number;
  aiFailures7d: number;
  voiceItemsCreated7d: number;
  estimatedAiCostMicros7d: number | null;
  lastBackup: BackupRun | null;
  health: SystemHealth;
};

export type UsageMetric = {
  date: string;
  householdsActive: number;
  itemsCreated: number;
  itemsCompleted: number;
  itemsUncompleted: number;
  itemsDeleted: number;
  itemCompletionsCreatedSameDay: number;
  itemCompletionsFromBacklog: number;
  tasksCreated: number;
  tasksCompleted: number;
  tasksUncompleted: number;
  tasksDeleted: number;
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
  inputTokens: number;
  outputTokens: number;
  cachedTokens: number;
  reasoningTokens: number;
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
  inputTokens: number;
  outputTokens: number;
  cachedTokens: number;
  reasoningTokens: number;
  estimatedCostMicros: number | null;
  days: VoiceMetric[];
};

export type OpenAiMetric = {
  date: string;
  requests: number;
  successes: number;
  failures: number;
  inputTokens: number;
  outputTokens: number;
  cachedTokens: number;
  reasoningTokens: number;
  audioTokens: number;
  textTokens: number;
  audioDurationSeconds: number;
  estimatedCostMicros: number | null;
  averageLatencyMs: number | null;
};

export type OpenAiOperationMetric = {
  feature: string;
  operation: string;
  model: string | null;
  requests: number;
  successes: number;
  failures: number;
  inputTokens: number;
  outputTokens: number;
  cachedTokens: number;
  reasoningTokens: number;
  audioTokens: number;
  textTokens: number;
  audioDurationSeconds: number;
  estimatedCostMicros: number | null;
  averageLatencyMs: number | null;
};

export type OpenAiMetrics = {
  from: string;
  to: string;
  requests: number;
  successes: number;
  failures: number;
  inputTokens: number;
  outputTokens: number;
  cachedTokens: number;
  reasoningTokens: number;
  audioTokens: number;
  textTokens: number;
  audioDurationSeconds: number;
  estimatedCostMicros: number | null;
  averageLatencyMs: number | null;
  days: OpenAiMetric[];
  operations: OpenAiOperationMetric[];
};
