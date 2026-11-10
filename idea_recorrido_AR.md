# Idea resumida – Recorrido AR con personaje 3D

## Objetivo
Crear una experiencia de realidad aumentada en **Unity + Vuforia** donde un personaje 3D aparezca al escanear un código QR o marcador y ejecute una pequeña secuencia de bienvenida y guía.

## Flujo de la experiencia

1. **Escaneo del QR**
   - Vuforia detecta el marcador.
   - Aparece el personaje 3D frente a la cámara.

2. **Saludo**
   - El personaje mira al usuario.
   - Ejecuta la animación **Waving**.

3. **Giro de 180°**
   - El personaje gira para quedar de espaldas a la cámara.
   - Se puede usar **Walking Turn 180** o una animación de giro equivalente.

4. **Caminar hacia adelante**
   - Ejecuta la animación **Walking**.
   - El personaje avanza algunos pasos alejándose del usuario.

5. **Giro hacia el usuario**
   - Se detiene.
   - Realiza otro giro de **180°** para volver a mirar a la cámara.

6. **Invitación**
   - Ejecuta la animación **Beckoning**.
   - El gesto indica al usuario: **“ven / sígueme”**.

## Secuencia resumida

**QR detectado → Aparece personaje → Waving → Giro 180° → Walking → Giro 180° → Beckoning**

## Recursos disponibles

- `Waving.fbx`
- `Waving2.fbx`
- `Walking.fbx`
- `Walking Turn 180.fbx`
- `Right Turn.fbx`
- `Stop Walking.fbx`
- `Beckoning.fbx`
- `Running.fbx`
- `Running Slide.fbx`

## Implementación prevista en Unity

La secuencia se controlará mediante:

- **Animator Controller** para las animaciones.
- **C#** para cambiar de estado en el orden definido.
- Movimiento del `Transform` o **Root Motion** durante `Walking`.
- Inicio de la secuencia cuando Vuforia detecte el marcador.

## Resultado esperado

Desde la perspectiva de la cámara del celular:

**Paso 1:** personaje aparece y saluda.  
**Paso 2:** gira 180°.  
**Paso 3:** camina alejándose de la cámara.  
**Paso 4:** vuelve a girar hacia el usuario.  
**Paso 5:** hace el gesto de “ven aquí”.

La idea es que el personaje funcione como una **mascota o guía AR** que posteriormente pueda conducir al usuario hacia diferentes puntos de interés.
