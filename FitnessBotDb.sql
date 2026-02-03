-- =============================================
-- FitnessBot Database Schema (PostgreSQL)
-- =============================================
-- Create Database
CREATE DATABASE "FitnessBot";

-- Connect to database
\connect "FitnessBot";

-- =============================================
-- Table: users
-- =============================================
create table if not exists users
(
    id               bigserial primary key,
    telegram_id      bigint       not null unique,
    name             text         not null,
    role             text         not null default 'User',
    age              integer,
    city             text,
    created_at       timestamptz  not null default now(),
    last_activity_at timestamptz  not null default now()
);

create index if not exists idx_users_last_activity
    on users (last_activity_at);

-- =============================================
-- Table: admins
-- =============================================
create table if not exists admins
(
    id       bigserial primary key,
    user_id  bigint      not null references users (id) on delete cascade,
    notes    text
);

-- =============================================
-- Table: activities
-- =============================================
create table if not exists activities
(
    id              bigserial primary key,
    user_id         bigint      not null references users (id) on delete cascade,
    date            date        not null,
    steps           integer     not null default 0,
    active_minutes  integer     not null default 0,
    calories_burned double precision not null default 0,
    source          text        not null default 'manual'
);

create index if not exists idx_activities_user_date
    on activities (user_id, date);

-- =============================================
-- Table: meals
-- =============================================
create table if not exists meals
(
    id        bigserial primary key,
    user_id   bigint      not null references users (id) on delete cascade,
    date_time timestamptz not null,
    meal_type text        not null, -- breakfast / lunch / dinner / snack
    calories  double precision not null default 0,
    protein   double precision not null default 0,
    fat       double precision not null default 0,
    carbs     double precision not null default 0,
    photo_url text
);

create index if not exists idx_meals_user_datetime
    on meals (user_id, date_time);

-- =============================================
-- Table: daily_goals
-- =============================================
create table if not exists daily_goals
(
    id                  bigserial primary key,
    user_id             bigint      not null references users (id) on delete cascade,
    date                date        not null,
    target_steps        integer     not null default 0,
    target_calories_in  double precision not null default 0,
    target_calories_out double precision not null default 0,
    is_completed        boolean     not null default false,
    completed_at        timestamptz
);

create unique index if not exists uix_daily_goals_user_date
    on daily_goals (user_id, date);

-- =============================================
-- Table: daily_goals
-- =============================================
create table if not exists bmi_records
(
    id             bigserial primary key,
    user_id        bigint      not null references users (id) on delete cascade,
    height_cm      double precision not null,
    weight_kg      double precision not null,
    bmi            double precision not null,
    category       text        not null,
    recommendation text        not null,
    measured_at    timestamptz not null default now()
);

create index if not exists idx_bmi_user_measured_at
    on bmi_records (user_id, measured_at desc);

-- =============================================
-- Table: error_logs
-- =============================================
create table if not exists error_logs
(
    id           bigserial primary key,
    timestamp    timestamptz not null default now(),
    level        text        not null,   -- Error / Warning / Critical
    message      text        not null,
    stack_trace  text,
    context_json text
);

-- =============================================
-- Table: change_logs
-- =============================================
create table if not exists change_logs
(
    id            bigserial primary key,
    admin_user_id bigint references users (id) on delete set null,
    timestamp     timestamptz not null default now(),
    change_type   text        not null,  -- например: ConfigUpdated, UserDeleted
    details       text        not null
);

-- =============================================
-- Table: content_items
-- =============================================
create table if not exists content_items
(
    id           bigserial primary key,
    user_id      bigint      not null references users (id) on delete cascade,
    content_type text        not null,   -- photo / report / text / ...
    size_bytes   bigint      not null,
    created_at   timestamptz not null default now(),
    external_url text
);

create index if not exists idx_content_items_user
    on content_items (user_id);


-- =============================================
-- Alter table: users
-- =============================================
    alter table users
    add column breakfast_time time,
    add column lunch_time time,
    add column dinner_time time;

-- =============================================
-- Table: notifications
-- =============================================
create table if not exists notifications
(
    id bigserial primary key,
    user_id bigint not null references users (id) on delete cascade,
    type text not null,               -- BreakfastReminder / LunchReminder / DinnerReminder
    text text not null,
    scheduled_at timestamptz not null,
    is_sent boolean not null default false,
    sent_at timestamptz
);

create index if not exists idx_notifications_user_scheduled
    on notifications (user_id, scheduled_at);

-- =============================================
-- Alter table: users (add registration)
-- =============================================

    alter table users
    add column if not exists heightcm double precision,
    add column if not exists weightkg double precision;