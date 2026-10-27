import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from novel_styler import (
    create_novel_document, add_book_title, add_epigraph,
    add_chapter_heading, add_prose_paragraph, add_dialogue,
    add_divider, save_novel
)

def build_book_5():
    doc = create_novel_document()
    
    add_book_title(
        doc,
        title="EL SUEÑO DE LA NIEBLA ETERNA",
        subtitle="La Metrópoli de los Ojos Amarillos y el Borde del Mundo",
        author="Volumen V — Colección de Crónicas de la Metrópoli Feral"
    )
    
    add_epigraph(
        doc,
        "No temas a la oscuridad, pequeño caminante; teme a aquello que la oscuridad engendra cuando alguien intenta recordarla. Para dondequiera que mires, la niebla devolverá tu mirada con ojos prestados. Porque la niebla no es vapor: es el olvido tomando cuerpo sobre el asfalto.",
        "El Libro de las Urracas de Ceniza, Canto Cuarto"
    )

    # -------------------------------------------------------------
    # CAPÍTULO I
    # -------------------------------------------------------------
    add_chapter_heading(doc, "Capítulo I: El Farol de Fósforo Blanco", "De sombras que se condensan cuando nadie parpadea")
    
    add_prose_paragraph(
        doc,
        "Nox sostenía su farolillo con la pata delantera izquierda, balanceándolo suavemente al ritmo de sus pasos cortos y rápidos sobre los adoquines húmedos. El farolillo era una maravilla de la orfebrería de desecho: el casquillo de latón de una bala de nueve milímetros, vaciado y perforado con orificios diminutos en forma de constelaciones, dentro del cual ardía un trozo minúsculo de fósforo blanco suspendido en aceite de clavo. Su llama no era naranja; emitía una luz pálida, casi fantasmal, que disipaba la oscuridad en un radio de exactamente dos metros."
    )
    add_prose_paragraph(
        doc,
        "Más allá de esos dos metros, el mundo dejaba de ser sólido."
    )
    add_prose_paragraph(
        doc,
        "La ciudad tenía una propiedad perturbadora que Nox había aprendido a temer más que a cualquier depredador: la niebla no era homogénea. Si Nox miraba hacia el norte, la niebla en el norte se volvía instantáneamente impenetrable, espesándose como almidón hirviente hasta tomar la consistencia de una pared de algodón gris; si giraba la cabeza bruscamente hacia el sur, el norte se despejaba en un laberinto de fachadas vacías mientras el sur se cubría de una humareda densa y amenazante. La niebla se concentraba allí donde él clavaba la vista, como si la realidad se negara a ser observada."
    )
    add_prose_paragraph(
        doc,
        "Y en el corazón de esa niebla siempre aparecían ellos: pares de ojos amarillos, fosforescentes y redondos como monedas de azufre, que flotaban a la altura de las rodillas de un hombre invisible."
    )
    add_dialogue(
        doc,
        "Mica",
        "No gires tan rápido el pescuezo, Nox. Cada vez que buscas los ojos en la esquina, le estás dando permiso a la niebla para inventar un monstruo nuevo.",
        "graznó Mica desde la capota abollada de un buzón de correos rojo."
    )
    add_prose_paragraph(
        doc,
        "Mica era una urraca vieja, con el plumaje azul metálico salpicado de manchas de hollín y una pata de palo tallada en una cerilla usada. Llevaba colgada del cuello una bolsa de terciopelo donde guardaba su tesoro más preciado: seis ojos de cristal de muñecas antiguas de porcelana, de todos los colores imaginables. Mica aseguraba que si mirabas a través de un ojo de cristal azul, podías ver cómo era el cielo antes de que las chimeneas de Malvoro lo envenenaran."
    )
    add_dialogue(
        doc,
        "Nox",
        "No los invento yo, Mica. Están ahí. Huelo su frío. Huelen a hielo y a mentol quemado.",
        "el hámster alzó el farol, iluminando la esquina de la panadería abandonada."
    )
    add_prose_paragraph(
        doc,
        "En efecto, en el rincón donde la niebla se había condensado ante la mirada de Nox, la pared de humo se abrió. De ella emergió un doberman feral. Pero no era un perro común de carne y sangre: su pelaje era translúcido como el cuarzo ahumado, sus costillas brillaban con una luz azulada y sus pisadas sobre los charcos no salpicaban agua; congelaban el líquido en láminas de escarcha quebradiza."
    )

    # -------------------------------------------------------------
    # CAPÍTULO II
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo II: La Geometría que se Dobla", "Donde los relojes marcan horas imposibles y las calles se muerden la cola")
    
    add_prose_paragraph(
        doc,
        "Nox no intentó combatir a la bestia de cristal con la fuerza. Sabía que un golpe de sus fauces quebraría su pequeño cuerpo como una nuez bajo un martillo. En lugar de eso, abrió la ventanilla del farolillo. Un rayo de luz blanca pura y concentrada incidió directamente sobre los ojos del canino."
    )
    add_prose_paragraph(
        doc,
        "La bestia aulló, pero no de rabia, sino con un llanto humano desgarrador. Al recibir la luz del fósforo, su cuerpo translúcido no sangró: comenzó a evaporarse en volutas de vapor blanco con aroma a alcanfor, retrocediendo hacia la niebla hasta disolverse como una gota de leche en un vaso de agua."
    )
    add_dialogue(
        doc,
        "Nox",
        "¿Por qué lloran cuando los alumbro, Mica? Ningún animal salvaje llora como un niño perdido.",
        "preguntó Nox, cerrando la ventanilla con una patita temblorosa."
    )
    add_dialogue(
        doc,
        "Mica",
        "Porque la luz les recuerda lo que fueron, Nox. Esas cosas no son animales que enfermaron con el tabaco de Malvoro. Son fragmentos de memoria que se resisten a disolverse. Toda esta ciudad... ¿no lo has notado en tus patitas cuando corres?",
        "la urraca batió sus alas y se posó sobre el farol de hierro de la esquina."
    )
    add_dialogue(
        doc,
        "Mica",
        "Mira el reloj del ayuntamiento.",
        "señaló con el pico hacia la torre barroca del centro de la plaza."
    )
    add_prose_paragraph(
        doc,
        "Nox alzó la mirada. La esfera de bronce del reloj no tenía doce números; tenía dieciséis. Y las manecillas no avanzaban: giraban hacia atrás a una velocidad frenética, deteniéndose a intervalos caprichosos para luego temblar como agujas de brújula en medio de una tormenta magnética."
    )
    add_dialogue(
        doc,
        "Mica",
        "Llevamos tres días caminando hacia el este, Nox. Y hace tres horas pasamos por delante de esta misma panadería por cuarta vez. El letrero roto de 'Pan Fresco' tiene la misma telaraña que esquivamos ayer. Las calles se están enrollando sobre sí mismas. Algo está apagando los bordes del mapa.",
        "la urraca bajó la voz hasta convertirla en un susurro gélido."
    )
    add_dialogue(
        doc,
        "Nox",
        "Entonces subamos al mirador de la Torre de Cristal. Desde allí se tiene que ver el límite del valle.",
        "propuso el roedor, ajustándose el morral de tela donde guardaba sus cerillas."
    )

    # -------------------------------------------------------------
    # CAPÍTULO III
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo III: La Escalinata del Cielo de Plomo", "El ascenso por encima de las nubes y el secreto del horizonte")
    
    add_prose_paragraph(
        doc,
        "Subir los sesenta pisos del Rascacielos Aurora fue una odisea que desafió las nociones de tiempo y espacio. A medida que ascendían por la escalera de incendios exterior, la ciudad allá abajo se desdibujaba en un mar de niebla gris del que solo sobresalían las antenas rojas de telecomunicaciones, como boyas flotando en un océano de mercurio."
    )
    add_prose_paragraph(
        doc,
        "A cada piso que dejaban atrás, el viento se volvía más helado y silencioso. No había pájaros en las alturas, ni hojas secas, ni el zumbido de transformadores eléctricos. Solo se escuchaba el leve chasquido del fósforo blanco ardiendo en el farolillo de Nox y el roce de la madera de la pata de Mica contra los peldaños de rejilla."
    )
    add_prose_paragraph(
        doc,
        "En el piso cuarenta y cinco, Mica se detuvo ante un ventanal roto. Metió el pico en su bolsa de terciopelo, extrajo uno de sus ojos de cristal verde y lo sostuvo contra la luna menguante."
    )
    add_dialogue(
        doc,
        "Mica",
        "Nox... mira hacia el oeste. Más allá de la refinería.",
        "su voz temblaba con una mezcla de éxtasis y terror cósmico."
    )
    add_prose_paragraph(
        doc,
        "El pequeño hámster se encaramó al borde del marco de aluminio y miró en la dirección que señalaba la urraca. Esperaba divisar las colinas boscosas que rodeaban la metrópoli, las carreteras interestatales por donde los convoyes militares habían huido durante la gran evacuación, o las luces de otras ciudades lejanas."
    )
    add_prose_paragraph(
        doc,
        "No había nada de eso."
    )
    add_prose_paragraph(
        doc,
        "Al final de la última avenida de la metrópoli, donde el asfalto de la autopista debía continuar hacia el horizonte, el mundo simplemente terminaba. No había un acantilado ni un mar: había una pared monumental, infinitamente alta y ancha, hecha de azulejos de cerámica blanca impoluta, como las paredes de una sala de operaciones gigantesca. Y en el cielo plomizo, más allá de las nubes de niebla, no había estrellas: había enormes lámparas fluorescentes tubulares de hospital que parpadeaban con una luz mortecina y emitían un zumbido eléctrico a sesenta ciclos."
    )
    add_dialogue(
        doc,
        "Nox",
        "Mica... ¿qué es eso? ¿Qué son esos muros blancos?",
        "el hámster sintió que el suelo bajo sus patitas perdía solidez, como si estuviera flotando sobre una esponja."
    )
    add_dialogue(
        doc,
        "Mica",
        "Es el borde de la habitación, pequeño. Es la pared de la jaula donde el soñador está muriendo.",
        "dos lágrimas resbalaron por las plumas cenicientas de la urraca."
    )

    # -------------------------------------------------------------
    # CAPÍTULO IV
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo IV: La Sombra que Mira", "El encuentro con la entidad de vapor y el trueno del corazón mecánico")
    
    add_prose_paragraph(
        doc,
        "En ese instante, el cielo entero vibró con un sonido que no procedía de la tormenta ni de las máquinas de Malvoro. Fue un latido sordo, colosal, que sacudió los cristales del rascacielos como un terremoto rítmico: *BUM... BUM... BUM...* Cada latido iba acompañado por un pitido agudo y monótono que resonaba desde el infinito como el sensor de un monitor electrocardiográfico humano."
    )
    add_prose_paragraph(
        doc,
        "La niebla en la terraza del rascacielos comenzó a girar en un remolino vertiginoso. De su centro no surgió un perro ni un ciervo; se condensó una figura titánica de más de cinco metros de altura, hecha de vapor denso, ceniza de cigarrillo y jirones de batas médicas desgarradas. En su rostro de humo no había facciones, salvo dos ojos amarillos inmensos y profundos como abismos que miraron directamente al diminuto hámster."
    )
    add_dialogue(
        doc,
        "La Sombra",
        "PEQUEÑO RECUERDO. ¿POR QUÉ SIGUES ENCENDIENDO ESA LLAMA? LA MEDICINA YA HA ENTRADO EN EL GOTEO. ES HORA DE DORMIR.",
        "la voz no sonaba en el aire; retumbaba dentro del cerebro de Nox con la fuerza de un pensamiento propio."
    )
    add_dialogue(
        doc,
        "Nox",
        "¡Yo no soy un recuerdo! ¡Soy Nox! ¡Tengo una madriguera, tengo frío, me duelen las patas y tengo miedo!",
        "gritó el hámster alzando su farolillo con ambas patas delanteras como un escudo sagrado."
    )
    add_dialogue(
        doc,
        "La Sombra",
        "Tienes frío porque la sangre del anciano se está enfriando en la cama de la clínica San Lucas. Tienes miedo porque la última arteria cerebral se está obstruyendo por el alquitrán de cuarenta años de humo. Toda esta ciudad de pesadilla... las fábricas Malvoro que él construyó... los animales de laboratorio que enjauló en su juventud... las bestias ferales que devoran las calles... no son el mundo de afuera. Son sus culpas devorando su mente antes del paro cardíaco final.",
        "la silueta de niebla extendió una mano de vapor hacia el farolillo."
    )
    add_dialogue(
        doc,
        "La Sombra",
        "Y tú, pequeño Nox... tú eres el hámster dorado que él tuvo cuando tenía siete años. El único ser vivo al que amó antes de aprender a vender veneno en cajetillas doradas. Eres su último pensamiento limpio luchando contra la muerte cerebral.",
        "los ojos amarillos de la entidad se suavizaron con una piedad inmensa y triste."
    )
    add_prose_paragraph(
        doc,
        "Mica miró a Nox. Su cuerpo de plumas comenzaba a desvanecerse en hilillos de vapor azulado. Con una sonrisa serena, arrojó su bolsa de ojos de cristal al vacío de la avenida."
    )
    add_dialogue(
        doc,
        "Mica",
        "Ha sido un buen viaje, Nox. Pero la lechuza tenía razón: ningún sueño dura para siempre.",
        "sus últimas plumas se disolvieron en la brisa de la noche."
    )

    # -------------------------------------------------------------
    # CAPÍTULO V
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo V: La Llama que no se Rinde", "De la dignidad de existir aunque sea por un segundo en la mente de otro")
    
    add_prose_paragraph(
        doc,
        "Nox quedó completamente solo en la terraza del rascacielos más alto de una ciudad que se desvanecía. Los edificios vecinos ya no eran de granito ni hormigón; sus bordes se difuminaban en píxeles de niebla y estática blanca. El sonido del monitor cardíaco en el cielo se volvía más espaciado: *BUM... (pausa)... BUM... (pausa más larga)...*"
    )
    add_prose_paragraph(
        doc,
        "La Sombra de Niebla lo cubría todo. Bastaba una ráfaga de aire para soplar sobre el farolillo de fósforo y permitir que el silencio blanco se tragara la metrópoli, el dolor y la memoria del anciano moribundo."
    )
    add_prose_paragraph(
        doc,
        "Pero Nox miró sus patitas doradas. Tocó la cicatriz de su oreja. Sintió el peso del morral en su espalda y el recuerdo de los pasos dados junto a Mica, las trampas construidas, los callejones sorteados. ¿Acaso importaba si había nacido en el vientre de una madre roedora en un prado o en la última neurona de un biólogo arrepentido en un hospital frío?"
    )
    add_dialogue(
        doc,
        "Nox",
        "Tal vez yo sea tu recuerdo, anciano. Pero mientras yo esté despierto, tú no estarás muerto.",
        "dijo Nox con una voz que no tembló ante la inmensidad del abismo."
    )
    add_prose_paragraph(
        doc,
        "El pequeño hámster sacó de su morral la última astilla de madera de pino y la frotó contra el pedernal de su encendedor. Alimentó la llama del farolillo de fósforo blanco con toda la fuerza de sus pulmones. La luz no se apagó: se expandió en una esfera dorada resplandeciente que atravesó la niebla como una supernova en miniatura."
    )
    add_prose_paragraph(
        doc,
        "El latido en el cielo, que amenazaba con apagarse en una línea recta y plana de silencio, cobró fuerza de pronto: *¡BUM! ¡BUM! ¡BUM!* La niebla retrocedió aullando; las paredes de azulejos blancos se alejaron hacia el infinito, y las calles de asfalto y ladrillo volvieron a cobrar solidez bajo la lluvia templada."
    )

    # -------------------------------------------------------------
    # EPÍLOGO
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Epílogo: La Chispa en el Amanecer", "De cómo una noche infinita encontró su primer rayo de sol")
    
    add_prose_paragraph(
        doc,
        "Nox bajó de la torre y caminó hacia la plaza central. La niebla aún cubría los callejones, pero ya no era un manto hostil de ojos acechantes; era una bruma de rocío matinal que se disipaba suavemente al contacto con el suelo."
    )
    add_prose_paragraph(
        doc,
        "Entre los coches abandonados, un ciervo de ojos negros pastaba sobre un brote de hierba fresca que nacía en una grieta del pavimento. A lo lejos, un perro mestizo levantó la cabeza, lo miró con curiosidad mansa y continuó trotando hacia el parque municipal."
    )
    add_prose_paragraph(
        doc,
        "En el horizonte del este, por primera vez desde que comenzó la gran catástrofe de Malvoro, una franja de luz dorada rasgó las nubes plomizas. Era el amanecer de un día nuevo."
    )
    add_prose_paragraph(
        doc,
        "Nox sopló sobre la ventanilla de su farolillo y apagó la llama de fósforo. Ya no la necesitaba. Ajustó su zurrón, olió el aire fresco de la mañana y comenzó a trotar hacia el nuevo barrio por explorar. Mientras hubiera un camino bajo sus patas y un rayo de sol en sus bigotes, el sueño continuaría siendo vida."
    )
    
    save_novel(doc, "Historia_5_El_Sueno_de_la_Niebla_Eterna.docx")

if __name__ == "__main__":
    build_book_5()
