# Supabase Realtime & Edge Functions

> **Agente propietario:** @IngenieroDatos
> **Cuándo cargar:** Para configurar módulos en tiempo real (Cola de Espera, Alertas).
> **Prerequisito:** skills/supabase/SKILL.md

---

## Habilitar Realtime en Tablas

```sql
-- Habilitar publicación realtime en tablas requeridas
ALTER PUBLICATION supabase_realtime ADD TABLE citas;
ALTER PUBLICATION supabase_realtime ADD TABLE alertas_espera;
```

---

## Tabla: alertas_espera (HU23)

```sql
-- Migración: create_alertas_espera | HU23
CREATE TABLE IF NOT EXISTS alertas_espera (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    clinica_id      UUID NOT NULL REFERENCES clinicas(id) ON DELETE RESTRICT,
    cita_id         UUID NOT NULL REFERENCES citas(id) ON DELETE RESTRICT,
    paciente_id     UUID NOT NULL REFERENCES pacientes(id) ON DELETE RESTRICT,
    doctor_id       UUID NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    sala_id         UUID REFERENCES salas(id),
    hora_cita       TIME NOT NULL,
    hora_llegada    TIME,
    minutos_espera  INTEGER NOT NULL,
    resuelta        BOOLEAN NOT NULL DEFAULT false,
    fecha_alerta    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_resolucion TIMESTAMPTZ
);

COMMENT ON TABLE alertas_espera IS 'Alertas cuando un paciente excede el tiempo de espera configurado';

ALTER TABLE alertas_espera ENABLE ROW LEVEL SECURITY;

CREATE POLICY "clinica_isolation_alertas" ON alertas_espera
    FOR ALL
    USING (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID)
    WITH CHECK (clinica_id = NULLIF(current_setting('app.current_clinica_id', true), '')::UUID);

CREATE POLICY "service_role_full_alertas" ON alertas_espera
    FOR ALL TO service_role USING (true) WITH CHECK (true);

GRANT SELECT, INSERT, UPDATE ON alertas_espera TO authenticated;

ALTER PUBLICATION supabase_realtime ADD TABLE alertas_espera;
```

---

## Edge Function: verificar-alertas-espera

```typescript
// supabase/functions/verificar-alertas-espera/index.ts
// Se ejecuta cada minuto via scheduled task o llamada desde el API

import { createClient } from 'https://esm.sh/@supabase/supabase-js@2'

Deno.serve(async (_req) => {
  const supabase = createClient(
    Deno.env.get('SUPABASE_URL')!,
    Deno.env.get('SUPABASE_SERVICE_ROLE_KEY')!
  )

  // Obtener todas las clínicas activas con su tiempo de espera
  const { data: clinicas } = await supabase
    .from('clinicas')
    .select('id, tiempo_espera_minutos')
    .eq('activo', true)

  for (const clinica of clinicas ?? []) {
    // Buscar citas en espera que superan el tiempo configurado
    const { data: citasExcedidas } = await supabase
      .from('citas')
      .select(`
        id, paciente_id, doctor_id, sala_id,
        hora_cita, hora_llegada,
        pacientes(primer_nombre, primer_apellido)
      `)
      .eq('clinica_id', clinica.id)
      .eq('fecha_cita', new Date().toISOString().split('T')[0])
      .in('estado', ['en_espera', 'agendada'])
      .not('hora_llegada', 'is', null)

    for (const cita of citasExcedidas ?? []) {
      const llegada = new Date(`1970-01-01T${cita.hora_llegada}`)
      const ahora = new Date()
      const minutosEspera = Math.floor((ahora.getTime() - llegada.getTime()) / 60000)

      if (minutosEspera >= clinica.tiempo_espera_minutos) {
        await supabase.from('alertas_espera').upsert({
          clinica_id: clinica.id,
          cita_id: cita.id,
          paciente_id: cita.paciente_id,
          doctor_id: cita.doctor_id,
          sala_id: cita.sala_id,
          hora_cita: cita.hora_cita,
          hora_llegada: cita.hora_llegada,
          minutos_espera: minutosEspera,
          resuelta: false
        }, { onConflict: 'cita_id' })
      }
    }
  }

  return new Response(JSON.stringify({ ok: true }), {
    headers: { 'Content-Type': 'application/json' }
  })
})
```

---

## Checklist de Calidad — Realtime

### Tablas
- [ ] Tabla `alertas_espera` creada con campos completos
- [ ] RLS habilitado con política `clinica_isolation_alertas`
- [ ] Tabla agregada a `supabase_realtime` publication
- [ ] Tabla `citas` agregada a `supabase_realtime` publication

### Edge Function
- [ ] Función usa `service_role` key para bypass RLS
- [ ] Verifica todas las clínicas activas
- [ ] Usa `upsert` con `onConflict: 'cita_id'` para evitar duplicados
- [ ] Calcula minutos de espera correctamente
- [ ] Respuesta JSON con `{ ok: true }`

### Frontend (validación)
- [ ] Supabase JS Client configurado con URL y AnonKey
- [ ] Canal de suscripción con filtro por `clinica_id`
- [ ] Fallback de polling cada 60 segundos
- [ ] Notificación de sonido en nueva alerta

---

*skills/supabase/realtime.md — Vittal v1.0.0*
