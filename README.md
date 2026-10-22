# 🥊 Dolondrazo: La Entrega Final
> **Videojuego 2D Pixel Art estilo Arcade Beat 'em Up ambientado en Dolores Hidalgo, Cuna de la Independencia Nacional, Guanajuato.**

![Unity 6](https://img.shields.io/badge/Unity-6000.3.24f1%20LTS-black?style=for-the-badge&logo=unity)
![Render Pipeline](https://img.shields.io/badge/URP-Universal%202D-blue?style=for-the-badge)
![Input System](https://img.shields.io/badge/Input%20System-New%20Package-green?style=for-the-badge)
![Escuela](https://img.shields.io/badge/CBTis%2075-Dolores%20Hidalgo-orange?style=for-the-badge)

---

## 📖 Sinopsis del Juego
Es el último día hábil del semestre en el **CBTis 75**. **Fercho**, un estudiante de mecatrónica, debe entregar su maqueta y proyecto escolar antes de las **2:00 PM** para salvar el semestre.

Tras desvelarse hasta las 3:00 AM terminando la maqueta, Fercho llega a la parada de autobuses y descubre que las rutas están en huelga. Al intentar pedir ayuda, su celular se apaga con el 0% de batería. Sin transporte ni comunicación, su única opción es cruzar la ciudad a pie.

Sin embargo, al llegar a la emblemática **Calzada de los Héroes**, grupos y pandillas de escuelas rivales (**CECyTE**, **SABES** y **Rébsamen**) han bloqueado las calles y juraron que nadie del CBTis pasará. ¡Fercho tendrá que abrirse paso a golpes para salvar su semestre!

---

## 🎮 Controles del Jugador (Teclado)

| Acción | Teclas | Descripción |
| :--- | :--- | :--- |
| **Moverse** | A / D o ← / → | Caminar / correr por el escenario |
| **Combo de Golpes** | Z / J o Click Izquierdo | Combo triple de golpes (Jab, Cross y Patada alta) |
| **Saltar** | Espacio | Salto acrobático vertical para esquivar ataques y saltar cajas |
| **Pausar** | Escape o P | Abre el menú de pausa dentro del juego |
| **Avanzar Diálogos** | Espacio / Enter / Z | Avanzar viñetas de cómic y diálogos RPG |

---

## 🌟 Características Implementadas
- **Nivel 1 (Calzada de los Héroes):** Escenario colonial con fondo detallado de Dolores Hidalgo.
- **Sistema de Oleadas:** 3 oleadas progresivas de estudiantes rivales con inteligencia artificial, patrullaje y ataques.
- **Jefe de Nivel:** «Alumno de Taller» con barra de salud segmentada y patrones de combate.
- **Historieta Narrativa (Cómic):** Prólogo interactivo con viñetas ilustradas y música épica (Intro.mp3).
- **Sistema de Diálogos RPG:** Diálogos con efecto máquina de escribir, retratos de personajes y formato narrativo.
- **Audio Manager Persistente:** Control de volumen Master, Música y Efectos de Sonido mediante PlayerPrefs.
- **Objetos Destructibles:** Cajas de madera con suministro de alimentos para restaurar la batería de vida.
- **Menú Principal Arcade:** Fondo nocturno pixel art, expediente de misión, marcador de récord histórico y opciones.

---

## 🛠️ Tecnologías y Requisitos
- **Motor:** Unity 6 (6000.3.24f1 LTS)
- **Render Pipeline:** Universal Render Pipeline (URP 2D)
- **Input:** Unity New Input System Package (UnityEngine.InputSystem)
- **Resolución de Referencia:** 1920x1080 (Full HD, 16:9)

---

## 📁 Estructura del Repositorio
`
Dolondrazo_Videogame/
├── Assets/
│   ├── Animaciones/       # Controladores y clips de animación (Fercho, Rivales, Jefe)
│   ├── Resources/         # Fuentes Arcade, música (Intro.mp3, SFX) y sprites dinámicos
│   ├── Scenes/            # Escenas del juego (Pantalla_Inicio, Nivel_1_Calzada)
│   ├── Scripts/           # Código fuente C# (PlayerController, EnemyAI, BossAI, UI, etc.)
│   ├── Sounds/            # Banda sonora y efectos de sonido arcade
│   └── Sprites/           # Spritesheets y fondos pixel art
├── Docs/                  # GDD (Game Design Document), Storyboard y documentos de diseño
├── Packages/              # Manifiesto de paquetes de Unity
├── ProjectSettings/       # Configuración de Input, Gráficos y Tags de Unity
├── .gitignore             # Configuración de exclusión para Unity
└── README.md              # Documentación del proyecto
`

---
© 2026 **Dolondrazo Project** — CBTis 75 Dolores Hidalgo, Gto.
