using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Prototipo3.Narrative;

namespace Prototipo3
{
    /// <summary>
    /// Enlaza los nodos del Grafo Narrativo No Lineal (StoryGraph) con las coordenadas reales de los
    /// 4 Edificios Importantes y asegura que todos los NPCs, Jefes Duales y Mecanismos estén presentes.
    /// </summary>
    public class StoryWorldBinder : MonoBehaviour
    {
        [Header("Prefabs de Personajes Opcionales")]
        public GameObject luluPrefab;
        public GameObject npcMarkerPrefab;
        public GameObject lockBarricadePrefab;
        public GameObject objectiveBeaconPrefab;

        public GameObject SpawnedLuluInstance { get; private set; }
        public LuluNPCPrototipo3 SpawnedLuluNPC { get; private set; }
        public DualRoleCharacter SpawnedMarco { get; private set; }
        public DualRoleCharacter SpawnedMorales { get; private set; }
        public DualRoleCharacter SpawnedSultan { get; private set; }
        public DualRoleCharacter SpawnedSalgado { get; private set; }
        public DualRoleCharacter SpawnedSofia { get; private set; }
        public DualRoleCharacter SpawnedClara { get; private set; }
        public DualRoleCharacter SpawnedKross { get; private set; }

        private readonly List<GameObject> spawnedWorldArtifacts = new List<GameObject>();

        public void BindGraphToWorld(StoryGraph graph, CityGeneratorPrototipo3 cityGen)
        {
            if (graph == null || cityGen == null) return;

            PopulateLandmarkActorsAndMechanisms(cityGen);

            foreach (StoryNode node in graph.nodes)
            {
                if (node == null) continue;
                node.worldPosition = cityGen.GetLandmarkPosition(node.landmarkId);
            }
        }

        public void PopulateLandmarkActorsAndMechanisms(CityGeneratorPrototipo3 cityGen)
        {
            if (cityGen == null) return;

            LimpiarArtefactos();

            // 1. PUNTO 0: Marco (Tejón Conserje - Mini-Boss Cap 1) en la calle adyacente a la Base
            Vector3 marcoPos = cityGen.GetLandmarkPosition("Marco");
            SpawnedMarco = EnsureCharacterExists(
                "Marco",
                "Marco",
                "Tejón Conserje • Mini-Boss Cap. 1",
                CharacterRoleType.DualCitizenFeral,
                BossProfileType.Marco_Tejon,
                marcoPos,
                cityGen.transform,
                new Color(0.35f, 0.32f, 0.30f),
                "Cof, cof... Hola, pequeño Kilo. Ese humo ámbar de la Torre Aethelgard me provoca jaquecas terribles al caer la tarde. Recoge chatarra de los contenedores y fabrica una herramienta en tu mesa antes de que anochezca.",
                "¡Kilo, perdóname! Anoche el humo me cegó de furia... Escuché que en la Escuela Bellavista y en el Bosque Ceniza buscan una forma de filtrar esta toxina."
            );

            // 2. ANILLO CERCANO (3-5): Escuela Secundaria Bellavista
            Transform escuelaRoot = cityGen.SpawnedEscuela != null ? cityGen.SpawnedEscuela.transform : cityGen.transform;
            Vector3 escPos = cityGen.GetLandmarkPosition("Escuela");

            EnsureCharacterExists(
                "Toby",
                "Toby",
                "Mapache • Taller de Robótica",
                CharacterRoleType.PureAlly,
                BossProfileType.None,
                escPos + new Vector3(-2.2f, 0f, 1.5f),
                escuelaRoot,
                new Color(0.45f, 0.48f, 0.52f),
                "¡Kilo! El Rector H. Morales tiene el oído de un búho real: si corres con [Shift] sin agacharte [C] te detectará de inmediato. Usa el Señuelo Acústico [Tecla 1] para distraerlo y espía su oficina antes de entrar al Laboratorio de Química.",
                ""
            );

            EnsureCharacterExists(
                "PerroGuardian",
                "Perro Guardián",
                "Mastín del Patio Escolar",
                CharacterRoleType.DualCitizenFeral,
                BossProfileType.None,
                escPos + new Vector3(2.6f, 0f, 2.8f),
                escuelaRoot,
                new Color(0.55f, 0.38f, 0.22f),
                "Guau... De día vigilo el patio de la Escuela Bellavista en paz, pero cuando cae el sol siento que algo me quema por dentro.",
                "Perdona los ladridos de anoche, amiguito... No sé qué nos hace ese humo."
            );

            SpawnedMorales = EnsureCharacterExists(
                "Morales",
                "Rector H. Morales",
                "Búho Real • \"El Silenciador\" (Boss 1)",
                CharacterRoleType.DualCitizenFeral,
                BossProfileType.Morales_Buho,
                escPos + new Vector3(0f, 0f, -1.2f),
                escuelaRoot,
                new Color(0.42f, 0.30f, 0.18f),
                "Silencio en los pasillos, joven Kilo. El laboratorio de química permanece bajo llave por orden de la corporación Aethelgard.",
                "¿Qué ocurrió durante el apagón? Mi cabeza da vueltas... El silencio debe mantenerse."
            );

            EnsureMechanismExists("Oficina_Morales", "Oficina del Rector Morales", StoryMechanismType.MoralesOfficeDesk, 1, escPos + new Vector3(2.1f, 0f, -0.5f), escuelaRoot);
            EnsureMechanismExists("Laboratorio_Quimica", "Laboratorio de Química (Vial de Enzima Base)", StoryMechanismType.SchoolChemistryLab, 1, escPos + new Vector3(-1.8f, 0f, -2.0f), escuelaRoot);

            // 3. ANILLO MEDIO (6-9, Cuadrante 1): Bosque Ceniza
            Transform bosqueRoot = cityGen.SpawnedBosque != null ? cityGen.SpawnedBosque.transform : cityGen.transform;
            Vector3 bosPos = cityGen.GetLandmarkPosition("Bosque");

            EnsureCharacterExists(
                "Moka",
                "Moka",
                "Zarigüeya Exploradora",
                CharacterRoleType.PureAlly,
                BossProfileType.None,
                bosPos + new Vector3(-2.4f, 0f, 2.0f),
                bosqueRoot,
                new Color(0.68f, 0.62f, 0.58f),
                "¡Psst, Kilo! Toma estas semillas de helecho para fabricar la Bomba de Polen [Trampa 2]. Cuando Sultán se vuelve feral de noche su olfato se multiplica x2.5, pero el polen lo deja sin rastro. Habla con él de día y repara los 3 postes del arroyo.",
                ""
            );

            EnsureCharacterExists(
                "CuervoHollin",
                "Cuervo de Hollín",
                "Vigía del Bosque Ceniza",
                CharacterRoleType.DualCitizenFeral,
                BossProfileType.None,
                bosPos + new Vector3(2.8f, 0f, 2.4f),
                bosqueRoot,
                new Color(0.18f, 0.18f, 0.22f),
                "¡Craa! Las cenizas del arroyo brillan cuando cae la noche...",
                "¡Craa! Mis plumas aún huelen a ozono tras la noche."
            );

            DualRoleCharacter sultan = EnsureCharacterExists(
                "Sultan",
                "Sultán",
                "Lobo Gris • \"Colmillo de Brasa\" (Boss 2)",
                CharacterRoleType.PacifiableBoss,
                BossProfileType.Sultan_Lobo,
                bosPos + new Vector3(0f, 0f, -1.0f),
                bosqueRoot,
                new Color(0.50f, 0.52f, 0.56f),
                "Kilo... ayúdame antes de que caiga la noche. Repara los 3 postes reflectores del arroyo usando Madera, Goma y Metal. Cuando me vuelva feral en la oscuridad, usa tu Bomba de Polen [2], atráeme a la subestación y enciende la luz fría.",
                "Perdóname por haber perdido el control anoche, Kilo... Aún quedan postes por reparar o falta encender el interruptor de luz fría de noche."
            );
            sultan.pacifiedDialogue = "Gracias por devolverme la cordura con la luz fría, Kilo. Toma el Filtro de Carbón Biológico y la llave del Hospital San Judas. ¡Estaré listo cuando ataques la Torre!";
            SpawnedSultan = sultan;

            EnsureMechanismExists("Poste_Reflector_1", "Poste Reflector del Arroyo #1", StoryMechanismType.ReflectorPole, 1, bosPos + new Vector3(-2.6f, 0f, -0.4f), bosqueRoot);
            EnsureMechanismExists("Poste_Reflector_2", "Poste Reflector del Arroyo #2", StoryMechanismType.ReflectorPole, 2, bosPos + new Vector3(0f, 0f, 2.4f), bosqueRoot);
            EnsureMechanismExists("Poste_Reflector_3", "Poste Reflector del Arroyo #3", StoryMechanismType.ReflectorPole, 3, bosPos + new Vector3(2.6f, 0f, -0.4f), bosqueRoot);
            EnsureMechanismExists("Interruptor_Subestacion", "Interruptor de Reflectores del Arroyo", StoryMechanismType.StreamSubstationSwitch, 1, bosPos + new Vector3(0f, 0f, -2.4f), bosqueRoot);

            // 4. ANILLO MEDIO (6-9, Cuadrante 2): Hospital General San Judas
            Transform hospRoot = cityGen.SpawnedHospital != null ? cityGen.SpawnedHospital.transform : cityGen.transform;
            Vector3 hospPos = cityGen.GetLandmarkPosition("Hospital");

            SpawnedSalgado = EnsureCharacterExists(
                "Salgado",
                "Dr. Arturo Salgado",
                "Tortuga Pediatra • Hospital San Judas",
                CharacterRoleType.PureAlly,
                BossProfileType.None,
                hospPos + new Vector3(-2.2f, 0f, 1.8f),
                hospRoot,
                new Color(0.28f, 0.55f, 0.34f),
                "¡Kilo! Con el Vial de Enzima Base y el Filtro de Carbón Biológico ya puedo preparar el nebulizador de Sofía en la Habitación 304. Solo falta que visites a Sofía, hables con la enfermera Clara y traigas el Cilindro de Oxígeno Puro del quirófano.",
                ""
            );

            SpawnedSofia = EnsureCharacterExists(
                "Sofia",
                "Sofía",
                "Conejita Albina • Habitación 304",
                CharacterRoleType.PureAlly,
                BossProfileType.None,
                hospPos + new Vector3(-2.2f, 0f, -1.6f),
                hospRoot,
                new Color(0.95f, 0.92f, 0.94f),
                "Kilo... sabía que vendrías por mí. No dejes que la enfermera Clara te atrape con su gas anestésico en el quirófano.",
                ""
            );

            EnsureCharacterExists(
                "CamilleroJabali",
                "Camillero Jabalí",
                "Guardia de Urgencias",
                CharacterRoleType.DualCitizenFeral,
                BossProfileType.None,
                hospPos + new Vector3(2.5f, 0f, 2.2f),
                hospRoot,
                new Color(0.45f, 0.32f, 0.26f),
                "El turno de urgencias es agotador... No te acerques a los pasillos cerrados cuando anochezca.",
                "Uf... otra noche de pesadillas en el pabellón quirúrgico."
            );

            DualRoleCharacter clara = EnsureCharacterExists(
                "Clara",
                "Clara Valdés",
                "Cisne Negro • \"La Dama de Gas\" (Boss 3)",
                CharacterRoleType.PacifiableBoss,
                BossProfileType.Clara_Cisne,
                hospPos + new Vector3(1.8f, 0f, -1.0f),
                hospRoot,
                new Color(0.15f, 0.15f, 0.20f),
                "Los cilindros de oxígeno están en el quirófano, pequeño Kilo. Pero mis válvulas de anestesia tienen fugas cuando oscurece... Si pierdo la razón, enciérrame en la Cámara Hiperbárica con una descarga estática sin lastimarme.",
                "Perdona el gas anestésico en los pasillos... La cámara hiperbárica es lo único que puede contenerme."
            );
            clara.pacifiedDialogue = "La presión hiperbárica despejó la toxina de mis pulmones. Toma el Cilindro de Oxígeno Puro y salva a Sofía, Kilo.";
            SpawnedClara = clara;

            EnsureMechanismExists("Valvula_Hiperbarica", "Válvula de la Cámara Hiperbárica", StoryMechanismType.HyperbaricChamberValve, 1, hospPos + new Vector3(2.0f, 0f, -2.2f), hospRoot);
            EnsureMechanismExists("Cilindro_Oxigeno", "Cilindro de Oxígeno Puro", StoryMechanismType.PureOxygenCylinder, 1, hospPos + new Vector3(0f, 0f, -2.4f), hospRoot);

            // 5. ANILLO LEJANO (10+): Torre Corporativa Aethelgard
            Transform torreRoot = cityGen.SpawnedTorre != null ? cityGen.SpawnedTorre.transform : cityGen.transform;
            Vector3 torrePos = cityGen.GetLandmarkPosition("Torre");

            EnsureCharacterExists(
                "Vane",
                "Agente Vane",
                "Zorro Ártico • Desertor de ECLIPSE",
                CharacterRoleType.PureAlly,
                BossProfileType.None,
                torrePos + new Vector3(-2.2f, 0f, 2.4f),
                torreRoot,
                new Color(0.88f, 0.92f, 0.98f),
                "Kilo, el Dr. Julián Kross se encerró en el reactor superior. Toby, Moka y Sultán están distrayendo a las patrullas ECLIPSE. Entra, libera a la Dra. Elena Vargas para recibir el Suero Alba-Puro y apaga los 3 Condensadores UV de Kross.",
                ""
            );

            EnsureCharacterExists(
                "HuronEclipse",
                "Hurón ECLIPSE",
                "Centinela Corporativo",
                CharacterRoleType.DualCitizenFeral,
                BossProfileType.None,
                torrePos + new Vector3(2.5f, 0f, 2.2f),
                torreRoot,
                new Color(0.30f, 0.34f, 0.40f),
                "Acceso restringido a personal del Proyecto Helios-Omega.",
                "Sistemas ópticos reiniciándose tras el ciclo nocturno..."
            );

            EnsureCharacterExists(
                "Elena",
                "Dra. Elena Vargas",
                "Nutria Bioquímica • Creadora del Suero Alba-Puro",
                CharacterRoleType.PureAlly,
                BossProfileType.None,
                torrePos + new Vector3(-2.2f, 0f, -1.6f),
                torreRoot,
                new Color(0.52f, 0.36f, 0.24f),
                "¡Kilo, lograste llegar! Aquí tienes el Suero Alba-Puro. El exoesqueleto de Kross se alimenta de los 3 Condensadores Fotónicos UV: sobrecárgalos con tu Descargador Estático [3] o con [F] e inserta el suero en el Inyector Central.",
                ""
            );

            DualRoleCharacter kross = EnsureCharacterExists(
                "Kross",
                "Dr. Julián Kross",
                "Murciélago Albino • \"El Heraldo Solar\" (Boss Final)",
                CharacterRoleType.PacifiableBoss,
                BossProfileType.Kross_Murcielago,
                torrePos + new Vector3(0f, 0f, -0.8f),
                torreRoot,
                new Color(0.90f, 0.85f, 0.95f),
                "¡La evolución feral es inevitable, pequeño roedor! ¡Mis 3 Condensadores Fotónicos UV harán que la noche sea eterna en Solis!",
                "¡Mis condensadores fotónicos mantienen mi sobrecarga día y noche!"
            );
            kross.pacifiedDialogue = "Mi exoesqueleto... apagado... El Suero Alba-Puro está disipando el gas Helios-Omega de toda la ciudad...";
            SpawnedKross = kross;

            EnsureMechanismExists("Celda_Dra_Elena", "Celda de Contención - Dra. Elena Vargas", StoryMechanismType.ElenaReactorCell, 1, torrePos + new Vector3(-2.0f, 0f, -1.2f), torreRoot);
            EnsureMechanismExists("Condensador_UV_1", "Condensador Fotónico UV #1", StoryMechanismType.PhotonicUVCapacitor, 1, torrePos + new Vector3(-2.6f, 0f, 0.5f), torreRoot);
            EnsureMechanismExists("Condensador_UV_2", "Condensador Fotónico UV #2", StoryMechanismType.PhotonicUVCapacitor, 2, torrePos + new Vector3(2.6f, 0f, 0.5f), torreRoot);
            EnsureMechanismExists("Condensador_UV_3", "Condensador Fotónico UV #3", StoryMechanismType.PhotonicUVCapacitor, 3, torrePos + new Vector3(2.2f, 0f, -2.0f), torreRoot);
            EnsureMechanismExists("Inyector_Central", "Inyector Central de la Chimenea Aethelgard", StoryMechanismType.CentralSerumInjector, 1, torrePos + new Vector3(0f, 0f, -2.5f), torreRoot);
        }

        private DualRoleCharacter EnsureCharacterExists(
            string charId,
            string displayName,
            string speciesTitle,
            CharacterRoleType roleType,
            BossProfileType bossProfile,
            Vector3 worldPos,
            Transform parent,
            Color bodyColor,
            string dayDialogue,
            string apologyDialogue)
        {
            for (int i = 0; i < DualRoleCharacter.ActiveCharacters.Count; i++)
            {
                DualRoleCharacter existing = DualRoleCharacter.ActiveCharacters[i];
                if (existing != null && string.Equals(existing.characterId, charId, StringComparison.OrdinalIgnoreCase))
                {
                    existing.transform.position = worldPos;
                    existing.SetHomeAnchor(worldPos);
                    return existing;
                }
            }

            GameObject go = new GameObject($"Actor_{charId}_{displayName}");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;

            // Contenedor estándar VisualMesh_ReplaceMe solicitado en PASO 7
            GameObject visualContainer = new GameObject("VisualMesh_ReplaceMe");
            visualContainer.transform.SetParent(go.transform, false);
            visualContainer.transform.localPosition = Vector3.zero;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "PlaceholderBody";
            body.transform.SetParent(visualContainer.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            body.transform.localScale = new Vector3(0.65f, 0.6f, 0.65f);
            Collider bodyCol = body.GetComponent<Collider>();
            if (bodyCol != null) Destroy(bodyCol);

            Renderer ren = body.GetComponent<Renderer>();
            if (ren != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
                Material m = new Material(sh) { color = bodyColor };
                ren.material = m;
            }

            CapsuleCollider col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.6f, 0f);
            col.radius = 0.4f;
            col.height = 1.2f;

            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1.2f;
            agent.baseOffset = 0f;

            EnemyAIPrototipo3 ai = go.AddComponent<EnemyAIPrototipo3>();
            ai.bossProfile = bossProfile;

            EnemyOnCollision onCol = go.AddComponent<EnemyOnCollision>();
            onCol.enabled = false;

            DualRoleCharacter drc = go.AddComponent<DualRoleCharacter>();
            drc.characterId = charId;
            drc.displayName = displayName;
            drc.speciesTitle = speciesTitle;
            drc.roleType = roleType;
            drc.bossProfile = bossProfile;
            drc.dayDialogue = dayDialogue;
            drc.postNightApologyDialogue = apologyDialogue;
            drc.SetHomeAnchor(worldPos);
            drc.ApplyBossProfileConfiguration();
            drc.EvaluateCycleStateImmediate();

            spawnedWorldArtifacts.Add(go);
            return drc;
        }

        private StoryMechanismPrototipo3 EnsureMechanismExists(
            string mechId,
            string displayName,
            StoryMechanismType type,
            int indexNumber,
            Vector3 worldPos,
            Transform parent)
        {
            for (int i = 0; i < StoryMechanismPrototipo3.ActiveMechanisms.Count; i++)
            {
                StoryMechanismPrototipo3 existing = StoryMechanismPrototipo3.ActiveMechanisms[i];
                if (existing != null && string.Equals(existing.mechanismId, mechId, StringComparison.OrdinalIgnoreCase))
                {
                    existing.transform.position = worldPos;
                    return existing;
                }
            }

            GameObject go = new GameObject($"Mechanism_{mechId}");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.tag = "Interactable";

            GameObject visualContainer = new GameObject("VisualMesh_ReplaceMe");
            visualContainer.transform.SetParent(go.transform, false);

            PrimitiveType prim = type == StoryMechanismType.ReflectorPole ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            GameObject mesh = GameObject.CreatePrimitive(prim);
            mesh.name = "MechanismModel";
            mesh.transform.SetParent(visualContainer.transform, false);
            mesh.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            mesh.transform.localScale = type == StoryMechanismType.ReflectorPole
                ? new Vector3(0.35f, 1.1f, 0.35f)
                : new Vector3(0.9f, 0.8f, 0.9f);
            Collider c = mesh.GetComponent<Collider>();
            if (c != null) Destroy(c);

            StoryMechanismPrototipo3 mech = go.AddComponent<StoryMechanismPrototipo3>();
            mech.mechanismId = mechId;
            mech.displayName = displayName;
            mech.mechanismType = type;
            mech.indexNumber = indexNumber;

            spawnedWorldArtifacts.Add(go);
            return mech;
        }

        public void DesactivarBarricadaLock()
        {
            for (int i = 0; i < spawnedWorldArtifacts.Count; i++)
            {
                if (spawnedWorldArtifacts[i] != null && spawnedWorldArtifacts[i].name == "StoryLock_Barricada")
                {
                    spawnedWorldArtifacts[i].SetActive(false);
                }
            }
        }

        public void LimpiarArtefactos()
        {
            for (int i = 0; i < spawnedWorldArtifacts.Count; i++)
            {
                if (spawnedWorldArtifacts[i] != null)
                {
                    if (Application.isPlaying) Destroy(spawnedWorldArtifacts[i]);
                    else DestroyImmediate(spawnedWorldArtifacts[i]);
                }
            }
            spawnedWorldArtifacts.Clear();
            SpawnedLuluInstance = null;
            SpawnedLuluNPC = null;
        }
    }
}
