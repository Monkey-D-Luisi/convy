SELECT 'activity_logs.household_id' AS reference, COUNT(*) AS orphan_count
FROM activity_logs child
LEFT JOIN households parent ON parent.id = child.household_id
WHERE parent.id IS NULL
UNION ALL
SELECT 'activity_logs.performed_by' AS reference, COUNT(*) AS orphan_count
FROM activity_logs child
LEFT JOIN users parent ON parent.id = child.performed_by
WHERE parent.id IS NULL
UNION ALL
SELECT 'device_tokens.user_id' AS reference, COUNT(*) AS orphan_count
FROM device_tokens child
LEFT JOIN users parent ON parent.id = child.user_id
WHERE parent.id IS NULL
UNION ALL
SELECT 'household_lists.created_by' AS reference, COUNT(*) AS orphan_count
FROM household_lists child
LEFT JOIN users parent ON parent.id = child.created_by
WHERE parent.id IS NULL
UNION ALL
SELECT 'household_lists.household_id' AS reference, COUNT(*) AS orphan_count
FROM household_lists child
LEFT JOIN households parent ON parent.id = child.household_id
WHERE parent.id IS NULL
UNION ALL
SELECT 'household_memberships.household_id' AS reference, COUNT(*) AS orphan_count
FROM household_memberships child
LEFT JOIN households parent ON parent.id = child.household_id
WHERE parent.id IS NULL
UNION ALL
SELECT 'household_memberships.user_id' AS reference, COUNT(*) AS orphan_count
FROM household_memberships child
LEFT JOIN users parent ON parent.id = child.user_id
WHERE parent.id IS NULL
UNION ALL
SELECT 'households.created_by' AS reference, COUNT(*) AS orphan_count
FROM households child
LEFT JOIN users parent ON parent.id = child.created_by
WHERE parent.id IS NULL
UNION ALL
SELECT 'invites.created_by' AS reference, COUNT(*) AS orphan_count
FROM invites child
LEFT JOIN users parent ON parent.id = child.created_by
WHERE parent.id IS NULL
UNION ALL
SELECT 'invites.household_id' AS reference, COUNT(*) AS orphan_count
FROM invites child
LEFT JOIN households parent ON parent.id = child.household_id
WHERE parent.id IS NULL
UNION ALL
SELECT 'invites.used_by' AS reference, COUNT(*) AS orphan_count
FROM invites child
LEFT JOIN users parent ON parent.id = child.used_by
WHERE child.used_by IS NOT NULL AND parent.id IS NULL
UNION ALL
SELECT 'list_items.completed_by' AS reference, COUNT(*) AS orphan_count
FROM list_items child
LEFT JOIN users parent ON parent.id = child.completed_by
WHERE child.completed_by IS NOT NULL AND parent.id IS NULL
UNION ALL
SELECT 'list_items.created_by' AS reference, COUNT(*) AS orphan_count
FROM list_items child
LEFT JOIN users parent ON parent.id = child.created_by
WHERE parent.id IS NULL
UNION ALL
SELECT 'list_items.list_id' AS reference, COUNT(*) AS orphan_count
FROM list_items child
LEFT JOIN household_lists parent ON parent.id = child.list_id
WHERE parent.id IS NULL
UNION ALL
SELECT 'list_items.returned_to_pending_by' AS reference, COUNT(*) AS orphan_count
FROM list_items child
LEFT JOIN users parent ON parent.id = child.returned_to_pending_by
WHERE child.returned_to_pending_by IS NOT NULL AND parent.id IS NULL
UNION ALL
SELECT 'notification_preferences.user_id' AS reference, COUNT(*) AS orphan_count
FROM notification_preferences child
LEFT JOIN users parent ON parent.id = child.user_id
WHERE parent.id IS NULL
UNION ALL
SELECT 'task_items.assigned_to_user_id' AS reference, COUNT(*) AS orphan_count
FROM task_items child
LEFT JOIN users parent ON parent.id = child.assigned_to_user_id
WHERE child.assigned_to_user_id IS NOT NULL AND parent.id IS NULL
UNION ALL
SELECT 'task_items.completed_by' AS reference, COUNT(*) AS orphan_count
FROM task_items child
LEFT JOIN users parent ON parent.id = child.completed_by
WHERE child.completed_by IS NOT NULL AND parent.id IS NULL
UNION ALL
SELECT 'task_items.created_by' AS reference, COUNT(*) AS orphan_count
FROM task_items child
LEFT JOIN users parent ON parent.id = child.created_by
WHERE parent.id IS NULL
UNION ALL
SELECT 'task_items.list_id' AS reference, COUNT(*) AS orphan_count
FROM task_items child
LEFT JOIN household_lists parent ON parent.id = child.list_id
WHERE parent.id IS NULL
ORDER BY reference;
