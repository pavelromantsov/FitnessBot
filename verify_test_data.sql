-- =====================================================
-- Скрипт для проверки тестовых данных
-- =====================================================

SELECT '============================================' as section;
SELECT '=== ТЕСТОВЫЕ ПОЛЬЗОВАТЕЛИ ===' as section;
SELECT '============================================' as section;

SELECT 
    id,
    telegram_id,
    name,
    role,
    age,
    city,
    ROUND((weightkg / ((heightcm / 100.0) * (heightcm / 100.0)))::numeric, 2) as bmi,
    breakfast_time,
    lunch_time,
    dinner_time,
    created_at
FROM users 
WHERE telegram_id IN (111111111, 222222222, 333333333)
ORDER BY telegram_id;

SELECT '============================================' as section;
SELECT '=== СТАТИСТИКА ПИТАНИЯ ===' as section;
SELECT '============================================' as section;

SELECT 
    u.name as user_name,
    m.meal_type,
    COUNT(*) as count,
    ROUND(AVG(m.calories)::numeric, 0) as avg_calories,
    ROUND(AVG(m.protein)::numeric, 0) as avg_protein,
    ROUND(AVG(m.fat)::numeric, 0) as avg_fat,
    ROUND(AVG(m.carbs)::numeric, 0) as avg_carbs
FROM meals m
JOIN users u ON m.user_id = u.id
WHERE u.telegram_id IN (111111111, 222222222, 333333333)
GROUP BY u.id, u.name, m.meal_type
ORDER BY u.id, m.meal_type;

SELECT '============================================' as section;
SELECT '=== СТАТИСТИКА АКТИВНОСТИ ===' as section;
SELECT '============================================' as section;

SELECT 
    u.name as user_name,
    COUNT(*) as activities_count,
    SUM(a.steps) as total_steps,
    ROUND(AVG(a.steps)::numeric, 0) as avg_steps,
    SUM(a.active_minutes) as total_minutes,
    SUM(a.calories_burned) as total_calories_burned
FROM activities a
JOIN users u ON a.user_id = u.id
WHERE u.telegram_id IN (111111111, 222222222, 333333333)
GROUP BY u.id, u.name
ORDER BY u.id;

SELECT '============================================' as section;
SELECT '=== ПИТАНИЕ ЗА ПОСЛЕДНИЕ 7 ДНЕЙ ===' as section;
SELECT '============================================' as section;

SELECT 
    u.name as user,
    DATE(m.date_time) as date,
    COUNT(*) as meals_count,
    SUM(m.calories) as total_calories,
    SUM(m.protein) as total_protein
FROM meals m
JOIN users u ON m.user_id = u.id
WHERE u.telegram_id IN (111111111, 222222222, 333333333)
AND m.date_time >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY u.id, u.name, DATE(m.date_time)
ORDER BY u.id, DATE(m.date_time) DESC;

SELECT '============================================' as section;
SELECT '=== АКТИВНОСТЬ ЗА ПОСЛЕДНИЕ 7 ДНЕЙ ===' as section;
SELECT '============================================' as section;

SELECT 
    u.name as user,
    a.date,
    SUM(a.steps) as total_steps,
    SUM(a.active_minutes) as total_minutes,
    SUM(a.calories_burned) as total_calories_burned
FROM activities a
JOIN users u ON a.user_id = u.id
WHERE u.telegram_id IN (111111111, 222222222, 333333333)
AND a.date >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY u.id, u.name, a.date
ORDER BY u.id, a.date DESC;
