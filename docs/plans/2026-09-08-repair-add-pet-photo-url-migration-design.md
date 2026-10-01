# Repair AddPetPhotoUrl Migration

## Problem

The application model reads PETS.PHOTO_URL, but the active VET_APP schema
does not expose that column. The existing AddPetPhotoUrl migration has no EF
migration metadata and is therefore absent from the migrations list. The
fallback SQL patch targets the obsolete TOMDEVV schema instead of the schema
used by the application.

## Design

Keep the existing migration identifier so environments that already recorded
it remain compatible. Add explicit EF DbContext and Migration metadata, and
make the migration idempotent with Oracle PL/SQL that checks the current
schema before adding or dropping the column.

Change the manual patch to operate on the connected user's schema through
USER_TAB_COLUMNS and unqualified application table names. It must validate
that the session is connected to FREEPDB1 and record migration history only
after the column exists.

Add a regression test that loads EF's migrations assembly and proves that
20260908150000_AddPetPhotoUrl is discoverable.

## Application and verification

Run the regression test red/green, build the solution, apply EF migrations to
the configured VET_APP@FREEPDB1 database, execute the idempotent production
seed runner, and verify the column, migration history, and canonical catalog
minimums.

No cleanup seed or test-data seed will be executed.
