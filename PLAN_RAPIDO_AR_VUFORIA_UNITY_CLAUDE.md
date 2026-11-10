# Plan rápido – Secuencia AR con Vuforia + Unity + Claude

## Objetivo
Crear un proyecto **rápido y funcional** en Unity usando **Vuforia + marcador QR**, donde un personaje 3D ejecute automáticamente esta secuencia:

**Detectar QR → aparecer → saludar → girar 180° → caminar → girar 180° → hacer “ven aquí”.**

> Para este prototipo con **Vuforia y marcador**, no necesitamos depender de ARCore. El objetivo es usar el tracking del marcador de Vuforia y mantener la configuración lo más simple posible.

---

## 1. Proyecto base en Unity

### Unity
Configurar únicamente:

- Proyecto nuevo.
- Vuforia instalado y habilitado.
- `ARCamera`.
- `ImageTarget` / marcador QR ya creado.
- Personaje 3D como hijo del `ImageTarget`.
- Escala, posición y orientación inicial correctas.

### Claude
Claude se encargará de:

- Crear estructura de carpetas.
- Crear scripts C#.
- Crear/controlar la secuencia.
- Gestionar cambios de animación.
- Gestionar giro y desplazamiento del personaje.
- Corregir errores de código.

---

## 2. Preparar las animaciones

Usaremos principalmente:

- `Waving.fbx` → saludo.
- `Walking Turn 180.fbx` o `Right Turn.fbx` → giro.
- `Walking.fbx` → caminar.
- `Stop Walking.fbx` → detenerse, si hace falta.
- `Beckoning.fbx` → “ven aquí”.

En Unity solo será necesario:

- Configurarlas como **Humanoid**.
- Asociarlas al mismo Avatar del personaje.
- Desactivar `Loop Time` en saludo, giro y Beckoning.
- Activar `Loop Time` únicamente en Walking si se necesita.

---

## 3. Animator Controller

Crear un Animator sencillo con estos estados:

`Idle`
→ `Wave`
→ `TurnAway`
→ `Walk`
→ `TurnBack`
→ `Beckoning`
→ `Idle`

No necesitamos lógica compleja dentro del Animator.

Claude controlará los cambios mediante código.

---

## 4. Script principal

Crear un único controlador principal, por ejemplo:

`ARCharacterSequence.cs`

Responsabilidades:

- Esperar a que Vuforia detecte el QR.
- Ejecutar la secuencia una sola vez.
- Reproducir cada animación.
- Esperar su duración.
- Rotar al personaje cuando corresponda.
- Moverlo hacia adelante durante `Walking`.
- Ejecutar `Beckoning` al final.

### Secuencia

1. QR detectado.
2. Esperar ~0.5 s.
3. `Waving`.
4. Giro de 180°.
5. `Walking` + movimiento hacia adelante.
6. Detenerse.
7. Segundo giro de 180°.
8. `Beckoning`.
9. Quedar en `Idle`.

---

## 5. Movimiento

Para evitar complicaciones:

- No usar navegación GPS.
- No usar NavMesh.
- No usar ARCore.
- No usar detección de superficies.
- No usar físicas complejas.

El personaje simplemente avanzará en su eje local:

`transform.position += transform.forward * velocidad`

El desplazamiento será corto, aproximadamente **1–2 metros virtuales**.

---

## 6. Detección del marcador

Cuando Vuforia detecte el marcador:

`OnTargetFound`
→ activar personaje
→ iniciar secuencia.

Cuando se pierda temporalmente el marcador:

- Para la primera versión, mantener el comportamiento simple.
- No reiniciar inmediatamente la secuencia.

La secuencia debe ejecutarse **una sola vez por aparición** para evitar que se repita constantemente.

---

## 7. Trabajo manual mínimo en Unity

Solo tendremos que hacer manualmente:

1. Crear proyecto.
2. Activar Vuforia.
3. Colocar `ARCamera`.
4. Colocar el `ImageTarget`.
5. Importar personaje + FBX.
6. Revisar Avatar/Humanoid.
7. Arrastrar referencias al Inspector.
8. Probar en el teléfono.

Todo el resto de programación lo hará Claude.

---

## 8. Estructura mínima

```text
Assets/
├── Models/
│   └── Character.fbx
├── Animations/
│   ├── Waving.fbx
│   ├── Walking.fbx
│   ├── Walking Turn 180.fbx
│   └── Beckoning.fbx
├── Animator/
│   └── CharacterAnimator.controller
├── Scripts/
│   ├── ARCharacterSequence.cs
│   └── VuforiaTargetHandler.cs
└── Vuforia/
```

---

## 9. Prioridad

### Primero hacer funcionar
- Detección QR.
- Aparición del personaje.
- Secuencia completa de animaciones.
- Giro correcto.
- Movimiento hacia adelante.

### Después mejorar
- Transiciones más suaves.
- Sonidos.
- Texto UI.
- Más recorridos.
- Diferentes destinos.
- Nuevas interacciones.

---

## Resultado mínimo esperado

Al apuntar la cámara al QR:

**Personaje aparece → saluda → se da la vuelta → camina alejándose → se vuelve a girar → mira al usuario → hace “ven aquí”.**

Si esto funciona correctamente en el teléfono, el primer prototipo está terminado.
