#!/usr/bin/env bash
# Runs once, automatically, the first time the tutorflow_postgres_data volume is initialized
# (docker-entrypoint-initdb.d scripts never re-run against an existing data directory). Creates
# a dedicated, non-superuser application role and one database per module that owns a schema —
# matching the connection string names each module's Add<Module>Module(config) already expects.
set -euo pipefail

: "${TUTORFLOW_APP_USER:?TUTORFLOW_APP_USER must be set}"
: "${TUTORFLOW_APP_PASSWORD:?TUTORFLOW_APP_PASSWORD must be set}"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres <<-EOSQL
    CREATE ROLE "${TUTORFLOW_APP_USER}" WITH LOGIN PASSWORD '${TUTORFLOW_APP_PASSWORD}';

    CREATE DATABASE tutorflow_identity OWNER "${TUTORFLOW_APP_USER}";
    CREATE DATABASE tutorflow_tutors OWNER "${TUTORFLOW_APP_USER}";
    CREATE DATABASE tutorflow_availability OWNER "${TUTORFLOW_APP_USER}";
    CREATE DATABASE tutorflow_bookings OWNER "${TUTORFLOW_APP_USER}";
    CREATE DATABASE tutorflow_search OWNER "${TUTORFLOW_APP_USER}";
    CREATE DATABASE tutorflow_subjects OWNER "${TUTORFLOW_APP_USER}";
    CREATE DATABASE tutorflow_students OWNER "${TUTORFLOW_APP_USER}";
    CREATE DATABASE tutorflow_payments OWNER "${TUTORFLOW_APP_USER}";
    CREATE DATABASE tutorflow_lessons OWNER "${TUTORFLOW_APP_USER}";
    CREATE DATABASE tutorflow_reviews OWNER "${TUTORFLOW_APP_USER}";
    CREATE DATABASE tutorflow_notifications OWNER "${TUTORFLOW_APP_USER}";
EOSQL

# tutorflow_app owns each database above, so it already has the privilege to install "trusted"
# extensions (btree_gist among them, since PostgreSQL 13) without being a superuser — the
# Bookings migration's own `CREATE EXTENSION IF NOT EXISTS btree_gist;` statement (ADR-0013 §1)
# runs as this same role in production use and needs no extra grant here.
