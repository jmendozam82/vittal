# Supabase Storage

> **Agente propietario:** @IngenieroDatos
> **Cuándo cargar:** Antes de configurar buckets de almacenamiento o políticas de archivos.
> **Prerequisito:** skills/supabase/SKILL.md

---

## Buckets Requeridos

```sql
-- Bucket para archivos de expedientes médicos (PRIVADO)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'expedientes',
    'expedientes',
    false,                          -- PRIVADO — acceso solo con token
    52428800,                       -- 50MB límite por archivo
    ARRAY[
        'application/pdf',
        'image/jpeg',
        'image/png',
        'image/webp',
        'application/msword',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
    ]
);

-- Bucket para fotos de pacientes y usuarios (PÚBLICO)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
    'avatares',
    'avatares',
    true,                           -- PÚBLICO — URL accesible directamente
    5242880,                        -- 5MB límite
    ARRAY['image/jpeg', 'image/png', 'image/webp']
);
```

---

## Políticas de Storage

```sql
-- Expedientes: lectura por usuarios de la misma clínica
CREATE POLICY "clinica_read_expedientes"
ON storage.objects FOR SELECT TO authenticated
USING (
    bucket_id = 'expedientes'
    AND (storage.foldername(name))[1] = (
        current_setting('app.current_clinica_id', true)
    )
);

-- Expedientes: escritura por usuarios de la misma clínica
CREATE POLICY "clinica_insert_expedientes"
ON storage.objects FOR INSERT TO authenticated
WITH CHECK (
    bucket_id = 'expedientes'
    AND (storage.foldername(name))[1] = (
        current_setting('app.current_clinica_id', true)
    )
);

-- Avatares: lectura pública
CREATE POLICY "public_read_avatares"
ON storage.objects FOR SELECT TO public
USING (bucket_id = 'avatares');

-- Avatares: escritura para autenticados
CREATE POLICY "authenticated_insert_avatares"
ON storage.objects FOR INSERT TO authenticated
WITH CHECK (bucket_id = 'avatares');
```

---

## Ruta de Almacenamiento Estándar

```
expedientes/
└── {clinica_id}/
    └── {paciente_id}/
        ├── {uuid}-resultado-exam.pdf
        ├── {uuid}-imagen-ojo.jpg
        └── {uuid}-epicrisis.pdf

avatares/
└── pacientes/
    └── {paciente_id}.jpg
```

---

## Integración desde el API (C#)

```csharp
// Ejemplo de subida a Supabase Storage desde el API
public async Task<string> UploadExpedienteFileAsync(
    IFormFile file, Guid pacienteId, Guid clinicaId)
{
    var bucketName = "expedientes";
    var path = $"{clinicaId}/{pacienteId}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

    using var stream = file.OpenReadStream();
    var response = await _supabaseClient.Storage
        .From(bucketName)
        .Upload(stream, path, new FileOptions { ContentType = file.ContentType });

    return _supabaseClient.Storage.From(bucketName).GetPublicUrl(path);
}
```

---

## Checklist de Calidad — Storage

### Buckets
- [ ] Bucket `expedientes` creado con `public = false`
- [ ] Bucket `avatares` creado con `public = true`
- [ ] `file_size_limit` configurado (50MB expedientes, 5MB avatares)
- [ ] `allowed_mime_types` restringido a tipos válidos

### Políticas
- [ ] Política de lectura por clínica en `expedientes`
- [ ] Política de escritura por clínica en `expedientes`
- [ ] Política de lectura pública en `avatares`
- [ ] Política de escritura para autenticados en `avatares`

### Rutas
- [ ] Archivos de expedientes en `{clinica_id}/{paciente_id}/{uuid}.{ext}`
- [ ] Avatares en `pacientes/{paciente_id}.{ext}`
- [ ] URLs de expedientes con tokens temporales, nunca públicas

---

*skills/supabase/storage.md — Vittal v1.0.0*
