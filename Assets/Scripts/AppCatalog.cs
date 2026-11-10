using System;
using UnityEngine;

[Serializable]
public class EventData
{
    public string id;
    public string title;
    public string shortDescription;
    public string longDescription;
    public string badge;          // "Recomendado" / "Popular" / "Nuevo" / vacio
    public string spriteResource; // ruta dentro de Resources

    // Datos puntuales del evento. OJO: son valores de ejemplo, reemplazalos por los reales.
    public string date;
    public string time;
    public string location;
    public string audience;
    public string organizer;
    public string[] highlights;

    private Sprite cachedSprite;
    public Sprite Sprite
    {
        get
        {
            if (cachedSprite == null) cachedSprite = Resources.Load<Sprite>(spriteResource);
            return cachedSprite;
        }
    }

    /// <summary>
    /// Ficha completa del evento con formato TMP, para los modales de informacion.
    /// Se arma aca para que las pantallas 2 y 4 muestren exactamente lo mismo.
    /// </summary>
    public string DetailsText
    {
        get
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(longDescription).Append("\n\n");

            AppendRow(sb, "Fecha", date);
            AppendRow(sb, "Hora", time);
            AppendRow(sb, "Lugar", location);
            AppendRow(sb, "Dirigido a", audience);
            AppendRow(sb, "Organiza", organizer);

            if (highlights != null && highlights.Length > 0)
            {
                sb.Append("\n<b>Qué vas a encontrar</b>\n");
                foreach (var item in highlights)
                {
                    sb.Append("•  ").Append(item).Append('\n');
                }
            }

            return sb.ToString().TrimEnd();
        }
    }

    private static void AppendRow(System.Text.StringBuilder sb, string label, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        sb.Append("<b>").Append(label).Append(":</b>  ").Append(value).Append('\n');
    }
}

[Serializable]
public class CharacterData
{
    public string id;             // coincide con el nombre del GameObject / modelo
    public string displayName;
    public string tagline;
    public string description;
    public string[] tags;
    public string logoResource;   // ruta dentro de Resources
    public string modelResource;  // ruta del modelo 3D dentro de Resources (vacio = ya esta en escena)
    public Color accent = new Color(0.16f, 0.50f, 0.95f, 1f); // tono de sus caracteristicas en la galeria
    // Skins del panel lateral de la galeria, en el mismo orden que las texturas de
    // Assets/Textures/<id>/<id>_texture_NN_<nombre> (la 01 es la Base).
    // Miniatura opcional en Resources/UI/skins/<id>_<skin>; si no existe, se usa un recorte
    // de la textura de la skin.
    public string[] skins = { "Base" };

    private Sprite cachedLogo;
    public Sprite Logo
    {
        get
        {
            if (cachedLogo == null) cachedLogo = Resources.Load<Sprite>(logoResource);
            return cachedLogo;
        }
    }
}

/// <summary>
/// Catalogo de eventos y personajes. Se mantiene como datos en codigo (no ScriptableObjects)
/// para que no haya que crear ni cablear assets a mano en el Inspector.
/// </summary>
public static class AppCatalog
{
    public static readonly string[] Careers =
    {
        "Administración",
        "Economía",
        "Ingeniería",
        "Derecho",
        "Psicología",
        "Marketing",
        "Otra"
    };

    /// <summary>
    /// IMPORTANTE: fechas, horarios y lugares son datos de EJEMPLO para poder ver la ficha
    /// completa funcionando. Reemplazalos por los reales antes de publicar.
    /// </summary>
    public static readonly EventData[] Events =
    {
        new EventData
        {
            id = "ciencia",
            title = "Feria de Ciencia",
            shortDescription = "Descubre proyectos, ideas y experimentos.",
            longDescription = "Recorre los proyectos de investigación de los estudiantes, conoce a " +
                              "los equipos detrás de cada uno y participa en demostraciones en vivo.",
            badge = "Popular",
            spriteResource = "UI/evento_ciencia",
            date = "Martes 13 de mayo",
            time = "09:00 – 14:00",
            location = "Explanada central y Pabellón B",
            audience = "Entrada libre para toda la comunidad",
            organizer = "Facultad de Ciencias",
            highlights = new[]
            {
                "Más de 30 proyectos de investigación en exhibición",
                "Demostraciones en vivo cada 30 minutos",
                "Concurso de pósters con premiación a las 13:30",
                "Zona de experimentos abierta al público"
            }
        },
        new EventData
        {
            id = "tecnologia",
            title = "Charla de Tecnología",
            shortDescription = "Aprende de expertos y explora nuevas tendencias.",
            longDescription = "Bloques cortos de charlas sobre innovación, datos e inteligencia " +
                              "artificial, con espacio para preguntas al cierre de cada uno.",
            badge = "Recomendado",
            spriteResource = "UI/evento_tecnologia",
            date = "Miércoles 14 de mayo",
            time = "16:00 – 18:30",
            location = "Auditorio Principal",
            audience = "Estudiantes de todas las carreras",
            organizer = "Facultad de Ingeniería",
            highlights = new[]
            {
                "3 charlas de 40 minutos con ronda de preguntas",
                "Casos reales de IA aplicada a la industria",
                "Espacio de networking al finalizar",
                "Cupos limitados: llega 15 minutos antes"
            }
        },
        new EventData
        {
            id = "deporte",
            title = "Jornada Deportiva",
            shortDescription = "Compite, entrena y suma puntos para tu facultad.",
            longDescription = "Torneos rápidos, retos por equipos y actividades abiertas en las " +
                              "canchas del campus durante todo el día.",
            badge = "Nuevo",
            spriteResource = "UI/evento_deporte",
            date = "Jueves 15 de mayo",
            time = "08:00 – 17:00",
            location = "Complejo deportivo",
            audience = "Inscripción por equipos de 5 personas",
            organizer = "Bienestar Universitario",
            highlights = new[]
            {
                "Torneos relámpago de fútbol, vóley y básquet",
                "Retos individuales con premios por facultad",
                "Hidratación y kit deportivo incluidos",
                "Trae ropa deportiva y tu carné universitario"
            }
        },
        new EventData
        {
            id = "debate",
            title = "Torneo de Debate",
            shortDescription = "Argumenta, escucha y defiende tus ideas.",
            longDescription = "Rondas de debate entre facultades con jurado docente. Puedes " +
                              "participar como orador o asistir como público.",
            badge = "",
            spriteResource = "UI/evento_debate",
            date = "Viernes 16 de mayo",
            time = "15:00 – 19:00",
            location = "Sala de Conferencias, Pabellón A",
            audience = "Participantes inscritos y público general",
            organizer = "Facultad de Derecho",
            highlights = new[]
            {
                "Rondas eliminatorias desde las 15:00",
                "Jurado docente y retroalimentación en vivo",
                "Final abierta al público a las 18:00",
                "Temas de actualidad entregados 20 minutos antes"
            }
        }
    };

    /// <summary>
    /// El id coincide con el nombre del GameObject en la escena (o del modelo en Resources).
    /// "Character" es el personaje original ya animado; los otros 4 estan en stand-by.
    /// </summary>
    public static readonly CharacterData[] Characters =
    {
        new CharacterData
        {
            id = "Character",
            displayName = "Sombra",
            tagline = "Misterioso y directo",
            description = "Tu guía original, listo para llevarte a tu evento.",
            tags = new[] { "Exploración", "Orientación", "Comunidad" },
            logoResource = "UI/logo_shadow",
            accent = new Color(0.55f, 0.36f, 0.90f, 1f), // violeta
            skins = new[] { "Base", "Cyber", "Galaxia", "Samurái", "Camuflaje", "Imperial" },
            modelResource = ""
        },
        new CharacterData
        {
            id = "wolf",
            displayName = "Lobo",
            tagline = "Curioso y amigable",
            description = "Tu guía para explorar el campus y llegar a tu evento.",
            tags = new[] { "Exploración", "Orientación", "Comunidad" },
            logoResource = "UI/logo_wolf",
            accent = new Color(0.16f, 0.50f, 0.95f, 1f), // azul
            skins = new[] { "Base", "Cósmico", "Tóxico", "Explorador", "Neón", "Bosque" },
            modelResource = "Characters/wolf"
        },
        new CharacterData
        {
            id = "bull",
            displayName = "Toro",
            tagline = "Fuerte y decidido",
            description = "Te empuja a no quedarte quieto y vivir cada actividad.",
            tags = new[] { "Energía", "Constancia", "Equipo" },
            logoResource = "UI/logo_bull",
            accent = new Color(0.86f, 0.25f, 0.22f, 1f), // rojo
            skins = new[] { "Base", "Ártico", "Berserker", "Real", "Futurista", "Infernal" },
            modelResource = "Characters/bull"
        },
        new CharacterData
        {
            id = "lion",
            displayName = "León",
            tagline = "Seguro y líder",
            description = "Te acompaña con carácter hacia los puntos clave del campus.",
            tags = new[] { "Liderazgo", "Confianza", "Impacto" },
            logoResource = "UI/logo_lion",
            accent = new Color(0.93f, 0.58f, 0.13f, 1f), // naranja
            skins = new[] { "Base", "Ártico", "Berserker", "Real", "Futurista", "Infernal" },
            modelResource = "Characters/lion"
        },
        new CharacterData
        {
            id = "viking",
            displayName = "Vikingo",
            tagline = "Aventurero y leal",
            description = "Convierte el recorrido en una expedición por la universidad.",
            tags = new[] { "Aventura", "Disciplina", "Historia" },
            logoResource = "UI/logo_viking",
            accent = new Color(0.18f, 0.68f, 0.38f, 1f), // verde
            skins = new[] { "Base", "Ártico", "Berserker", "Real", "Futurista", "Infernal" },
            modelResource = "Characters/viking"
        }
    };

    public static EventData GetEvent(string id)
    {
        foreach (var e in Events)
        {
            if (e.id == id) return e;
        }
        return Events[0];
    }

    public static int GetEventIndex(string id)
    {
        for (int i = 0; i < Events.Length; i++)
        {
            if (Events[i].id == id) return i;
        }
        return 0;
    }

    public static CharacterData GetCharacter(string id)
    {
        foreach (var c in Characters)
        {
            if (c.id == id) return c;
        }
        return Characters[0];
    }

    public static int GetCharacterIndex(string id)
    {
        for (int i = 0; i < Characters.Length; i++)
        {
            if (Characters[i].id == id) return i;
        }
        return 0;
    }
}
