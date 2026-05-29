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
  mcpHealthy: boolean;
  authHealthy: boolean;
  mcpMetadataHealthy: boolean;
  authMetadataHealthy: boolean;
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

export type SystemHistory = {
  from: string;
  to: string;
  samples: SystemMetricSnapshot[];
};

export type SystemMetricSnapshot = {
  capturedAt: string;
  diskFreeBytes: number | null;
  diskTotalBytes: number | null;
  memoryAvailableBytes: number | null;
  memoryTotalBytes: number | null;
  loadAverage1m: number | null;
  uptimeSeconds: number | null;
  postgresDataSizeBytes: number | null;
};

export type McpSummary = {
  mcpHealthy: boolean;
  authHealthy: boolean;
  toolCalls24h: number;
  toolSuccesses24h: number;
  toolFailures24h: number;
  successRate24h: number;
  lastInvocationAt: string | null;
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
  mcp: McpSummary;
  lastBackup: BackupRun | null;
  health: SystemHealth;
  risk: AdminRiskSummary;
  growth: AdminGrowthSummary;
  engagement: AdminEngagementSummary;
  aiReliability: AdminAiReliabilitySummary;
  voiceReliability: AdminVoiceReliabilitySummary;
  backupHealth: AdminBackupHealthSummary;
};

export type AdminRiskSummary = {
  criticalCount: number;
  warningCount: number;
  items: AdminRiskItem[];
};

export type AdminRiskItem = {
  key: string;
  label: string;
  severity: string;
  detail: string;
  targetPath: string | null;
};

export type AdminGrowthSummary = {
  newUsers7d: number;
  newHouseholds7d: number;
  newLists7d: number;
  activeHouseholdRate7d: number;
  activeHouseholdRate30d: number;
};

export type AdminEngagementSummary = {
  activeHouseholds7d: number;
  itemsCreated7d: number;
  itemsCompleted7d: number;
  tasksCreated7d: number;
  tasksCompleted7d: number;
  itemCompletionRatio7d: number;
};

export type AdminAiReliabilitySummary = {
  requests7d: number;
  failures7d: number;
  failureRate7d: number;
  averageLatencyMs7d: number | null;
  estimatedCostMicros7d: number | null;
};

export type AdminVoiceReliabilitySummary = {
  requests7d: number;
  failures7d: number;
  successRate7d: number;
  parsedItems7d: number;
  itemsCreated7d: number;
};

export type AdminBackupHealthSummary = {
  successes30d: number;
  failures30d: number;
  lastSuccessfulAt: string | null;
  latestSuccessful: boolean;
  verificationHealthy: boolean;
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

export type McpRuntime = {
  mcpUrl: string;
  authUrl: string;
  issuer: string;
  audience: string;
  scopes: string[];
  mcpHealthHealthy: boolean;
  authHealthHealthy: boolean;
  mcpMetadataHealthy: boolean;
  authMetadataHealthy: boolean;
};

export type McpOAuthMetrics = {
  activeConsents: number;
  revokedConsents: number;
  activeRefreshTokens: number;
  revokedRefreshTokens: number;
  refreshTokensExpiring7d: number;
  lastConsentAt: string | null;
  lastTokenUsedAt: string | null;
  lastRevokedAt: string | null;
};

export type McpUsageMetrics = {
  invocations: number;
  successes: number;
  failures: number;
  validationErrors: number;
  unauthorized: number;
  forbidden: number;
  notFound: number;
  providerErrors: number;
  unexpectedErrors: number;
  successRate: number;
  averageLatencyMs: number | null;
  p95LatencyMs: number | null;
  lastInvocationAt: string | null;
};

export type McpDailyMetric = {
  date: string;
  invocations: number;
  successes: number;
  failures: number;
  averageLatencyMs: number | null;
};

export type McpToolMetric = {
  toolName: string;
  invocations: number;
  successes: number;
  failures: number;
  averageLatencyMs: number | null;
  p95LatencyMs: number | null;
  lastInvocationAt: string | null;
};

export type McpRecentInvocation = {
  createdAt: string;
  toolName: string;
  status: string;
  latencyMs: number;
  errorType: string | null;
  userId: string;
  householdId: string | null;
};

export type McpToolCatalogItem = {
  name: string;
  title: string;
  requiredScopes: string[];
  readOnlyHint: boolean;
  destructiveHint: boolean;
  idempotentHint: boolean;
  openWorldHint: boolean;
};

export type McpPublicationReadinessCheck = {
  key: string;
  label: string;
  status: string;
  details: string;
};

export type McpOverview = {
  from: string;
  to: string;
  runtime: McpRuntime;
  oauth: McpOAuthMetrics;
  usage: McpUsageMetrics;
  days: McpDailyMetric[];
  tools: McpToolMetric[];
  recentInvocations: McpRecentInvocation[];
  toolCatalog: McpToolCatalogItem[];
  readinessChecks: McpPublicationReadinessCheck[];
};
