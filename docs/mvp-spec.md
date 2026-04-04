# Smart Home
## MVP Product Specification

> **Estado:** Draft v1  
> **Tipo de documento:** Product / MVP Spec  
> **Idioma de trabajo:** Español  
> **Nombre del producto:** **Smart Home** _(provisional)_

---

## 1. Resumen ejecutivo

**Smart Home** es una app móvil pensada para ayudar a convivir mejor dentro de un hogar compartido. Su objetivo no es ser una suite de productividad doméstica recargada, sino una herramienta extremadamente rápida para coordinar **listas de la compra**, **tareas del hogar** y **recados** entre las personas que conviven juntas.

El foco del MVP es resolver un problema cotidiano y muy frecuente:

- cosas que faltan en casa y nadie apunta;
- tareas que “alguien iba a hacer” pero quedan en el limbo;
- duplicidades en la compra;
- discusiones o confusión sobre quién añadió, hizo o compró algo;
- fricción excesiva en herramientas existentes.

La promesa principal del producto es:

> **coordinar el hogar en segundos, con la mínima fricción posible.**

El MVP se probará inicialmente en un entorno real y controlado: **Luis y su pareja**, sin necesidad de monetización ni distribución pública inicial.

---

## 2. Visión del producto

### 2.1 Visión
Crear una app móvil de uso diario que permita a dos o más personas organizar la vida del hogar de forma rápida, clara y compartida.

### 2.2 Principios del producto

1. **Rapidez por encima de complejidad**  
   Añadir o completar algo debe requerir el menor número posible de acciones.

2. **Diseño móvil realista**  
   La app debe poder usarse con una mano, deprisa, desde el supermercado, la cocina o el sofá.

3. **Colaboración sin fricción**  
   Compartir hogar, listas y cambios debe ser trivial.

4. **Claridad total**  
   Debe quedar claro qué está pendiente, qué está completado y quién hizo qué.

5. **MVP de verdad**  
   Todo lo que no aporte valor claro en la prueba inicial queda fuera.

---

## 3. Objetivo del MVP

El MVP debe validar si Smart Home resuelve de forma suficientemente buena estos casos de uso:

1. **Añadir rápidamente artículos a una lista compartida de compra.**
2. **Marcar artículos como comprados mientras una persona está comprando.**
3. **Crear y completar tareas del hogar compartidas.**
4. **Ver el estado actualizado en tiempo real en ambos dispositivos.**
5. **Evitar pérdida de contexto sobre quién añadió o completó algo.**
6. **Reducir la necesidad de coordinarse por WhatsApp u otros mensajes dispersos.**

### 3.1 Señales de éxito del MVP

Se considerará que el MVP tiene utilidad real si, durante el uso piloto:

- se utiliza varias veces por semana de forma natural;
- sustituye parcialmente mensajes o notas externas;
- resulta más cómodo que usar una nota del móvil o un chat;
- no genera fricción importante al compartir hogar o listas;
- los dos usuarios entienden el estado de las listas sin explicaciones.

---

## 4. Público objetivo inicial

### 4.1 Usuario primario
Parejas que comparten vivienda y necesitan coordinar:

- compra habitual;
- tareas del hogar;
- recados rápidos;
- pequeñas responsabilidades compartidas.

### 4.2 Usuarios secundarios futuros
Aunque no forman parte del foco inicial, el producto podría encajar después en:

- pisos compartidos;
- familias;
- personas que cuidan a familiares;
- compañeros de vivienda temporal.

### 4.3 Contexto de uso
La app se usará principalmente en estos contextos:

- en casa, al detectar que falta algo;
- fuera de casa, antes de entrar al supermercado;
- dentro del supermercado, mientras se compra;
- cuando aparece una tarea o recado inesperado;
- al revisar qué queda pendiente.

---

## 5. Problemas a resolver

### 5.1 Problemas actuales

1. **No existe un punto único de verdad del hogar.**  
   Parte de la información está en notas mentales, chats, papel o apps distintas.

2. **Se olvidan cosas.**  
   Faltan productos o tareas porque nadie los apuntó a tiempo.

3. **Hay duplicados.**  
   Se añaden varias veces los mismos artículos o no se sabe si algo ya estaba apuntado.

4. **No hay trazabilidad suficiente.**  
   No queda claro quién añadió algo o si alguien ya lo resolvió.

5. **La coordinación consume más energía de la necesaria.**  
   Algo que debería resolverse en 3 segundos acaba requiriendo mensajes, preguntas o comprobaciones.

### 5.2 Oportunidad del producto
La oportunidad no está en inventar una nueva categoría, sino en hacer una herramienta de coordinación doméstica que sea:

- más simple que las apps complejas de productividad;
- más útil que una nota compartida;
- más estructurada que un chat;
- más rápida que un gestor de tareas genérico.

---

## 6. Propuesta de valor

### 6.1 Propuesta de valor principal
**Smart Home permite compartir la compra, tareas y recados del hogar en una sola app, con actualización en tiempo real y casi sin esfuerzo.**

### 6.2 Diferenciadores del MVP

1. **Un único espacio compartido del hogar**  
   No solo compra. También tareas y recados.

2. **Interacción ultrarrápida**  
   Añadir, marcar y revisar debe ser inmediato.

3. **Dos vistas conceptualmente simples**  
   - cosas que hay que comprar;
   - cosas que hay que hacer.

4. **Colaboración clara**  
   Cada acción tiene contexto de autor y estado.

5. **Base sólida para crecer sin sobreconstruir**  
   El MVP ya deja preparada una evolución lógica.

---

## 7. Alcance funcional del MVP

### 7.1 Incluido en el MVP

#### A. Cuenta y acceso
- Registro de usuario.
- Inicio de sesión.
- Persistencia de sesión.

#### B. Hogar compartido
- Crear un hogar.
- Unirse a un hogar mediante invitación.
- Ver miembros del hogar.

#### C. Listas
- Crear listas dentro del hogar.
- Dos tipos de lista:
  - **Compra**
  - **Tareas**
- Editar nombre de lista.
- Archivar lista.

#### D. Ítems
- Crear ítem.
- Editar ítem.
- Marcar ítem como completado.
- Desmarcar ítem.
- Eliminar ítem.
- Ver quién lo creó.
- Ver quién lo completó.
- Ver fecha de creación/completado.

#### E. Metadatos mínimos por ítem
- título;
- nota corta opcional;
- cantidad opcional;
- unidad opcional;
- estado;
- autor;
- timestamps.

#### F. Colaboración en tiempo real
- Sincronización de cambios entre dispositivos.
- Refresco de estado casi inmediato tras cambios.

#### G. Historial básico
- Registro básico de eventos relevantes:
  - creación de ítem;
  - edición de ítem;
  - completado/descompletado;
  - eliminación.

#### H. Ayudas de usabilidad mínimas
- aviso de posible duplicado al crear un ítem;
- sugerencias rápidas basadas en ítems frecuentes o recientes.

### 7.2 Explícitamente fuera del MVP

Estas funcionalidades **no** deben entrar en la primera versión:

- recetas;
- planificación de menús;
- presupuestos avanzados;
- OCR de tickets;
- IA compleja o asistente conversacional;
- chat interno;
- gamificación;
- integración con Alexa, Google Assistant o WhatsApp;
- geolocalización avanzada;
- notificaciones sofisticadas por contexto;
- categorías complejas o taxonomías profundas;
- soporte multi-hogar avanzado;
- compras por tienda o por recorrido de supermercado;
- adjuntos/imágenes por ítem.

---

## 8. Casos de uso principales

### 8.1 Caso de uso 1: Añadir un producto que falta
**Como** miembro del hogar  
**quiero** añadir rápidamente un producto a una lista de compra  
**para** no olvidarlo y que la otra persona también lo vea.

**Criterios clave:**
- añadirlo debe llevar pocos segundos;
- ambos usuarios deben verlo rápido;
- si ya existe algo parecido, el sistema debe advertirlo.

### 8.2 Caso de uso 2: Comprar desde el supermercado
**Como** miembro del hogar  
**quiero** abrir la lista y marcar productos mientras compro  
**para** no duplicar compras y saber lo que queda.

**Criterios clave:**
- check rápido;
- elementos visualmente claros;
- estado actualizado para ambos.

### 8.3 Caso de uso 3: Crear una tarea doméstica
**Como** miembro del hogar  
**quiero** apuntar una tarea pendiente  
**para** que no se olvide y cualquiera pueda resolverla.

### 8.4 Caso de uso 4: Entender quién hizo qué
**Como** miembro del hogar  
**quiero** ver quién añadió o completó un ítem  
**para** mantener contexto sin tener que preguntar.

### 8.5 Caso de uso 5: Revisar pendientes del hogar
**Como** miembro del hogar  
**quiero** entrar y ver rápidamente qué queda pendiente  
**para** actuar sin perder tiempo navegando.

---

## 9. User stories del MVP

### 9.1 Onboarding y hogar
- Como usuario, quiero registrarme para poder usar la app.
- Como usuario, quiero crear un hogar para compartirlo con otra persona.
- Como usuario, quiero invitar a otra persona al hogar mediante enlace o código.
- Como usuario invitado, quiero unirme al hogar fácilmente.

### 9.2 Gestión de listas
- Como usuario, quiero crear una lista de compra.
- Como usuario, quiero crear una lista de tareas.
- Como usuario, quiero renombrar una lista.
- Como usuario, quiero archivar una lista que ya no uso.

### 9.3 Gestión de ítems
- Como usuario, quiero añadir un ítem con solo el título.
- Como usuario, quiero opcionalmente indicar cantidad y unidad.
- Como usuario, quiero añadir una nota corta si hace falta contexto.
- Como usuario, quiero editar un ítem si me he equivocado.
- Como usuario, quiero eliminar un ítem innecesario.
- Como usuario, quiero marcar un ítem como completado.
- Como usuario, quiero deshacer esa acción si ha sido un error.

### 9.4 Colaboración
- Como usuario, quiero que mi pareja vea enseguida los cambios.
- Como usuario, quiero saber quién creó el ítem.
- Como usuario, quiero saber quién lo completó.

### 9.5 Usabilidad
- Como usuario, quiero recibir un aviso si el ítem parece duplicado.
- Como usuario, quiero ver sugerencias frecuentes para escribir menos.

---

## 10. Modelo mental del usuario

El usuario probablemente piensa el producto en este orden natural:

1. **Estamos compartiendo una casa.**
2. **Necesitamos un sitio común para apuntar cosas.**
3. **Algunas cosas son para comprar y otras para hacer.**
4. **Quiero añadir algo en segundos, sin configurar nada raro.**
5. **Quiero ver lo pendiente de un vistazo.**
6. **Quiero marcar lo hecho o comprado mientras lo hago.**
7. **Quiero entender quién ha movido cada cosa sin pedir explicaciones.**

Este modelo mental debe reflejarse en la IA del producto y en la estructura de navegación.

---

## 11. Navegación del MVP

### 11.1 Estructura principal
La estructura recomendada del MVP es:

1. **Pantalla de acceso / onboarding**
2. **Pantalla de hogar**
3. **Pantalla de listas del hogar**
4. **Pantalla de detalle de lista**
5. **Pantalla/modal de crear/editar ítem**
6. **Pantalla de miembros/invitación**
7. **Pantalla de ajustes mínimos**

### 11.2 Navegación sugerida
**Bottom navigation simple** con 2-3 secciones como máximo:

- **Listas**
- **Actividad** _(opcional en MVP, si el historial merece pantalla propia)_
- **Ajustes**

Alternativamente, para reducir complejidad, el MVP puede tener una navegación aún más simple:

- Home/Listas
- Ajustes

Y dejar la actividad dentro de cada lista.

### 11.3 Recomendación
Para el MVP, se recomienda evitar una navegación profunda.  
La entrada principal del producto debe ser:

> **Hogar → Listas → Lista → Ítems**

---

## 12. Pantallas del MVP

### 12.1 Pantalla 1: Onboarding / acceso
**Objetivo:** permitir acceso rápido.

**Contenido mínimo:**
- logo/nombre provisional;
- CTA de registro/login;
- flujo simple de autenticación.

**Requisitos:**
- sin pasos innecesarios;
- preparar al usuario para crear o unirse a un hogar.

### 12.2 Pantalla 2: Crear o unirse a un hogar
**Objetivo:** resolver la colaboración desde el inicio.

**Acciones:**
- crear hogar;
- unirse mediante código/enlace.

**Datos mínimos:**
- nombre del hogar.

### 12.3 Pantalla 3: Listas del hogar
**Objetivo:** mostrar el espacio compartido central.

**Contenido:**
- nombre del hogar;
- listas activas;
- tipo de lista;
- contador de pendientes;
- CTA para crear nueva lista;
- acceso a miembros/invitación.

**Recomendaciones UX:**
- tarjetas limpias;
- distinguir listas de compra y tareas;
- priorizar listas activas y con pendientes.

### 12.4 Pantalla 4: Detalle de lista
**Objetivo:** ser la pantalla de uso más frecuente.

**Contenido:**
- nombre de lista;
- tipo de lista;
- ítems pendientes;
- ítems completados;
- buscador opcional si compensa;
- CTA flotante o fijo para añadir ítem.

**Interacciones clave:**
- check rápido;
- swipe opcional para acciones rápidas;
- tap en ítem para editar;
- separación clara entre pendientes y completados.

### 12.5 Pantalla 5: Crear / editar ítem
**Objetivo:** máxima velocidad con suficiente contexto.

**Campos:**
- título (obligatorio)
- cantidad (opcional)
- unidad (opcional)
- nota (opcional)

**Extras UX deseables en MVP:**
- sugerencias recientes/frecuentes;
- aviso de duplicado;
- confirmación clara al guardar.

### 12.6 Pantalla 6: Miembros del hogar
**Objetivo:** gestionar colaboración.

**Contenido:**
- miembros actuales;
- rol básico si aplica;
- generar enlace/código de invitación;
- copiar o compartir invitación.

### 12.7 Pantalla 7: Ajustes mínimos
**Contenido:**
- perfil básico;
- cerrar sesión;
- nombre del hogar;
- salir del hogar si aplica.

---

## 13. Flujos funcionales principales

### 13.1 Flujo A: Crear hogar y compartirlo
1. Usuario se registra o inicia sesión.
2. Elige crear hogar.
3. Introduce nombre del hogar.
4. El sistema crea el hogar.
5. El usuario accede a la vista de listas vacía.
6. Desde miembros/invitación, genera un enlace o código.
7. La otra persona lo usa para unirse.
8. Ambas personas comparten ya el mismo espacio.

### 13.2 Flujo B: Crear una lista de compra
1. Usuario entra en la pantalla de listas.
2. Pulsa “crear lista”.
3. Indica nombre.
4. Selecciona tipo “Compra”.
5. La lista aparece en el hogar.

### 13.3 Flujo C: Añadir un ítem
1. Usuario abre una lista.
2. Pulsa añadir ítem.
3. Escribe título.
4. Opcionalmente completa cantidad, unidad y nota.
5. El sistema comprueba duplicados básicos.
6. El usuario guarda.
7. El ítem aparece como pendiente para ambos usuarios.

### 13.4 Flujo D: Marcar como comprado/hecho
1. Usuario abre la lista.
2. Marca el ítem.
3. El sistema actualiza estado a completado.
4. Registra quién y cuándo lo completó.
5. El otro usuario ve el cambio en tiempo real.

### 13.5 Flujo E: Editar o deshacer
1. Usuario toca el ítem.
2. Modifica campos o desmarca como completado.
3. El sistema actualiza estado y conserva trazabilidad mínima.

---

## 14. Requisitos funcionales detallados

### 14.1 Gestión de usuarios
- El sistema debe permitir registro e inicio de sesión.
- El sistema debe persistir sesión entre aperturas.
- El sistema debe permitir al usuario cerrar sesión.

### 14.2 Gestión de hogares
- Un usuario debe poder crear un hogar.
- Un hogar debe tener un nombre.
- Un hogar debe poder tener varios miembros.
- Un usuario debe poder unirse a un hogar mediante invitación válida.

### 14.3 Gestión de miembros
- El sistema debe mostrar los miembros del hogar.
- El sistema debe permitir generar una invitación.
- La invitación debe ser fácil de compartir.

### 14.4 Gestión de listas
- El sistema debe permitir crear listas dentro de un hogar.
- Cada lista debe tener tipo.
- Cada lista debe poder renombrarse.
- Cada lista debe poder archivarse.
- Las listas archivadas no deben mezclarse con las activas por defecto.

### 14.5 Gestión de ítems
- Cada ítem debe pertenecer a una lista.
- El título debe ser obligatorio.
- Cantidad, unidad y nota deben ser opcionales.
- Un ítem debe poder estar pendiente o completado.
- Un ítem debe poder editarse.
- Un ítem debe poder eliminarse.
- El sistema debe registrar autor de creación.
- El sistema debe registrar autor de completado cuando aplique.
- El sistema debe registrar timestamps relevantes.

### 14.6 Duplicados
- Al añadir un ítem, el sistema debe detectar coincidencias razonables dentro de la misma lista activa.
- El sistema debe avisar sin bloquear automáticamente la creación.

### 14.7 Sugerencias rápidas
- El sistema debe poder sugerir ítems recientes o frecuentes.
- La sugerencia debe reducir tiempo de escritura.

### 14.8 Sincronización
- Los cambios deben reflejarse en ambos dispositivos en tiempo cercano a real.
- El sistema debe manejar conflictos simples de edición de forma consistente.

### 14.9 Actividad / trazabilidad
- El sistema debe registrar eventos clave.
- El usuario debe poder ver contexto de autoría y completado al menos a nivel de ítem.

---

## 15. Requisitos no funcionales

### 15.1 Rendimiento
- La app debe abrir rápido.
- Añadir un ítem debe sentirse inmediato.
- El cambio de estado de un ítem debe reflejarse sin retrasos perceptibles excesivos.

### 15.2 Simplicidad
- La app no debe requerir formación.
- Las acciones principales deben ser autoexplicativas.

### 15.3 Fiabilidad
- No deben perderse cambios una vez confirmados.
- El estado compartido debe ser consistente.

### 15.4 Seguridad
- Los hogares deben aislar sus datos entre sí.
- Solo miembros autorizados deben acceder al contenido del hogar.
- Las invitaciones deben tener validez controlable.

### 15.5 Escalabilidad razonable
Aunque el MVP se pruebe con 2 personas, la arquitectura debe permitir crecer sin rehacer el núcleo básico.

### 15.6 Mantenibilidad
El sistema debe construirse con una base limpia, simple y modular.  
No se debe sobrearquitecturar el MVP, pero sí dejarlo ordenado.

---

## 16. Reglas de negocio iniciales

1. Un usuario puede pertenecer al menos a un hogar.
2. Un hogar contiene listas.
3. Una lista pertenece a un único hogar.
4. Un ítem pertenece a una única lista.
5. Un ítem puede estar en estado `pending` o `completed`.
6. Un ítem completado registra quién lo completó y cuándo.
7. Un ítem eliminado desaparece de la vista normal.
8. Las invitaciones deben poder invalidarse o expirar.
9. Los duplicados son advertencias, no bloqueos obligatorios.

---

## 17. Dominio y modelo conceptual

### 17.1 Entidades principales

#### User
Representa una persona que usa la app.

#### Household
Representa el hogar compartido.

#### HouseholdMembership
Representa la pertenencia de un usuario a un hogar.

#### List
Representa una lista del hogar.

#### ListItem
Representa un elemento de compra o tarea.

#### Invite
Representa una invitación al hogar.

#### ActivityLog
Representa un evento relevante del sistema.

### 17.2 Propuesta de atributos mínimos

#### User
- id
- displayName
- email
- createdAt

#### Household
- id
- name
- createdBy
- createdAt

#### HouseholdMembership
- id
- householdId
- userId
- role
- joinedAt

#### List
- id
- householdId
- name
- type (`shopping`, `tasks`)
- isArchived
- createdBy
- createdAt
- updatedAt

#### ListItem
- id
- listId
- title
- note
- quantity
- unit
- status (`pending`, `completed`)
- createdBy
- completedBy
- createdAt
- updatedAt
- completedAt

#### Invite
- id
- householdId
- code / token
- createdBy
- expiresAt
- usedAt
- revokedAt

#### ActivityLog
- id
- householdId
- entityType
- entityId
- actionType
- performedBy
- createdAt
- metadata

---

## 18. API / backend scope de alto nivel

> Nota: este documento define producto y MVP. No entra en diseño técnico profundo, pero deja una guía clara.

### 18.1 Backend sugerido
- **ASP.NET Core**
- API limpia para móvil
- Persistencia relacional
- Auth segura
- mecanismo de realtime

### 18.2 Capacidades backend del MVP
- autenticación;
- gestión de hogares;
- invitaciones;
- CRUD de listas;
- CRUD de ítems;
- detección básica de duplicados;
- feed mínimo de actividad;
- eventos en tiempo real.

### 18.3 Estilo arquitectónico recomendado
Para este MVP, se recomienda:

- **modular monolith**;
- separación clara de dominio, aplicación, infraestructura y API;
- sin microservicios en esta fase;
- modelo centrado en simplicidad y velocidad de iteración.

---

## 19. App móvil / cliente KMP de alto nivel

### 19.1 Objetivo del cliente
La app móvil debe priorizar:

- rapidez de interacción;
- consistencia de estado;
- sincronización clara;
- bajo número de pantallas;
- feedback inmediato.

### 19.2 Responsabilidades del cliente
- autenticación;
- navegación;
- render de listas e ítems;
- cache local ligera si se implementa;
- gestión de estado de UI;
- sincronización con backend.

### 19.3 Consideraciones KMP
KMP encaja bien para:

- compartir modelos de dominio;
- compartir lógica de casos de uso;
- compartir validaciones;
- compartir clientes de red;
- reducir duplicidad futura.

No es necesario decidir en este documento todo el detalle técnico de UI, pero el MVP debe construirse de modo que la experiencia móvil sea el centro.

---

## 20. Experiencia de usuario: decisiones clave

### 20.1 Añadir debe ser más importante que configurar
La acción principal del producto es añadir algo rápido.  
Todo lo demás debe estar subordinado a eso.

### 20.2 Los pendientes son la vista por defecto
Lo más importante es lo que aún falta.  
Los completados deben existir, pero sin robar protagonismo.

### 20.3 El sistema debe ser tolerante
Si el usuario crea duplicados o se equivoca al completar, debe poder corregirlo fácilmente.

### 20.4 El contexto debe existir sin saturar
Mostrar quién hizo qué aporta valor, pero sin llenar la UI de ruido.

### 20.5 Un hogar, una verdad compartida
La app debe sentirse como el sitio único donde están las cosas del hogar.

---

## 21. Criterios de aceptación por bloques

### 21.1 Acceso y hogar
- Un usuario puede registrarse e iniciar sesión.
- Un usuario puede crear un hogar y verlo persistido.
- Un segundo usuario puede unirse al hogar con invitación válida.
- Ambos ven el mismo conjunto de listas.

### 21.2 Listas
- Se puede crear una lista de tipo compra.
- Se puede crear una lista de tipo tareas.
- Se puede renombrar una lista.
- Se puede archivar una lista.

### 21.3 Ítems
- Se puede crear un ítem con solo título.
- Se puede crear un ítem con campos opcionales.
- Se puede editar un ítem existente.
- Se puede eliminar un ítem.
- Se puede completar y descompletar un ítem.

### 21.4 Colaboración
- Los cambios hechos por un usuario aparecen en el otro en tiempo razonable.
- El autor de creación se conserva.
- El autor de completado se conserva cuando aplica.

### 21.5 Usabilidad
- El flujo de añadir un ítem se resuelve de forma rápida.
- El usuario puede distinguir fácilmente qué está pendiente y qué está completado.
- El sistema advierte sobre duplicados básicos.

---

## 22. Métricas de validación del piloto

Aunque no haya monetización, sí conviene medir si el MVP sirve.

### 22.1 Métricas cuantitativas mínimas
- número de sesiones por semana;
- número de ítems creados por semana;
- porcentaje de ítems completados;
- tiempo medio entre creación y completado;
- listas activas por hogar;
- uso de sugerencias;
- frecuencia de duplicados detectados.

### 22.2 Métricas cualitativas
- ¿os acordáis de usarla sin forzaros?
- ¿os evita mensajes innecesarios?
- ¿sentís que es más cómoda que una nota o WhatsApp?
- ¿alguna parte da pereza o confusión?
- ¿echáis algo importante en falta para el uso real?

---

## 23. Riesgos del MVP

### 23.1 Riesgo: parecer “otra lista más”
**Mitigación:** centrar el producto en hogar compartido y velocidad extrema.

### 23.2 Riesgo: demasiada complejidad demasiado pronto
**Mitigación:** mantener scope estricto y excluir extras tentadores.

### 23.3 Riesgo: sincronización poco fiable
**Mitigación:** priorizar consistencia y realtime antes que features vistosas.

### 23.4 Riesgo: UX torpe al añadir ítems
**Mitigación:** tratar el flujo de creación como pieza más crítica del producto.

### 23.5 Riesgo: el piloto no cambie hábitos
**Mitigación:** diseñar la app para uso real diario, no para demos bonitas.

---

## 24. Prioridades del backlog inicial

### 24.1 Prioridad P0
Imprescindible para que exista MVP funcional:

- auth;
- crear/unirse a hogar;
- crear listas;
- CRUD de ítems;
- estado pendiente/completado;
- sincronización compartida;
- metadatos básicos de autoría.

### 24.2 Prioridad P1
Muy valioso para una primera iteración útil:

- invitación por enlace/código cómoda;
- aviso de duplicados;
- sugerencias rápidas;
- archivado de listas;
- historial básico.

### 24.3 Prioridad P2
Puede esperar a la siguiente iteración:

- notificaciones push;
- recurrentes;
- modo compra más avanzado;
- filtros;
- actividad global más rica;
- soporte de voz.

---

## 25. Roadmap propuesto del MVP

### Fase 1. Fundaciones
- auth
- hogar
- miembros
- invitaciones
- modelo de listas

### Fase 2. Núcleo de uso diario
- crear listas
- crear/editar/eliminar ítems
- completar/descompletar
- UI de lista optimizada

### Fase 3. Colaboración real
- realtime
- autoría
- timestamps
- consistencia de cambios

### Fase 4. Pulido de valor
- duplicados
- sugerencias
- archivado
- actividad básica

### Fase 5. Piloto real
- prueba diaria con dos usuarios
- recogida de fricciones
- decisiones para iteración 2

---

## 26. Open questions para resolver antes o durante implementación

1. ¿Un usuario podrá pertenecer a varios hogares desde el MVP o solo a uno?  
   **Recomendación MVP:** uno solo visible/usable inicialmente.

2. ¿Las listas deben estar separadas por tipo o mezcladas en una sola vista?  
   **Recomendación MVP:** mezcladas en la vista del hogar, pero claramente etiquetadas por tipo.

3. ¿La actividad debe tener pantalla propia?  
   **Recomendación MVP:** no necesariamente; basta con metadata en ítems y quizá un historial básico por lista.

4. ¿Las sugerencias serán recientes, frecuentes o ambas?  
   **Recomendación MVP:** empezar por recientes y/o más usados por hogar.

5. ¿El duplicado debe ser exacto o difuso?  
   **Recomendación MVP:** coincidencia sencilla case-insensitive, trimmed y normalizada.

6. ¿Habrá roles dentro del hogar?  
   **Recomendación MVP:** roles mínimos o incluso invisibles salvo owner implícito.

7. ¿Cómo se manejará el borrado?  
   **Recomendación MVP:** soft delete en backend, oculto en UI normal.

---

## 27. Definición de MVP completado

Se considerará que el MVP está completado cuando:

1. dos usuarios pueden compartir un hogar real;
2. pueden crear listas de compra y tareas;
3. pueden añadir, editar, completar y eliminar ítems;
4. ambos ven los cambios de forma consistente;
5. la app es utilizable sin explicaciones continuas;
6. durante varios días de uso real resulta más cómoda que las alternativas actuales.

---

## 28. Recomendaciones estratégicas finales

1. **Mantener el alcance pequeño.**  
   Este producto puede crecer mucho, pero su valor inicial depende de ser rápido y estable.

2. **Obsesionarse con el flujo de crear ítems.**  
   Esa interacción decide gran parte del éxito.

3. **No convertirlo en un gestor de vida completa.**  
   El producto gana si se centra en el hogar compartido.

4. **Probarlo en vida real antes de ampliar.**  
   Las siguientes decisiones deben salir del uso diario, no de ideas atractivas en abstracto.

5. **Tratar el nombre “Smart Home” como provisional.**  
   Es válido para arrancar, pero puede ser demasiado genérico para una marca futura.

---

## 29. Resumen final del alcance MVP

El MVP de **Smart Home** debe ser una app móvil compartida para dos personas que conviven, con capacidad para:

- crear un hogar compartido;
- invitar a la otra persona;
- crear listas de compra y tareas;
- añadir y completar ítems;
- sincronizar cambios en tiempo real;
- mantener contexto básico sobre quién hizo qué;
- ofrecer una experiencia tan rápida que dé ganas de usarla de verdad.

Si eso funciona, el producto ya habrá demostrado su valor central.

---

## 30. Próximo entregable recomendado

Después de este documento, el siguiente entregable ideal sería uno de estos dos:

1. **Desglose del MVP en roadmap de implementación por tareas técnicas**, listo para agentes o ejecución manual.
2. **Especificación UX/UI de pantallas y estados**, con estructura detallada de cada vista y componentes.

