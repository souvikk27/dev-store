-- Verify seeded roles
SELECT 
    id,
    name,
    code,
    level,
    is_system,
    description,
    created_date
FROM roles
ORDER BY level DESC;

-- Count roles
SELECT COUNT(*) as total_roles FROM roles;

-- Get system roles only
SELECT name, code, level 
FROM roles 
WHERE is_system = true
ORDER BY level DESC;

-- Get non-system roles
SELECT name, code, level 
FROM roles 
WHERE is_system = false
ORDER BY level DESC;
