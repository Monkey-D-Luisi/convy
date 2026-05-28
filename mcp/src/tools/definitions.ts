import { z } from "zod";
import { ConvyApiError, type ConvyApiClient } from "../convy-api-client.js";
import { readOnlyScopes, writeScopes, type SupportedScope } from "../metadata.js";
import type { McpAuthContext } from "../auth.js";
import type { McpToolInvocationStatus } from "../tool-status.js";

const uuid = z.string().uuid();
const status = z.enum(["Pending", "Completed", "All"]).default("Pending");
const householdSelectionSchema = z.object({ householdId: uuid.optional() }).strict();
const listItemsSchema = z.object({
  listId: uuid,
  status,
  limit: z.number().int().min(1).max(100).default(50),
}).strict();
const activitySchema = z.object({
  householdId: uuid.optional(),
  limit: z.number().int().min(1).max(50).default(20),
}).strict();
const listsSchema = z.object({
  householdId: uuid.optional(),
  includeArchived: z.boolean().default(false),
  limit: z.number().int().min(1).max(100).default(50),
}).strict();
const idempotencyKey = z.string().trim().min(8).max(128);
const createShoppingItemSchema = z.object({
  listId: uuid,
  title: z.string().trim().min(1).max(200),
  quantity: z.number().int().min(1).max(999).optional(),
  unit: z.string().trim().min(1).max(50).optional(),
  note: z.string().trim().max(500).optional(),
  idempotencyKey,
}).strict();
const shoppingItemMutationSchema = z.object({
  listId: uuid,
  itemId: uuid,
  idempotencyKey,
}).strict();
const createTaskSchema = z.object({
  listId: uuid,
  title: z.string().trim().min(1).max(200),
  note: z.string().trim().max(500).optional(),
  idempotencyKey,
}).strict();
const taskMutationSchema = z.object({
  listId: uuid,
  taskId: uuid,
  idempotencyKey,
}).strict();

const toolOutputSchema = z.object({
  data: z.unknown(),
  meta: z.object({
    source: z.literal("convy_api"),
    householdId: uuid.nullable().optional(),
    truncated: z.boolean().optional(),
    selectionRequired: z.boolean().optional(),
  }).strict(),
}).strict();

const readOnlyAnnotations = {
  readOnlyHint: true,
  destructiveHint: false,
  idempotentHint: true,
  openWorldHint: false,
} as const;

const writeAnnotations = {
  readOnlyHint: false,
  destructiveHint: false,
  idempotentHint: true,
  openWorldHint: false,
} as const;

export type ToolExecutionContext = {
  apiClient: ConvyApiClient;
  auth: McpAuthContext;
};

export type ToolResult = z.infer<typeof toolOutputSchema>;

export type ToolDefinition<TInput extends z.ZodTypeAny = z.ZodTypeAny> = {
  name: string;
  title: string;
  description: string;
  inputSchema: TInput;
  outputSchema: typeof toolOutputSchema;
  annotations: typeof readOnlyAnnotations | typeof writeAnnotations;
  requiredScopes: SupportedScope[];
  execute: (args: z.infer<TInput>, context: ToolExecutionContext) => Promise<ToolResult>;
};

const writeToolDefinitions = [
  defineTool({
    name: "convy_create_shopping_item",
    title: "Create Shopping Item",
    description: "Creates a shopping item in a Convy shopping list. Requires an idempotency key.",
    inputSchema: createShoppingItemSchema,
    outputSchema: toolOutputSchema,
    annotations: writeAnnotations,
    requiredScopes: [writeScopes[0]],
    execute: async (args, context) => {
      const created = await context.apiClient.createShoppingItem(context.auth.token, args.listId, {
        title: args.title,
        quantity: args.quantity ?? null,
        unit: args.unit ?? null,
        note: args.note ?? null,
        idempotencyKey: args.idempotencyKey,
      });

      return result({
        item: {
          ...pick(created, ["id"]),
          listId: args.listId,
          title: args.title,
          quantity: args.quantity ?? null,
          unit: args.unit ?? null,
          note: args.note ?? null,
        },
      });
    },
  }),
  defineTool({
    name: "convy_complete_shopping_item",
    title: "Complete Shopping Item",
    description: "Marks a Convy shopping item as completed. Requires an idempotency key.",
    inputSchema: shoppingItemMutationSchema,
    outputSchema: toolOutputSchema,
    annotations: writeAnnotations,
    requiredScopes: [writeScopes[0]],
    execute: async (args, context) => {
      await context.apiClient.completeShoppingItem(context.auth.token, args.listId, args.itemId, args.idempotencyKey);
      return result({ listId: args.listId, itemId: args.itemId, isCompleted: true });
    },
  }),
  defineTool({
    name: "convy_uncomplete_shopping_item",
    title: "Uncomplete Shopping Item",
    description: "Marks a Convy shopping item as pending again. Requires an idempotency key.",
    inputSchema: shoppingItemMutationSchema,
    outputSchema: toolOutputSchema,
    annotations: writeAnnotations,
    requiredScopes: [writeScopes[0]],
    execute: async (args, context) => {
      await context.apiClient.uncompleteShoppingItem(context.auth.token, args.listId, args.itemId, args.idempotencyKey);
      return result({ listId: args.listId, itemId: args.itemId, isCompleted: false });
    },
  }),
  defineTool({
    name: "convy_create_task",
    title: "Create Task",
    description: "Creates a task in a Convy task list. Requires an idempotency key.",
    inputSchema: createTaskSchema,
    outputSchema: toolOutputSchema,
    annotations: writeAnnotations,
    requiredScopes: [writeScopes[1]],
    execute: async (args, context) => {
      const created = await context.apiClient.createTask(context.auth.token, args.listId, {
        title: args.title,
        note: args.note ?? null,
        idempotencyKey: args.idempotencyKey,
      });

      return result({
        task: {
          ...pick(created, ["id"]),
          listId: args.listId,
          title: args.title,
          note: args.note ?? null,
        },
      });
    },
  }),
  defineTool({
    name: "convy_complete_task",
    title: "Complete Task",
    description: "Marks a Convy task as completed. Requires an idempotency key.",
    inputSchema: taskMutationSchema,
    outputSchema: toolOutputSchema,
    annotations: writeAnnotations,
    requiredScopes: [writeScopes[1]],
    execute: async (args, context) => {
      await context.apiClient.completeTask(context.auth.token, args.listId, args.taskId, args.idempotencyKey);
      return result({ listId: args.listId, taskId: args.taskId, isCompleted: true });
    },
  }),
  defineTool({
    name: "convy_uncomplete_task",
    title: "Uncomplete Task",
    description: "Marks a Convy task as pending again. Requires an idempotency key.",
    inputSchema: taskMutationSchema,
    outputSchema: toolOutputSchema,
    annotations: writeAnnotations,
    requiredScopes: [writeScopes[1]],
    execute: async (args, context) => {
      await context.apiClient.uncompleteTask(context.auth.token, args.listId, args.taskId, args.idempotencyKey);
      return result({ listId: args.listId, taskId: args.taskId, isCompleted: false });
    },
  }),
] as const;

export function ensureToolScopes(definition: ToolDefinition, auth: McpAuthContext) {
  const missingScopes = definition.requiredScopes.filter((scope) => !auth.scopes.has(scope));
  if (missingScopes.length > 0) {
    throw new ConvyApiError(403, "Forbidden", `Missing required Convy MCP scope: ${missingScopes.join(", ")}.`);
  }
}

const readOnlyToolDefinitions = [
  defineTool({
    name: "convy_get_context",
    title: "Get Convy Context",
    description: "Returns the households visible to the connected Convy user.",
    inputSchema: z.object({}).strict(),
    outputSchema: toolOutputSchema,
    annotations: readOnlyAnnotations,
    requiredScopes: [readOnlyScopes[0]],
    execute: async (_args, context) => {
      const households = await context.apiClient.getHouseholds(context.auth.token);
      const compactHouseholds = compactHouseholdsForSelection(households);

      return result({
        householdCount: compactHouseholds.length,
        defaultHouseholdId: compactHouseholds.length === 1 ? compactHouseholds[0]?.id : null,
        selectionRequired: compactHouseholds.length > 1,
        households: compactHouseholds,
      }, {
        selectionRequired: compactHouseholds.length > 1,
      });
    },
  }),
  defineTool({
    name: "convy_get_household_overview",
    title: "Get Household Overview",
    description: "Returns a concise read-only household overview, members, list counts, and latest activity.",
    inputSchema: householdSelectionSchema,
    outputSchema: toolOutputSchema,
    annotations: readOnlyAnnotations,
    requiredScopes: [readOnlyScopes[0], readOnlyScopes[1], readOnlyScopes[4]],
    execute: async (args, context) => {
      const selection = await resolveHouseholdSelection(context, args.householdId);
      if (selection.selectionRequired) {
        return selectionRequiredResult(selection.households);
      }

      const householdId = selection.householdId;
      const [household, lists, activity] = await Promise.all([
        context.apiClient.getHousehold(context.auth.token, householdId),
        context.apiClient.getLists(context.auth.token, householdId, false),
        context.apiClient.getRecentActivity(context.auth.token, householdId, 5),
      ]);
      const compactLists = compactListsForResponse(lists, 100);
      const activeLists = compactLists.items.filter((list) => !list.isArchived);

      return result({
        household: compactHouseholdDetail(household),
        counts: {
          lists: compactLists.items.length,
          shoppingLists: activeLists.filter((list) => list.type === "Shopping").length,
          taskLists: activeLists.filter((list) => list.type === "Tasks").length,
        },
        latestActivity: compactActivityForResponse(activity, 5).items,
      }, {
        householdId,
      });
    },
  }),
  defineTool({
    name: "convy_get_lists",
    title: "Get Lists",
    description: "Returns read-only Convy shopping and task lists for a household.",
    inputSchema: listsSchema,
    outputSchema: toolOutputSchema,
    annotations: readOnlyAnnotations,
    requiredScopes: [readOnlyScopes[0], readOnlyScopes[1]],
    execute: async (args, context) => {
      const selection = await resolveHouseholdSelection(context, args.householdId);
      if (selection.selectionRequired) {
        return selectionRequiredResult(selection.households);
      }

      const lists = await context.apiClient.getLists(context.auth.token, selection.householdId, args.includeArchived);
      const compact = compactListsForResponse(lists, args.limit);

      return result({ lists: compact.items }, {
        householdId: selection.householdId,
        truncated: compact.truncated,
      });
    },
  }),
  defineTool({
    name: "convy_get_shopping_items",
    title: "Get Shopping Items",
    description: "Returns read-only shopping items for a Convy shopping list.",
    inputSchema: listItemsSchema,
    outputSchema: toolOutputSchema,
    annotations: readOnlyAnnotations,
    requiredScopes: [readOnlyScopes[2]],
    execute: async (args, context) => {
      const items = await context.apiClient.getShoppingItems(
        context.auth.token,
        args.listId,
        args.status === "All" ? undefined : args.status,
      );
      const compact = compactItemsForResponse(items, args.limit);

      return result({ items: compact.items }, { truncated: compact.truncated });
    },
  }),
  defineTool({
    name: "convy_get_tasks",
    title: "Get Tasks",
    description: "Returns read-only task items for a Convy task list.",
    inputSchema: listItemsSchema,
    outputSchema: toolOutputSchema,
    annotations: readOnlyAnnotations,
    requiredScopes: [readOnlyScopes[3]],
    execute: async (args, context) => {
      const tasks = await context.apiClient.getTasks(
        context.auth.token,
        args.listId,
        args.status === "All" ? undefined : args.status,
      );
      const compact = compactTasksForResponse(tasks, args.limit);

      return result({ tasks: compact.items }, { truncated: compact.truncated });
    },
  }),
  defineTool({
    name: "convy_get_recent_activity",
    title: "Get Recent Activity",
    description: "Returns recent read-only activity for a Convy household.",
    inputSchema: activitySchema,
    outputSchema: toolOutputSchema,
    annotations: readOnlyAnnotations,
    requiredScopes: [readOnlyScopes[0], readOnlyScopes[4]],
    execute: async (args, context) => {
      const selection = await resolveHouseholdSelection(context, args.householdId);
      if (selection.selectionRequired) {
        return selectionRequiredResult(selection.households);
      }

      const activity = await context.apiClient.getRecentActivity(context.auth.token, selection.householdId, args.limit);
      const compact = compactActivityForResponse(activity, args.limit);

      return result({ activity: compact.items }, {
        householdId: selection.householdId,
        truncated: compact.truncated,
      });
    },
  }),
] as const;

export const toolDefinitions = [
  ...readOnlyToolDefinitions,
  ...writeToolDefinitions,
] as const;

export function statusForError(error: unknown): McpToolInvocationStatus {
  if (error instanceof ConvyApiError) {
    return error.errorType as McpToolInvocationStatus;
  }

  if (error instanceof z.ZodError) {
    return "ValidationError";
  }

  return "UnexpectedError";
}

function result(
  data: unknown,
  meta?: Omit<ToolResult["meta"], "source">,
): ToolResult {
  return {
    data,
    meta: {
      source: "convy_api",
      ...meta,
    },
  };
}

function defineTool<TInput extends z.ZodTypeAny>(definition: ToolDefinition<TInput>) {
  return definition;
}

async function resolveHouseholdSelection(context: ToolExecutionContext, requestedHouseholdId?: string) {
  if (requestedHouseholdId) {
    return { householdId: requestedHouseholdId, selectionRequired: false as const };
  }

  const households = compactHouseholdsForSelection(await context.apiClient.getHouseholds(context.auth.token));
  if (households.length === 1) {
    return { householdId: households[0]!.id, selectionRequired: false as const };
  }

  return { households, selectionRequired: true as const };
}

function selectionRequiredResult(households: Array<Record<string, unknown>>) {
  return result({
    selectionRequired: true,
    households,
  }, {
    selectionRequired: true,
    householdId: null,
  });
}

function compactHouseholdsForSelection(values: unknown[]) {
  return values
    .map((value) => pick(value, ["id", "name", "createdAt"]))
    .filter((value): value is { id: string; name?: unknown; createdAt?: unknown } => typeof value.id === "string");
}

function compactHouseholdDetail(value: Record<string, unknown>) {
  return {
    ...pick(value, ["id", "name", "createdAt"]),
    members: Array.isArray(value.members)
      ? value.members.map((member) => pick(member, ["userId", "displayName", "role", "joinedAt"]))
      : [],
  };
}

function compactListsForResponse(values: unknown[], limit: number) {
  return compact(values, limit, ["id", "name", "type", "householdId", "createdAt", "isArchived", "archivedAt"]);
}

function compactItemsForResponse(values: unknown[], limit: number) {
  return compact(values, limit, [
    "id",
    "title",
    "quantity",
    "unit",
    "note",
    "listId",
    "createdByName",
    "createdAt",
    "isCompleted",
    "completedByName",
    "completedAt",
    "recurrenceFrequency",
    "recurrenceInterval",
    "nextDueDate",
  ]);
}

function compactTasksForResponse(values: unknown[], limit: number) {
  return compact(values, limit, [
    "id",
    "title",
    "note",
    "listId",
    "createdByName",
    "createdAt",
    "isCompleted",
    "completedByName",
    "completedAt",
  ]);
}

function compactActivityForResponse(values: unknown[], limit: number) {
  return compact(values, limit, [
    "id",
    "householdId",
    "entityType",
    "entityId",
    "actionType",
    "performedByName",
    "createdAt",
    "metadata",
  ]);
}

function compact(values: unknown[], limit: number, keys: string[]) {
  const items = values.slice(0, limit).map((value) => pick(value, keys));

  return {
    items,
    truncated: values.length > items.length,
  };
}

function pick(value: unknown, keys: string[]) {
  if (typeof value !== "object" || value === null) {
    return {};
  }

  const record = value as Record<string, unknown>;
  return Object.fromEntries(keys.filter((key) => key in record).map((key) => [key, record[key]]));
}
