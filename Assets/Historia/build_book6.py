import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from novel_styler import (
    create_novel_document, add_book_title, add_epigraph,
    add_chapter_heading, add_prose_paragraph, add_dialogue,
    add_divider, save_novel
)

def build_book_6():
    doc = create_novel_document()
    
    add_book_title(
        doc,
        title="SINFONÍA DE CENIZA, HUESO Y HUMO",
        subtitle="La Gran Novela de Malvoro: La Epopeya de los Cinco Heraldos",
        author="Volumen VI — La Obra Cumbre de la Metrópoli Feral"
    )
    
    add_epigraph(
        doc,
        "Hubo un tiempo en que los hombres creyeron ser los dueños del trueno, del fuego y de las criaturas que reptaban bajo sus pies. Mezclaron el veneno del tabaco con las raíces de la tierra profunda y las voces del éter, soñando con un imperio de bestias obedientes. Pero el asfalto guarda memoria de cada gota de sangre. Cuando la gran noche cayó, solo aquellos que sabían caminar sin hacer ruido heredaron las ruinas del templo.",
        "Crónicas Fundacionales de la Madriguera de Bronce"
    )

    # -------------------------------------------------------------
    # PRÓLOGO
    # -------------------------------------------------------------
    add_chapter_heading(doc, "Prólogo: La Noche del Fuego Blanco", "El Génesis de la Gran Catástrofe")
    
    add_prose_paragraph(
        doc,
        "Todo comenzó con un pacto de soberbia en el piso cuarenta y cuatro de la Torre Malvoro. Durante tres décadas, la multinacional tabacalera no solo dominó el mercado de cigarrillos del hemisferio; financió en el subsuelo más profundo el Proyecto Arca, una división de investigación bio-militar liderada por el Dr. Alistair Finch y supervisada por el magnate Helmut Voss. Los directores buscaban el compuesto definitivo: un aditivo sintético capaz de generar una adicción irreversible a nivel celular y neutralizar cualquier brote de insumisión humana o animal."
    )
    add_prose_paragraph(
        doc,
        "El destino, sin embargo, cruzó sus ambiciones con fuerzas que escapaban a la química convencional. Durante las excavaciones subterráneas del metro, los ingenieros desenterraron un fragmento de roca meteórica de color púrpura fosforescente que albergaba esporas latentes de un hongo milenario: *Phalloides Aeterna*. Finch combinó las hifas carnívoras del hongo con el alcaloide C-Zero en los filtros de cigarrillo, mientras las antenas de telecomunicaciones de la metrópoli eran calibradas a cuarenta y siete hercios para mantener a los organismos en una sincronía cerebral dócil."
    )
    add_prose_paragraph(
        doc,
        "Pero la naturaleza no admite cadenas de titanio. La noche del diecisiete de octubre, un rayo globular impactó en los condensadores de la refinería química. El Reactor Carmesí colapsó en una llamarada de calor blanco que pulverizó los filtros de seguridad. Millones de toneladas de alquitrán mutagenizado, esporas carnívoras y vapor de cuarenta y siete hercios se derramaron por los colectores de la ciudad."
    )
    add_prose_paragraph(
        doc,
        "En cuestión de horas, los humanos fueron borrados del mapa: sus cuerpos se deshidrataron molecularmente bajo la frecuencia armónica, convirtiéndose en montículos de sal blanca en las aceras. Y la fauna entera —desde los ratones de alcantarilla hasta los ciervos del parque y los tigres del zoológico— inhaló el aliento del veneno. La gran inversión zoológica había comenzado: todos los animales, sin importar si eran herbívoros, omnívoros o carnívoros, sucumbieron a una feralidad homicida y sangrienta. Todos... salvo un puñado minúsculo que retuvo la chispa sagrada de la razón."
    )

    # -------------------------------------------------------------
    # ACTO I
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Acto I: El Cónclave de la Madriguera de Bronce", "Donde los cinco supervivientes unen sus heridas y sus secretos")
    
    add_prose_paragraph(
        doc,
        "Bajo los cimientos de la vieja estación de ferrocarril, en un sótano blindado con planchas de zinc y sacos de arena, ardía un fuego diminuto dentro de un cenicero de bronce. En torno a esa llama temblorosa se habían reunido los cinco heraldos de la cordura, convocados por las señales luminosas que habían cruzado los tejados durante las noches de asedio."
    )
    add_prose_paragraph(
        doc,
        "Allí estaba **Barnaby**, el veterano hámster de las cloacas, con su púa de erizo al hombro y los ojos entornados por la desconfianza; a su lado, **Silas**, el alquimista botánico del invernadero, con su lupa de relojero y su zurrón de semillas puras; frente a ellos, el sabio anciano **Dr. Vane**, con sus cascos de cobre amortiguadores de frecuencia, recostado sobre el lomo musculoso del **Capitán Fang**, el imponente bóxer militar cuya sordera lo convertía en el único mastín inmune a la locura del éter."
    )
    add_prose_paragraph(
        doc,
        "Y en la esquina más sombría del refugio reposaba **Caspian**, el Sujeto 042, también conocido en los callejones como **Nox**, el hámster del farolillo de fósforo blanco cuya mente hiperaumentada por implantes de fibra óptica cargaba con la terrible sospecha de ser el vector involuntario que atraía a las hordas."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "Estamos perdiendo la ciudad, metro a metro. Los ciervos de Kaelan han tomado la rotonda; sus pezuñas han triturado a las jaurías de perros del norte y las liebres carnívoras patrullan los tejados. Si no destruimos el reactor de la Torre Malvoro, la niebla terminará asimilando hasta la última piedra.",
        "Barnaby clavó su aguja en el mapa de papel de periódico extendido sobre el suelo."
    )
    add_dialogue(
        doc,
        "Silas",
        "El reactor no es una caldera ordinaria, Barnaby. Es el corazón de un micelio gigante. Kaelan no actúa por instinto de presa; sus astas son las antenas que recogen las órdenes de los humanos que duermen en el bulbo amniótico subterráneo. Si apagamos el fuego sin neutralizar las raíces con azufre y resina, el hongo liberará una marea de esporas negras que asfixiará a todos los que aún respiramos.",
        "Silas mostró una probeta con líquido esmeralda."
    )
    add_dialogue(
        doc,
        "Dr. Vane",
        "Y no olviden las antenas en los rascacielos. Cada vez que intentamos coordinar una salida, la Frecuencia 47 satura los tímpanos. La única razón por la que el Capitán Fang y yo seguimos cuerdos es que él no la escucha y yo la disipo con mis bobinas. Pero si entramos al perímetro de la torre, el Heraldo Mecánico nos freirá los nervios antes de llegar a la puerta.",
        "Vane ajustó el dial de sus cascos."
    )
    add_prose_paragraph(
        doc,
        "Una risa suave y cristalina cortó la discusión. Desde las sombras de la viga superior descendió **Lulú**. La gata siamesa albina, con su andar serpentino y sus ojos violetas cargados de misterio, aterrizó entre ellos sin hacer crujir un solo grano de arena. Entre sus fauces sostenía la llave maestra de titanio con el sello grabado de Malvoro."
    )
    add_dialogue(
        doc,
        "Lulú",
        "Tienen las piezas del rompecabezas, queridos, pero son demasiado cobardes para armarlo. Ustedes ven una catástrofe; yo veo un trono vacío. ¿Por qué quieren apagar el reactor cuando podemos reprogramarlo para que las bestias solo se arrodillen ante nuestra voluntad?",
        "Lulú acarició con su cola el arnés del Capitán Fang, quien gruñó sordamente en advertencia."
    )
    add_dialogue(
        doc,
        "Caspian",
        "Porque la dominación fue lo que destruyó a los humanos, Lulú. Si usamos su máquina para mandar sobre las fieras, nos convertiremos exactamente en los monstruos que nos encerraron en tubos de cristal.",
        "Caspian alzó su farolillo de fósforo blanco, cuya luz limpia hizo retroceder a la gata."
    )

    # -------------------------------------------------------------
    # ACTO II
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Acto II: La Gran Marcha por las Siete Avenidas", "Trampas colosales, niebla direccional y la caída de la retaguardia")
    
    add_prose_paragraph(
        doc,
        "La expedición hacia la Torre Malvoro comenzó a la hora más oscura de la noche, cuando la temperatura descendía hasta congelar el agua en las cunetas y la niebla se volvía más espesa y hostil. Para dondequiera que miraba Caspian, la niebla se densificaba en una muralla de algodón gris donde flotaban ojos amarillos incandescentes. Pero ahora los cinco no estaban desarmados: marchaban en una formación de combate táctico nunca antes vista en la historia animal."
    )
    add_prose_paragraph(
        doc,
        "El Capitán Fang abría la marcha como un ariete blindado, llevando en su arnés dos potentes faros halógenos alimentados por una batería de litio que Caspian había adaptado. Sentados en cestas laterales sobre los hombros del mastín, Barnaby y Vane montaban guardia: Barnaby disparaba proyectiles incendiarios de fósforo con una ballesta micrométrica construida con resortes de pinzas de freno, mientras Vane utilizaba un megáfono sónico direccional para repeler a las bandadas de palomas enloquecidas del Coro del Éter."
    )
    add_prose_paragraph(
        doc,
        "Silas cubría la retaguardia montando trampas botánicas instantáneas: sembraba cápsulas de arcilla rellenas de resina de pino y ácido de batería que, al ser pisadas por las liebres carnívoras que los perseguían en jaurías, detonaban en una espuma viscosa e inflamable que inmovilizaba a las bestias sin matarlas."
    )
    add_prose_paragraph(
        doc,
        "Al cruzar el Viaducto de la Séptima Avenida, la niebla se abrió con un trueno espantoso. Una emboscada de caninos ferales de cristal, liderados por el temible Cazador de Cuero —el tigre albino biomecánico que había sido reconstruido por los protocolos automáticos de la estación— bloqueó ambos extremos del puente colapsado."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "¡Nos tienen rodeados! ¡El puente se está partiendo bajo el peso del tigre!",
        "gritó Barnaby mientras tensaba su cuerda de ballesta."
    )
    add_dialogue(
        doc,
        "Capitán Fang",
        "(Ladrido de combate)",
        "el gran bóxer empujó con su hocico a los tres roedores hacia la cesta de un montacargas de obra que colgaba sobre el abismo de la vía férrea."
    )
    add_prose_paragraph(
        doc,
        "Fang tomó la cuerda del montacargas con sus mandíbulas y la tiró con fuerza hercúlea, enviando a Barnaby, Silas, Vane y Caspian hacia los cables de acero de la torre vecina, fuera del alcance de las bestias. Luego, el noble perro se dio media vuelta para encarar en solitario al tigre de titanio y a los cincuenta caninos de cristal que se abalanzaban sobre él."
    )
    add_dialogue(
        doc,
        "Dr. Vane",
        "¡FANG! ¡NO! ¡VUELVE AQUÍ, MUCHACHO!",
        "gritó el anciano hámster con lágrimas quemándole los ojos a través de sus gafas de cobre."
    )
    add_prose_paragraph(
        doc,
        "Fang no miró atrás. Se plantó en el centro del viaducto agrietado y arremetió contra el pilar maestro de soporte con una embestida suicida. El hormigón debilitado por cincuenta años de lluvia ácida cedió. Con un estrépito colosal que sacudió los cimientos de la avenida entera, el puente central se derrumbó hacia el abismo de las vías férreas, arrastrando al Cazador de Cuero y a la horda entera en una avalancha de escombros y polvo blanco, dejando a Fang al otro lado de la brecha, herido pero en pie, ladrando su victoria hacia el cielo."
    )

    # -------------------------------------------------------------
    # ACTO III
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Acto III: El Duelo en la Rotonda de Kaelan", "La liberación del soberano de las astas y la fórmula de pino")
    
    add_prose_paragraph(
        doc,
        "Superado el viaducto, los cuatro supervivientes alcanzaron la Gran Rotonda Monumental que precedía a la plaza de la Torre Malvoro. Pero el camino estaba sellado. En el centro de la plaza se alzaba Kaelan, el Ciervo de Ébano. Sus astas monumentales de metro y medio ardían con una fosforescencia violeta salvaje, y a su alrededor, una barrera viva de lianas de micelio y zarzas de espinas impedía cualquier avance hacia las puertas de la torre."
    )
    add_prose_paragraph(
        doc,
        "Kaelan bajó la cabeza, con los belfos cubiertos de espuma negra y sus ojos ciegos inyectados en la locura del hongo. El suelo bajo sus pezuñas palpitaba con los latidos del cerebro vegetal subterráneo. Iba a embestir con la fuerza de un tren de carga."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "No tenemos suficientes dardos de fósforo para perforar ese blindaje de micelio, Silas. Nos va a ensartar contra el mármol.",
        "Barnaby preparó su aguja con resignación heroica."
    )
    add_dialogue(
        doc,
        "Silas",
        "No lo ataques, Barnaby. Kaelan no es un enemigo; es un prisionero de su propia corona. El hongo lo está devorando por dentro para usarlo de repetidor.",
        "Silas extrajo de su morral el atomizador de perfume de cristal que había cargado con su fórmula destilada de resina de pino centenario, savia de roble y azufre volcánico."
    )
    add_dialogue(
        doc,
        "Silas",
        "Caspian, necesito que uses tu dash de velocidad para llamar su atención. Atrae su embestida hacia la estatua ecuestre central. Cuando clave sus astas en el bronce, yo pulverizo la fórmula en la base de su cráneo.",
        "los ojos de Silas brillaron con determinación científica."
    )
    add_prose_paragraph(
        doc,
        "Caspian no vaciló. Encendió su farolillo al máximo y corrió en zigzag hacia el centro de la rotonda, esquivando las lianas que brotaban del asfalto como látigos. Kaelan rugió con furia homicida y cargó a toda velocidad. El suelo tembló. Justo cuando las puntas afiladas de las astas estaban a centímetros de ensartar al hámster, Caspian ejecutó un dash lateral milimétrico, rodando sobre el pavimento mojado."
    )
    add_prose_paragraph(
        doc,
        "El impacto de Kaelan contra la base de bronce de la estatua fue descomunal: el metal se dobló y las astas del ciervo quedaron profundamente incrustadas en el pedestal. Silas saltó desde la cornisa superior, aterrizando sobre la cerviz del titán, y accionó el atomizador directamente sobre los nódulos micóticos que conectaban la cornamenta con las vértebras cervicales de la bestia."
    )
    add_prose_paragraph(
        doc,
        "La reacción química fue instantánea. Las flores púrpuras del hongo se marchitaron en segundos, soltando un vapor negro que se disipó en el viento; el micelio que cubría las astas se secó y se desprendió en escamas muertas como ceniza de papel quemado. Kaelan sacudió la cabeza, liberándose del pedestal con un esfuerzo supremo, y retrocedió tambaleante."
    )
    add_prose_paragraph(
        doc,
        "Sus ojos, antes encendidos en una demencia homicida, recuperaron su color castaño profundo, húmedo y dulce. El gran ciervo miró a los pequeños hámsters que respiraban jadeantes a sus pezuñas. Inclinó su majestuosa testa en un saludo solemne de agradecimiento silencioso, y con un resoplido limpio, dio media vuelta y trotó hacia los parques del oeste, llamando con un bramido sereno a las manadas de herbívoros para que despertaran de su pesadilla carnívora."
    )

    # -------------------------------------------------------------
    # ACTO IV
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Acto IV: La Ascensión a la Cúpula de Hierro", "La traición de Lulú y el duelo en la boca del reactor")
    
    add_prose_paragraph(
        doc,
        "El camino a la cúspide de la Torre Malvoro estaba despejado. Utilizando la tarjeta de titanio de Lulú, los héroes ascendieron por el ascensor de servicio hasta el laboratorio del piso cuarenta y cuatro. El espectáculo en la cámara acorazada cortaba el aliento: el Reactor Carmesí latía como un corazón mecánico colosal dentro de un domo de titanio, alimentando una columna de humo escarlata que salía por la chimenea maestra hacia las nubes de tormenta."
    )
    add_prose_paragraph(
        doc,
        "En la consola principal, conectada por cables ópticos al Heraldo Mecánico, yacía la ranura del Filtro Maestro de oro y platino que controlaba el flujo alcaloide de toda la metrópoli."
    )
    add_dialogue(
        doc,
        "Dr. Vane",
        "Tengo el bypass acústico listo en mi terminal. En cuanto Barnaby desacople el filtro maestro y Silas vierta el neutralizador en la tubería de condensado, yo emitiré la frecuencia invertida para apagar las antenas de la ciudad para siempre.",
        "Vane conectó sus cables de cobre a la placa base."
    )
    add_prose_paragraph(
        doc,
        "Pero antes de que Barnaby diera un paso hacia el filtro, la puerta neumática de la esclusa se cerró con un estruendo hermético. Las luces de emergencia cambiaron de azul a rojo escarlata."
    )
    add_prose_paragraph(
        doc,
        "Sobre la pasarela superior, sonriendo con desprecio gélido, apareció Lulú. Ya no fingía ser una superviviente asustada. En su cuello lucía el collar de diamantes del Director Voss, y en su pata delantera derecha sostenía el detonador manual de sobrepresión del reactor."
    )
    add_dialogue(
        doc,
        "Lulú",
        "Fascinante trabajo de ingeniería, ratoncitos. Me han ahorrado semanas de trabajo esquivando a las bestias allá afuera. Pero la farsa termina aquí.",
        "sus ojos violetas resplandecían con una codicia demencial."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "Lulú... estuviste manipulándonos desde la primera noche en el drenaje.",
        "Barnaby apretó los dientes, sintiendo la amargura de la traición en su garganta."
    )
    add_dialogue(
        doc,
        "Lulú",
        "Por supuesto. ¿De verdad creyeron que una gata de mi linaje se conformaría con vivir comiendo gusanos en alcantarillas inundadas? Los humanos construyeron este trono para mí. Esta niebla me obedece. Cuando el reactor alcance el cien por ciento en tres minutos, la nube cubrirá el continente entero. Las fieras no serán salvajes desquiciadas: serán mi ejército personal, y yo seré la primera emperatriz de un mundo donde ningún roedor volverá a levantar la cabeza.",
        "Lulú bajó la palanca de sobrepresión a la posición de purga masiva."
    )
    add_prose_paragraph(
        doc,
        "Las alarmas de la torre aullaron con sirenas de ataque aéreo. Las turbinas del reactor comenzaron a girar a revoluciones supersónicas; el metal de las paredes comenzó a ponerse al rojo vivo por la fricción térmica del alquitrán hirviente. El tiempo se había agotado."
    )

    # -------------------------------------------------------------
    # ACTO V
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Acto V: La Última Trampa del Roedor", "El sacrificio coordinado de los diminutos y el estallido purificador")
    
    add_prose_paragraph(
        doc,
        "Fue en ese instante decisivo cuando la verdadera grandeza de las criaturas pequeñas se manifestó. Lulú esperaba que los tres hámsters se acobardaran como presas acorraladas. Pero no conocía la fuerza de una hermandad forjada en el fuego y la ceniza."
    )
    add_dialogue(
        doc,
        "Caspian",
        "¡Vane, inicia la secuencia sónica ahora mismo! ¡Silas, a las tuberías de azufre! ¡Barnaby y yo nos encargamos de la gata!",
        "rugió Caspian con una autoridad que resonó en toda la cámara de acero."
    )
    add_prose_paragraph(
        doc,
        "El Dr. Vane accionó los disyuntores de sus bobinas de cobre. Por los altavoces de la torre no salió un zumbido, sino un pulso invertido a noventa y cuatro hercios que desestabilizó instantáneamente los sensores acústicos de la sala. Lulú soltó un chillido de dolor y se llevó las patas a los oídos mientras el Heraldo Mecánico colapsaba desconectado en su nicho."
    )
    add_prose_paragraph(
        doc,
        "Silas corrió por la pasarela inferior, esquivando chorros de vapor hirviente, y vació el contenido entero de su frasco de resina purificada directamente en la toma de admisión del combustible. El alquitrán líquido reaccionó al contacto con el azufre, cristalizándose en una masa sólida e inerte que bloqueó las bombas de inyección."
    )
    add_prose_paragraph(
        doc,
        "Arriba, en la pasarela maestra, Lulú se abalanzó sobre Barnaby con las garras desenvainadas para degollarlo. Pero Caspian se interpuso en una fracción de segundo: activó el mecanismo de su farolillo de fósforo blanco y apuntó el haz de luz pura directamente a los ojos violetas de la felina. La luz cegadora desorientó a Lulú, haciéndola trastabillar sobre la rejilla resbaladiza."
    )
    add_prose_paragraph(
        doc,
        "Barnaby no dudó. Con un salto acrobático digno de su juventud, alcanzó la base del Filtro Maestro de oro y platino. Con su púa de acero hizo palanca sobre el perno de sujeción neumática y gritó con toda la fuerza de sus pulmones:"
    )
    add_dialogue(
        doc,
        "Barnaby",
        "¡Esta ciudad no pertenece a ningún amo! ¡Ni a los hombres, ni a las fieras, ni a ti!",
        "y arrancó el catalizador dorado de su alojamiento."
    )
    add_prose_paragraph(
        doc,
        "El circuito se quebró. La reacción en cadena se invirtió en una fracción de segundo. Una sobretensión electromagnética barrió la consola central en una onda de choque expansiva que arrojó a Lulú hacia el conducto de desecho abierto de la chimenea, precipitándola al vacío del río de aguas residuales en las profundidades de la ciudad."
    )
    add_prose_paragraph(
        doc,
        "Barnaby rodó por la plataforma envuelto en llamas azules, pero Caspian y Silas se lanzaron sobre él, sofocando el fuego con sus capas de musgo húmedo y poniéndolo a salvo en el túnel de evacuación de emergencia justo cuando el reactor colapsaba en una implosión sónica de luz blanca y pura que desintegró los últimos residuos de veneno químico."
    )

    # -------------------------------------------------------------
    # EPÍLOGO
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Epílogo: La Ciudad del Nuevo Amanecer", "Donde los gigantes duermen y los pequeños construyen el futuro")
    
    add_prose_paragraph(
        doc,
        "La detonación en la cima de la Torre Malvoro no destruyó la metrópoli; liberó una onda de choque purificadora que barrió el cielo de horizonte a horizonte. Las chimeneas dejaron de escupir humo escarlata. Las antenas rojas de cuarenta y siete hercios se apagaron con un gemido de estática final. Y la niebla... la densa y corrosiva niebla nocturna comenzó a elevarse hacia la atmósfera superior, disolviéndose bajo la fuerza del viento limpio."
    )
    add_prose_paragraph(
        doc,
        "Por primera vez desde el inicio de los tiempos oscuros, el sol salió sobre la ciudad con una claridad radiante, bañando el asfalto lavado por la lluvia en destellos dorados y esmeraldas."
    )
    add_prose_paragraph(
        doc,
        "En la Plaza Central, cientos de animales comenzaron a desperezarse como si despertaran de un letargo de pesadillas. Ciervos de pelaje rojizo pastaban pacíficamente junto a perros mestizos y gatos callejeros; palomas de alas blancas picoteaban granos limpios entre los adoquines; y en el puente reparado con troncos y cables, el noble Capitán Fang regresaba trotando hacia la plaza, con la lengua afuera y la cola en alto, recibido con júbilo por el Dr. Vane, quien corrió a abrazar su hocico arrugado."
    )
    add_prose_paragraph(
        doc,
        "Sobre la cornisa del viejo ayuntamiento, Barnaby, Silas y Caspian contemplaban el renacimiento de su mundo. En la pata de Barnaby descansaba el aro de platino del filtro maestro, ahora transformado en una pequeña campana que repicaría en el refugio para anunciar la hora de la cosecha de semillas."
    )
    add_dialogue(
        doc,
        "Silas",
        "Miren allá abajo... El micelio se está convirtiendo en tierra negra y fértil. En primavera, las avenidas serán praderas de trébol.",
        "Silas sonrió mientras guardaba sus semillas en el bolsillo."
    )
    add_dialogue(
        doc,
        "Caspian",
        "Y ya no hay monstruos en la niebla. Ni afuera... ni adentro de mi pecho.",
        "Caspian respiró hondo el aire de la mañana, sintiendo que su pulso latía en perfecta calma y armonía con el mundo."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "Los hombres construyeron rascacielos para tocar el cielo y se olvidaron de cuidar la tierra que pisaban. Nosotros empezaremos desde abajo. Con piedras pequeñas, madrigueras cálidas y puertas sin cerrojos para los amigos.",
        "el viejo hámster ajustó su zurrón y miró a sus hermanos de camino."
    )
    add_prose_paragraph(
        doc,
        "Y así, bajo el sol tibio de una metrópoli que comenzaba a florecer entre las grietas del cemento, los diminutos héroes descendieron hacia las calles para escribir, pata a pata y corazón a corazón, el primer capítulo de un mundo verdaderamente libre."
    )
    
    save_novel(doc, "Historia_6_Cronica_de_Ceniza_y_Humo_La_Gran_Novela_de_Malvoro.docx")

if __name__ == "__main__":
    build_book_6()
