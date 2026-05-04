# skill-supabase.md — Backward Compatible Loader

> **NOTE:** This skill has been modularized. Load the new structure:

## New Modular Structure

Load `/skills/supabase/SKILL.md` as the main entry point. It references these sub-skills:

| Sub-skill | Content |
|---|---|
| `skills/supabase/SKILL.md` | Core principles, directory structure, naming conventions, CLI commands, common errors |
| `skills/supabase/migrations-core.md` | Master migration template + core system tables (clinicas, perfiles, usuarios, permisos) |
| `skills/supabase/migrations-business.md` | Business tables (pacientes, citas, expedientes + hojas de cita) |
| `skills/supabase/storage.md` | Supabase Storage buckets, policies, file paths |
| `skills/supabase/realtime.md` | Realtime pub/sub, alertas_espera table, Edge Functions |

## Quick Load by Task

- **Create a new table migration:** → `skills/supabase/SKILL.md` then `skills/supabase/migrations-core.md`
- **Create business tables:** → `skills/supabase/migrations-business.md`
- **Configure Storage:** → `skills/supabase/storage.md`
- **Setup Realtime/Alerts:** → `skills/supabase/realtime.md`

---

*Legacy loader — redirects to /skills/supabase/SKILL.md*
