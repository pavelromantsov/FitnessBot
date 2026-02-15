-- =====================================================
-- Скрипт для заполнения базы данных FitnessBot
-- тестовыми данными
-- =====================================================

-- Очистка существующих тестовых данных
DELETE FROM activities WHERE user_id IN (
    SELECT id FROM users WHERE telegram_id IN (111111111, 222222222, 333333333)
);

DELETE FROM meals WHERE user_id IN (
    SELECT id FROM users WHERE telegram_id IN (111111111, 222222222, 333333333)
);

DELETE FROM daily_goals WHERE user_id IN (
    SELECT id FROM users WHERE telegram_id IN (111111111, 222222222, 333333333)
);

DELETE FROM bmi_records WHERE user_id IN (
    SELECT id FROM users WHERE telegram_id IN (111111111, 222222222, 333333333)
);

DELETE FROM admins WHERE user_id IN (
    SELECT id FROM users WHERE telegram_id IN (111111111, 222222222, 333333333)
);

DELETE FROM users WHERE telegram_id IN (111111111, 222222222, 333333333);

-- =====================================================
-- 1. СОЗДАНИЕ ТЕСТОВЫХ ПОЛЬЗОВАТЕЛЕЙ
-- =====================================================

INSERT INTO users (
    telegram_id, 
    name, 
    role, 
    age, 
    city, 
    heightcm, 
    weightkg,
    breakfast_time,
    lunch_time,
    dinner_time,
    activity_reminders_enabled,
    created_at
)
VALUES 
    (
        111111111, 
        'Иван Петров', 
        'User', 
        28, 
        'Москва', 
        180, 
        80,
        '08:00:00',
        '13:00:00',
        '19:00:00',
        true,
        NOW() - INTERVAL '30 days'
    ),
    (
        222222222, 
        'Мария Сидорова', 
        'User', 
        25, 
        'Санкт-Петербург', 
        165, 
        58,
        '09:00:00',
        '14:00:00',
        '20:00:00',
        true,
        NOW() - INTERVAL '25 days'
    ),
    (
        333333333, 
        'Алексей Смирнов', 
        'Admin', 
        35, 
        'Ростов-на-Дону', 
        175, 
        90,
        '07:30:00',
        '13:30:00',
        '18:30:00',
        true,
        NOW() - INTERVAL '20 days'
    );

-- =====================================================
-- 2. СОЗДАНИЕ АДМИНИСТРАТОРА
-- =====================================================

INSERT INTO admins (user_id, notes)
SELECT id, 'Тестовый администратор'
FROM users 
WHERE telegram_id = 333333333;

-- =====================================================
-- 3. СОЗДАНИЕ ЗАПИСЕЙ BMI
-- =====================================================

INSERT INTO bmi_records (user_id, height_cm, weight_kg, bmi, category, recommendation, measured_at)
SELECT 
    u.id,
    u.heightcm,
    u.weightkg,
    ROUND((u.weightkg / ((u.heightcm / 100.0) * (u.heightcm / 100.0)))::numeric, 2) as bmi,
    CASE 
        WHEN (u.weightkg / ((u.heightcm / 100.0) * (u.heightcm / 100.0))) < 18.5 THEN 'Недостаточный вес'
        WHEN (u.weightkg / ((u.heightcm / 100.0) * (u.heightcm / 100.0))) < 25 THEN 'Нормальный вес'
        WHEN (u.weightkg / ((u.heightcm / 100.0) * (u.heightcm / 100.0))) < 30 THEN 'Избыточный вес'
        ELSE 'Ожирение'
    END as category,
    'Регулярно отслеживайте свой вес и занимайтесь физической активностью' as recommendation,
    NOW() - INTERVAL '15 days'
FROM users u
WHERE u.telegram_id IN (111111111, 222222222, 333333333);

-- =====================================================
-- 4. СОЗДАНИЕ ЕЖЕДНЕВНЫХ ЦЕЛЕЙ (последние 14 дней)
-- =====================================================

INSERT INTO daily_goals (user_id, date, target_steps, target_calories_in, target_calories_out, is_completed)
SELECT 
    u.id,
    CURRENT_DATE - d.day,
    CASE 
        WHEN u.telegram_id = 111111111 THEN 10000
        WHEN u.telegram_id = 222222222 THEN 12000
        WHEN u.telegram_id = 333333333 THEN 8000
    END as target_steps,
    CASE 
        WHEN u.telegram_id = 111111111 THEN 2000
        WHEN u.telegram_id = 222222222 THEN 1800
        WHEN u.telegram_id = 333333333 THEN 2500
    END as target_calories_in,
    CASE 
        WHEN u.telegram_id = 111111111 THEN 500
        WHEN u.telegram_id = 222222222 THEN 600
        WHEN u.telegram_id = 333333333 THEN 400
    END as target_calories_out,
    random() > 0.3 as is_completed
FROM users u
CROSS JOIN generate_series(0, 13) as d(day)
WHERE u.telegram_id IN (111111111, 222222222, 333333333);

-- =====================================================
-- 5. СОЗДАНИЕ ЗАПИСЕЙ О ПИТАНИИ (последние 14 дней)
-- =====================================================

-- Завтраки
INSERT INTO meals (user_id, date_time, meal_type, calories, protein, fat, carbs)
SELECT 
    u.id,
    (CURRENT_DATE - d.day * INTERVAL '1 day' + u.breakfast_time::interval + (random() * INTERVAL '1 hour')) as date_time,
    'breakfast' as meal_type,
    (300 + floor(random() * 200))::int as calories,
    (15 + floor(random() * 15))::int as protein,
    (8 + floor(random() * 10))::int as fat,
    (40 + floor(random() * 30))::int as carbs
FROM users u
CROSS JOIN generate_series(0, 13) as d(day)
WHERE u.telegram_id IN (111111111, 222222222, 333333333);

-- Обеды
INSERT INTO meals (user_id, date_time, meal_type, calories, protein, fat, carbs)
SELECT 
    u.id,
    (CURRENT_DATE - d.day * INTERVAL '1 day' + u.lunch_time::interval + (random() * INTERVAL '1 hour')) as date_time,
    'lunch' as meal_type,
    (400 + floor(random() * 300))::int as calories,
    (25 + floor(random() * 25))::int as protein,
    (12 + floor(random() * 15))::int as fat,
    (50 + floor(random() * 40))::int as carbs
FROM users u
CROSS JOIN generate_series(0, 13) as d(day)
WHERE u.telegram_id IN (111111111, 222222222, 333333333);

-- Ужины
INSERT INTO meals (user_id, date_time, meal_type, calories, protein, fat, carbs)
SELECT 
    u.id,
    (CURRENT_DATE - d.day * INTERVAL '1 day' + u.dinner_time::interval + (random() * INTERVAL '1 hour')) as date_time,
    'dinner' as meal_type,
    (350 + floor(random() * 250))::int as calories,
    (20 + floor(random() * 20))::int as protein,
    (10 + floor(random() * 12))::int as fat,
    (35 + floor(random() * 35))::int as carbs
FROM users u
CROSS JOIN generate_series(0, 13) as d(day)
WHERE u.telegram_id IN (111111111, 222222222, 333333333);

-- Перекусы (не каждый день)
INSERT INTO meals (user_id, date_time, meal_type, calories, protein, fat, carbs)
SELECT 
    u.id,
    (CURRENT_DATE - d.day * INTERVAL '1 day' + INTERVAL '16 hours' + (random() * 2) * INTERVAL '1 hour') as date_time,
    'snack' as meal_type,
    (100 + floor(random() * 150))::int as calories,
    (5 + floor(random() * 10))::int as protein,
    (3 + floor(random() * 8))::int as fat,
    (15 + floor(random() * 20))::int as carbs
FROM users u
CROSS JOIN generate_series(0, 13) as d(day)
WHERE u.telegram_id IN (111111111, 222222222, 333333333)
AND random() > 0.3; -- 70% дней с перекусом

-- =====================================================
-- 6. СОЗДАНИЕ ЗАПИСЕЙ ОБ АКТИВНОСТИ (последние 14 дней)
-- =====================================================

-- Утренние активности
INSERT INTO activities (user_id, date, steps, active_minutes, calories_burned, source)
SELECT 
    u.id,
    (CURRENT_DATE - d.day)::date as date,
    (5000 + floor(random() * 8000))::int as steps,
    (30 + floor(random() * 30))::int as active_minutes,
    (200 + floor(random() * 300))::int as calories_burned,
    'manual' as source
FROM users u
CROSS JOIN generate_series(0, 13) as d(day)
WHERE u.telegram_id IN (111111111, 222222222, 333333333)
AND random() > 0.2; -- 80% дней с активностью

-- Вечерние активности
INSERT INTO activities (user_id, date, steps, active_minutes, calories_burned, source)
SELECT 
    u.id,
    (CURRENT_DATE - d.day)::date as date,
    (3000 + floor(random() * 5000))::int as steps,
    (45 + floor(random() * 45))::int as active_minutes,
    (250 + floor(random() * 350))::int as calories_burned,
    'manual' as source
FROM users u
CROSS JOIN generate_series(0, 13) as d(day)
WHERE u.telegram_id IN (111111111, 222222222, 333333333)
AND random() > 0.4; -- 60% дней с вечерней активностью

-- =====================================================
-- ВЫВОД СТАТИСТИКИ
-- =====================================================

SELECT '============================================' as info;
SELECT '=== СТАТИСТИКА ТЕСТОВЫХ ДАННЫХ ===' as info;
SELECT '============================================' as info;

SELECT 
    'Создано пользователей: ' || COUNT(*) as result
FROM users 
WHERE telegram_id IN (111111111, 222222222, 333333333);

SELECT 
    'Создано администраторов: ' || COUNT(*) as result
FROM admins a
JOIN users u ON a.user_id = u.id
WHERE u.telegram_id IN (111111111, 222222222, 333333333);

SELECT 
    'Создано BMI записей: ' || COUNT(*) as result
FROM bmi_records b
JOIN users u ON b.user_id = u.id
WHERE u.telegram_id IN (111111111, 222222222, 333333333);

SELECT 
    'Создано ежедневных целей: ' || COUNT(*) as result
FROM daily_goals dg
JOIN users u ON dg.user_id = u.id
WHERE u.telegram_id IN (111111111, 222222222, 333333333);

SELECT 
    'Создано записей о питании: ' || COUNT(*) as result
FROM meals m
JOIN users u ON m.user_id = u.id
WHERE u.telegram_id IN (111111111, 222222222, 333333333);

SELECT 
    'Создано записей об активности: ' || COUNT(*) as result
FROM activities a
JOIN users u ON a.user_id = u.id
WHERE u.telegram_id IN (111111111, 222222222, 333333333);

SELECT '============================================' as info;
SELECT '=== ДЕТАЛЬНАЯ СТАТИСТИКА ===' as info;
SELECT '============================================' as info;

-- Детальная статистика по пользователям
SELECT 
    u.name as "Пользователь",
    u.telegram_id as "Telegram ID",
    u.role as "Роль",
    u.age as "Возраст",
    u.city as "Город",
    ROUND((u.weightkg / ((u.heightcm / 100.0) * (u.heightcm / 100.0)))::numeric, 2) as "BMI",
    (SELECT COUNT(*) FROM meals m WHERE m.user_id = u.id) as "Приёмов пищи",
    (SELECT COUNT(*) FROM activities a WHERE a.user_id = u.id) as "Активностей",
    (SELECT COUNT(*) FROM daily_goals dg WHERE dg.user_id = u.id) as "Целей"
FROM users u
WHERE u.telegram_id IN (111111111, 222222222, 333333333)
ORDER BY u.telegram_id;
