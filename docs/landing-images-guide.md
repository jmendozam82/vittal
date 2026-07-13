# Guía de Imágenes — Landing Page HU-L01

> **Fecha:** 2026-07-11
> **Estado:** Referencia para imágenes de la landing
> **Ubicación:** `wwwroot/images/landing/` (en la aplicación, NO en Supabase Storage)
> **Decisión:** Las imágenes son estáticas y se despliegan con la aplicación

---

## 1. Especificaciones Técnicas

### 1.1 Ubicación de las Imágenes

| Propiedad | Valor |
|---|---|
| **Ruta local** | `src/Vittal.Aplicacion/wwwroot/images/landing/` |
| **URL pública** | `/images/landing/[carpeta]/[archivo]` |
| **Storage** | NO aplica — imágenes en la aplicación |
| **Despliegue** | Se incluyen en cada build/deploy |

### 1.2 Formatos Aceptados

| Formato | Uso | Recomendado |
|---|---|---|
| **PNG** | Logos, iconos, imágenes con transparencia | ✅ Sí |
| **WebP** | Fotos, banners (mejor compresión) | ✅ Sí (recomendado) |
| **JPG/JPEG** | Fotos, screenshots | ✅ Sí |
| **SVG** | Iconos vectoriales, logos | ✅ Sí |
| **AVIF** | Fotos (compresión máxima) | ⚠️ Opcional |
| **GIF** | Animaciones simples | ⚠️ Evitar |

### 1.3 Tamaños Recomendados

| Tipo de imagen | Dimensiones | Peso máximo | Formato |
|---|---|---|---|
| **Logo principal** | 200x60 px | 50 KB | PNG/SVG |
| **Logo white (navbar)** | 200x60 px | 50 KB | PNG/SVG |
| **Hero banner** | 1920x1080 px | 200 KB | WebP/JPG |
| **Favicon** | 32x32 px | 10 KB | PNG |
| **Apple touch icon** | 180x180 px | 30 KB | PNG |
| **Open Graph image** | 1200x630 px | 150 KB | JPG/PNG |
| **Feature icons** | 128x128 px | 20 KB | PNG/SVG |
| **Benefit icons** | 64x64 px | 15 KB | PNG/SVG |
| **Screenshots** | 800x600 px | 100 KB | WebP/JPG |

### 1.4 Estructura de Carpetas Local

```
src/Vittal.Aplicacion/wwwroot/images/landing/
├── logo-vittal.png                    ← Logo principal (color)
├── logo-vittal-white.svg              ← Logo para navbar dark (blanco)
├── favicon.ico                        ← Favicon
├── favicon-96x96.png                  ← Favicon 96x96
├── apple-touch-icon.png               ← Apple touch icon
├── og-vittal.png                      ← Open Graph image (1200x630)
├── features/
│   ├── icon-expedientes.svg           ← Icono de Expedientes
│   ├── icon-agenda.svg                ← Icono de Agenda
│   ├── icon-cola-espera.svg           ← Icono de Cola de Espera
│   ├── icon-diagnosticos.svg          ← Icono de Diagnósticos
│   ├── icon-cirugias.svg              ← Icono de Cirugías
│   ├── icon-reportes.svg              ← Icono de Reportes
│   ├── icon-alertas.svg               ← Icono de Alertas
│   ├── icon-tratamientos.svg          ← Icono de Tratamientos
│   ├── icon-examenes.svg              ← Icono de Exámenes
│   ├── icon-recomendaciones.svg       ← Icono de Recomendaciones
│   ├── icon-signos-vitales.svg        ← Icono de Signos Vitales
│   └── icon-antecedentes.svg          ← Icono de Antecedentes
├── benefits/
│   ├── icon-director.svg              ← Icono rol Director
│   ├── icon-gerente.svg               ← Icono rol Gerente
│   ├── icon-doctor.svg                ← Icono rol Doctor
│   └── icon-recepcionista.svg         ← Icono rol Recepcionista
└── icons/
    ├── icon-cloud.svg                 ← Icono "100% Cloud"
    ├── icon-multi-tenant.svg          ← Icono "Multi Tenant"
    └── icon-24-7.svg                  ← Icono "24/7"
```

---

## 2. Catálogo de Imágenes Necesarias

### 2.1 Imágenes Obligatorias (Fase 1)

| # | Nombre | Sección | Dimensiones | Formato | Descripción |
|---|---|---|---|---|---|
| 1 | `logo-vittal.png` | Navbar/Footer | 200x60 | PNG | Logo principal con fondo transparente |
| 2 | `logo-vittal-white.svg` | Navbar (dark) | 200x60 | SVG | Logo blanco para navbar oscuro |
| 3 | `favicon.ico` | Browser | 32x32 | ICO | Favicon del sitio |
| 4 | `favicon-96x96.png` | Browser | 96x96 | PNG | Favicon alternativo |
| 5 | `apple-touch-icon.png` | iOS | 180x180 | PNG | Icono para pantalla de inicio iOS |
| 6 | `og-vittal.png` | Redes sociales | 1200x630 | JPG | Imagen para compartir en redes |

### 2.2 Feature Icons — Sección Funcionalidades

| # | Nombre | Módulo | Descripción |
|---|---|---|---|
| 8 | `icon-expedientes.png` | Expedientes | Icono de carpeta/clínico |
| 9 | `icon-agenda.png` | Agenda | Icono de calendario |
| 10 | `icon-cola-espera.png` | Cola de Espera | Icono de lista/cola |
| 11 | `icon-diagnosticos.png` | Diagnósticos | Icono de estetoscopio |
| 12 | `icon-cirugias.png` | Cirugías | Icono quirúrgico |
| 13 | `icon-reportes.png` | Reportes | Icono de gráfica |
| 14 | `icon-alertas.png` | Alertas | Icono de campana |
| 15 | `icon-tratamientos.png` | Tratamientos | Icono de pastilla |
| 16 | `icon-examenes.png` | Exámenes | Icono de microscopio |
| 17 | `icon-recomendaciones.png` | Recomendaciones | Icono de documento |
| 18 | `icon-signos-vitales.png` | Signos Vitales | Icono de corazón/pulso |
| 19 | `icon-antecedentes.png` | Antecedentes | Icono de historial |

### 2.3 Benefit Icons — Sección Beneficios

| # | Nombre | Rol | Descripción |
|---|---|---|---|
| 20 | `icon-director.png` | Director | Icono de persona ejecutiva |
| 21 | `icon-gerente.png` | Gerente | Icono de gestión |
| 22 | `icon-doctor.png` | Doctor | Icono médico |
| 23 | `icon-recepcionista.png` | Recepcionista | Icono de recepción |

### 2.5 Stats Icons — Hero Section

| # | Nombre | Stat | Descripción |
|---|---|---|---|
| 24 | `icon-cloud.png` | 100% Cloud | Icono de nube |
| 25 | `icon-multi-tenant.png` | Multi Tenant | Icono de usuarios |
| 26 | `icon-24-7.png` | 24/7 | Icono de reloj |

---

## 3. Prompts para Generación con IA

### 3.1 Logo Principal

```
Prompt para DALL-E / Midjourney:

"Minimalist medical software logo for 'Vittal', clean modern design, 
flat design style, medical cross or heartbeat line integrated into 
the letter V, professional healthcare aesthetic, blue and white color 
scheme (#2563EB primary blue), transparent background, vector style, 
suitable for web header, 200x60 pixels aspect ratio"

Prompt en español:
"Logo minimalista de software médico para 'Vittal', diseño moderno y limpio, 
estilo flat design, cruz médica o línea de pulso integrada en la letra V, 
estética profesional de salud, esquema de colores azul y blanco (#2563EB azul primario), 
fondo transparente, estilo vectorial, adecuado para encabezado web, 
relación de aspecto 200x60 píxeles"
```

### 3.2 Logo Blanco (Navbar Dark)

```
Prompt:

"Same Vittal logo but in pure white color, minimalist medical software 
logo, flat design, suitable for dark navigation bar, transparent background, 
vector style, 200x60 pixels aspect ratio"

Prompt en español:
"Mismo logo de Vittal pero en color blanco puro, logo minimalista de software médico, 
estilo flat design, adecuado para barra de navegación oscura, fondo transparente, 
estilo vectorial, relación de aspecto 200x60 píxeles"
```

### 3.3 Hero Background

```
Prompt:

"Abstract medical technology background, soft blue gradient, subtle 
geometric patterns, DNA helix or medical cross watermark, modern 
healthcare SaaS aesthetic, clean and professional, 1920x1080 resolution, 
subtle light effects, not distracting"

Prompt en español:
"Fondo abstracto de tecnología médica, gradiente azul suave, patrones 
geométricos sutiles, marca de agua de hélice de ADN o cruz médica, 
estética moderna de SaaS de salud, limpio y profesional, 
resolución 1920x1080, efectos de luz sutiles, no distractor"
```

### 3.4 Feature Icons (Estilo Uniforme)

```
Prompt para cada icono:

"[Feature name] flat icon, medical software style, blue color (#2563EB), 
white background, minimalist design, 128x128 pixels, suitable for 
web feature card, clean lines, professional healthcare aesthetic"

Ejemplo para Expedientes:
"Medical records folder flat icon, healthcare software style, blue color (#2563EB), 
white background, minimalist design, 128x128 pixels, suitable for 
web feature card, clean lines, professional healthcare aesthetic"

Ejemplo para Agenda:
"Calendar appointment flat icon, medical software style, blue color (#2563EB), 
white background, minimalist design, 128x128 pixels, suitable for 
web feature card, clean lines, professional healthcare aesthetic"

Prompt en español (ejemplo):
"Icono plano de expedientes médicos, estilo software de salud, color azul (#2563EB), 
fondo blanco, diseño minimalista, 128x128 píxeles, adecuado para 
tarjeta de funcionalidad web, líneas limpias, estética profesional de salud"
```

### 3.5 Benefit Icons (Estilo Uniforme)

```
Prompt:

"[Role] person flat icon, medical professional style, blue color (#2563EB), 
white background, minimalist design, 64x64 pixels, suitable for 
web benefit card, clean lines, professional healthcare aesthetic"

Ejemplo para Doctor:
"Doctor with stethoscope flat icon, medical professional style, blue color (#2563EB), 
white background, minimalist design, 64x64 pixels, suitable for 
web benefit card, clean lines, professional healthcare aesthetic"

Prompt en español:
"Icono plano de doctor con estetoscopio, estilo profesional médico, color azul (#2563EB), 
fondo blanco, diseño minimalista, 64x64 píxeles, adecuado para 
tarjeta de beneficios web, líneas limpias, estética profesional de salud"
```

### 3.6 Stats Icons

```
Prompt:

"[Stat name] flat icon, technology style, blue color (#2563EB), 
white background, minimalist design, 48x48 pixels, suitable for 
web statistics section, clean lines, modern aesthetic"

Ejemplo para Cloud:
"Cloud computing flat icon, technology style, blue color (#2563EB), 
white background, minimalist design, 48x48 pixels, suitable for 
web statistics section, clean lines, modern aesthetic"

Prompt en español:
"Icono plano de nube computacional, estilo tecnología, color azul (#2563EB), 
fondo blanco, diseño minimalista, 48x48 píxeles, adecuado para 
sección de estadísticas web, líneas limpias, estética moderna"
```

### 3.7 Open Graph Image

```
Prompt:

"Professional healthcare software promotional image, 'Vittal' text 
prominently displayed, medical technology theme, blue gradient 
background (#2563EB to #1E40AF), modern SaaS aesthetic, clean 
composition, 1200x630 pixels, suitable for social media sharing"

Prompt en español:
"Imagen promocional profesional de software de salud, texto 'Vittal' 
prominentemente displayed, tema de tecnología médica, fondo de gradiente 
azul (#2563EB a #1E40AF), estética moderna de SaaS, composición limpia, 
1200x630 píxeles, adecuada para compartir en redes sociales"
```

---

## 4. Cómo Se Ubican en la Landing

### 4.1 Estructura de la Landing y Ubicación de Imágenes

```
LANDING PAGE
│
├── NAVBAR (fijo)
│   ├── Logo: logo-vittal-white.png (izquierda)
│   └── Botón Login (derecha)
│
├── HERO SECTION (Index.cshtml)
│   ├── Fondo: hero-bg.jpg (o gradiente CSS)
│   ├── Título + subtítulo
│   ├── CTA buttons
│   └── Stats icons:
│       ├── icon-cloud.png → "100% Cloud"
│       ├── icon-multi-tenant.png → "Multi Tenant"
│       └── icon-24-7.png → "24/7"
│
├── FEATURED SECTION (Index.cshtml)
│   └── Grid de 9 features:
│       ├── icon-expedientes.png → "Expedientes"
│       ├── icon-agenda.png → "Agenda"
│       ├── icon-cola-espera.png → "Cola de Espera"
│       └── ... (12 features en total)
│
├── FUNCIONALIDADES (Funcionalidades.cshtml)
│   └── Grid de 12 tarjetas:
│       ├── icon-expedientes.png → Expedientes
│       ├── icon-agenda.png → Agenda
│       ├── icon-cola-espera.png → Cola de Espera
│       ├── icon-diagnosticos.png → Diagnósticos
│       ├── icon-tratamientos.png → Tratamientos
│       ├── icon-examenes.png → Exámenes
│       ├── icon-cirugias.png → Cirugías
│       ├── icon-recomendaciones.png → Recomendaciones
│       ├── icon-signos-vitales.png → Signos Vitales
│       ├── icon-antecedentes.png → Antecedentes
│       ├── icon-reportes.png → Reportes
│       └── icon-alertas.png → Alertas
│
├── BENEFICIOS (Beneficios.cshtml)
│   └── 4 tarjetas por rol:
│       ├── icon-director.png → "Para Directores"
│       ├── icon-gerente.png → "Para Gerentes"
│       ├── icon-doctor.png → "Para Doctores"
│       └── icon-recepcionista.png → "Para Recepcionistas"
│
├── CONTACTO (Contacto.cshtml)
│   └── Formulario (sin imágenes)
│
└── FOOTER
    └── Logo: logo-vittal.png (versión clara)
```

### 4.2 Referencia de Imágenes en el Código Razor

```html
<!-- Ejemplo: Logo en Navbar -->
<img src="~/images/landing/logo-vittal-white.svg" 
     alt="Vittal" 
     height="36" 
     loading="lazy" />

<!-- Ejemplo: Feature Icon -->
<img src="~/images/landing/features/icon-expedientes.svg" 
     alt="Expedientes" 
     class="feature-icon" 
     width="128" 
     height="128" 
     loading="lazy" />

<!-- Ejemplo: Benefit Icon -->
<img src="~/images/landing/benefits/icon-doctor.svg" 
     alt="Para Doctores" 
     class="benefit-icon" 
     width="64" 
     height="64" 
     loading="lazy" />

<!-- Ejemplo: Stats Icon -->
<img src="~/images/landing/icons/icon-cloud.svg" 
     alt="100% Cloud" 
     class="stat-icon" 
     width="48" 
     height="48" 
     loading="lazy" />
```

### 4.3 Rutas de Acceso

```
URL pública de una imagen:
/images/landing/[carpeta]/[archivo]

Ejemplos:
/images/landing/logo-vittal.png
/images/landing/features/icon-expedientes.svg
/images/landing/benefits/icon-doctor.svg
/images/landing/icons/icon-cloud.svg
```

---

## 5. ¿Las Imágenes Necesitan Estar Renderizadas?

### 5.2 Explicación Detallada

| Tipo de imagen | ¿Necesita render? | Ejemplo |
|---|---|---|
| **PNG/JPG/WebP** | **NO** | Fotos, screenshots, iconos raster |
| **SVG** | **NO** | Iconos vectoriales (el browser los renderiza) |
| **HTML/CSS** | **SÍ** | Gráficas, animaciones, gradientes complejos |

### 5.3 Lo que SÍ necesitas hacer

| Acción | Descripción |
|---|---|
| **Optimizar** | Comprimir imágenes para web (usar TinyPNG, Squoosh, etc.) |
| **Redimensionar** | Ajustar al tamaño exacto necesario (no subir 4K para un icono de 64px) |
| **Formato correcto** | Usar WebP para fotos, PNG para iconos con transparencia |
| **Lazy loading** | Agregar `loading="lazy"` para carga diferida |

### 5.4 Ejemplo de Optimización

```bash
# Con Squoosh CLI (recomendado)
npx squoosh-cli --webp '{quality: 80}' hero-bg.jpg -o hero-bg.webp

# Con ImageMagick
convert hero-bg.jpg -quality 80 -resize 1920x1080 hero-bg.webp

# Con Sharp (Node.js)
sharp('hero-bg.jpg')
  .webp({ quality: 80 })
  .resize(1920, 1080)
  .toFile('hero-bg.webp');
```

### 5.5 CSS para Imágenes en la Landing

```css
/* Imágenes responsive */
.feature-icon {
    width: 128px;
    height: 128px;
    object-fit: contain;
}

.benefit-icon {
    width: 64px;
    height: 64px;
    object-fit: contain;
}

.stat-icon {
    width: 48px;
    height: 48px;
    object-fit: contain;
}

/* Lazy loading placeholder */
img[loading="lazy"] {
    background-color: #f0f0f0;
    transition: opacity 0.3s ease;
}

img[loading="lazy"].loaded {
    opacity: 1;
}
```

---

## 6. Checklist de Verificación

- [ ] Logo visible en navbar
- [ ] Logo visible en footer
- [ ] Feature icons visibles en grid
- [ ] Benefit icons visibles en tarjetas
- [ ] Stats icons visibles en hero
- [ ] Lazy loading funcionando
- [ ] Imágenes responsive en mobile

---

## 7. Resumen

| Aspecto | Valor |
|---|---|
| **Ubicación** | `wwwroot/images/landing/` |
| **Formato** | SVG (iconos), PNG (logo, favicon) |
| **Total archivos** | 24 |
| **Decisión** | Imágenes en la aplicación, NO en Supabase Storage |
| **Razón** | Contenido estático que se despliega con la app |

---

*Guía de Imágenes — Landing Page HU-L01*
*Vittal v1.0.0 | 2026-07-11*
*Última actualización: Imágenes en wwwroot/images/landing/*
