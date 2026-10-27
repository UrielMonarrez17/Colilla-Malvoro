import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from novel_styler import (
    create_novel_document, add_book_title, add_epigraph,
    add_chapter_heading, add_prose_paragraph, add_dialogue,
    add_divider, save_novel
)

def build_book_4():
    doc = create_novel_document()
    
    add_book_title(
        doc,
        title="EL CÓDICE DEL SUJETO 042",
        subtitle="Crónica de la Sangre Primordial y el Engaño de la Mente",
        author="Volumen IV — Colección de Crónicas de la Metrópoli Feral"
    )
    
    add_epigraph(
        doc,
        "La soberbia del creador radica en creer que la jaula contiene a la bestia. No comprendieron que la bestia más peligrosa no ruge ni enseña colmillos de sable; cabe en la palma de una mano infantil y calcula la resistencia de los materiales con la frialdad de un bisturí.",
        "Diarios de Campo del Dr. Alistair Finch, Proyecto Arca, Nivel Sub-4"
    )

    # -------------------------------------------------------------
    # CAPÍTULO I
    # -------------------------------------------------------------
    add_chapter_heading(doc, "Capítulo I: Despertar en el Cristal Frío", "De cicatrices quirúrgicas, circuitos ópticos y la amnesia del laberinto")
    
    add_prose_paragraph(
        doc,
        "El despertar fue un dolor líquido de color verde esmeralda. Caspian tosió con violencia, expulsando por el hocico y las fosas nasales el gel hidrostático que había llenado sus pulmones durante meses. A su alrededor, los fragmentos de un tubo cilíndrico de metacrilato yacían esparcidos sobre el piso de rejilla de acero. En la pared del cilindro roto, grabado con láser indeleble, se leía: «PROYECTO ARCA — ESPÉCIMEN H-042 — ESTADO: RETENCIÓN VITAL»."
    )
    add_prose_paragraph(
        doc,
        "Caspian se incorporó sobre sus cuatro patas temblorosas. Al sacudirse el pelaje dorado salpicado de manchas cobrizas, sintió una tirantez extraña en la base del cráneo. Al palparse con la pata derecha descubrió una cicatriz milimétrica en forma de cruz, y bajo la piel, el relieve rígido de un microprocesador cuántico encapsulado en resina biológica."
    )
    add_prose_paragraph(
        doc,
        "Su mente no funcionaba como la de un hámster ordinario. Al mirar el cableado colgante del techo del laboratorio destruido, no vio sombras amenazantes: vio instantáneamente la caída de potencial eléctrico, la resistencia en ohmios del cobre expuesto y la trayectoria de un arco voltaico en caso de sobretensión. Sabía matemáticas vectoriales, conocía la tabla periódica de los elementos y comprendía el lenguaje grabado en las placas de acero de los archivadores."
    )
    add_dialogue(
        doc,
        "Caspian",
        "¿Quién soy? ¿Dónde están los guardianes de bata blanca?",
        "su voz era un susurro limpio, modulado con una precisión casi humana."
    )
    add_prose_paragraph(
        doc,
        "En el escritorio de aluminio del centro de la sala parpadeaba una terminal holográfica. Caspian trepó por la pata de la mesa con agilidad prodigiosa y presionó la tecla espaciadora con sus incisivos. La pantalla se iluminó proyectando el holograma tridimensional de un hombre demacrado, de unos cincuenta años, con ojeras profundas y bata manchada de reactivos: el Dr. Alistair Finch."
    )
    add_dialogue(
        doc,
        "Holograma de Finch",
        "Si estás viendo esta grabación, Caspian... significa que la brecha de contención se ha consumado. Malvoro y el mando militar decidieron purgar la ciudad tras la fuga del compuesto C-Zero. Nos encerraron aquí abajo para que muriéramos con nuestro trabajo.",
        "el rostro del científico parpadeaba con estática azulada."
    )
    add_dialogue(
        doc,
        "Holograma de Finch",
        "Pero tú eres diferente, 042. Inyectamos en tu torrente sanguíneo el vector sintético 'Phobos-Alpha'. Eres la única criatura que sobrevivió al proceso de aceleración neurocognitiva. Tienes la cura, Caspian. En tu sangre reside la fórmula para revertir la feralidad en los animales de la superficie. Debes llegar a la Torre de Bombeo Central y verter tu suero en los tanques de agua de la metrópoli. Sálvalos... perdónanos...",
        "la grabación se cortó con el sonido ensordecedor de un cristal estallando y disparos lejanos."
    )
    add_prose_paragraph(
        doc,
        "Caspian se irguió sobre sus patas traseras. Un deber sagrado se encendió en su diminuto pecho. Él era el héroe de la historia. El ungido de la ciencia. El pequeño mesías que devolvería la razón al mundo de las bestias."
    )

    # -------------------------------------------------------------
    # CAPÍTULO II
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo II: La Ciudadela de los Túneles", "La resistencia de Madame Vesper y las barricadas de bronce")
    
    add_prose_paragraph(
        doc,
        "El acceso a la superficie exigía cruzar la antigua línea de metro de la Calle Cuarenta. Los andenes subterráneos, antaño transitados por miles de pasajeros humanos apresurados, se habían convertido en una fortaleza amurallada. Vagones de tren descarrilados formaban una empalizada inexpugnable, reforzada con placas de matrícula de automóviles, vallas de construcción y pinchos de alambre de espino."
    )
    add_prose_paragraph(
        doc,
        "Allí gobernaba Madame Vesper. Era una rata topo desnuda de casi dos kilos de peso, con la piel rosada y arrugada como cuero viejo, privada de ojos pero provista de unos enormes anteojos de visión nocturna militar humana que llevaba atados a la cabeza con tirantes de goma. Su ejército estaba compuesto por comadrejas, topos de jardín y ardillas de alcantarilla que habían construido una sociedad de ingenieros subterráneos."
    )
    add_prose_paragraph(
        doc,
        "Cuando Caspian fue interceptado por la guardia de comadrejas armadas con estiletes de hueso, lo llevaron encadenado a la presencia de Madame Vesper en el interior de un vagón restaurante transformado en sala del trono."
    )
    add_dialogue(
        doc,
        "Madame Vesper",
        "Huelo a antiséptico. Huelo a vidrio de laboratorio y a la grasa sintética de los tubos criogénicos. Suelten a este espécimen... o mátenlo antes de que respire más fuerte.",
        "gruñó la anciana rata topo, apoyándose en un bastón hecho con una bujía de motocicleta."
    )
    add_dialogue(
        doc,
        "Caspian",
        "¡No soy su enemigo, Madame Vesper! Soy el Sujeto 042 del Proyecto Arca. Llevo la cura en mis venas. Vengo a liberar a la superficie del veneno de Malvoro.",
        "declaró Caspian con orgullo, mostrando la etiqueta grabada en su oreja."
    )
    add_prose_paragraph(
        doc,
        "Madame Vesper no sonrió. Se inclinó hacia él, y a través de los lentes verdes de sus anteojos, Caspian sintió una mirada que parecía atravesarle el cráneo hasta el microchip."
    )
    add_dialogue(
        doc,
        "Madame Vesper",
        "¿La cura? ¿Eso te dijeron los monstruos de bata blanca antes de pudrirse en sus sótanos? Pequeño tonto engreído. ¿Sabes cuántos distritos han caído presa de la locura en los últimos tres días? Todos aquellos por donde tú has pasado huyendo de las jaulas de cimentación.",
        "su voz era un silbido cargado de desprecio y miedo."
    )
    add_dialogue(
        doc,
        "Caspian",
        "Eso es imposible... Yo no he atacado a nadie. ¡Ni siquiera tengo garras para cazar!",
        "protestó el hámster con indignación."
    )
    add_dialogue(
        doc,
        "Madame Vesper",
        "No necesitas garras cuando eres una bomba bacteriológica andante. Mis exploradores te han seguido desde el nivel sub-cuatro. Cada vez que tu corazoncito de roedor late a más de trescientas pulsaciones por miedo o por furia, tus glándulas exocrinas liberan una neblina invisible de feromonas recombinantes. Los animales que la huelen no se curan, Caspian: pierden hasta el último vestigio de autocontrol y se convierten en bestias carniceras que destrozan a sus propias crías.",
        "la rata topo golpeó el suelo con su bastón."
    )
    add_dialogue(
        doc,
        "Madame Vesper",
        "Tú no eres la cura, Sujeto 042. Tú eres la plaga con patas de seda.",
        "sentenció con frialdad matemática."
    )

    # -------------------------------------------------------------
    # CAPÍTULO III
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo III: El Asalto del Cazador de Cuero", "Garras de titanio en el andén y la trampa de alta tensión")
    
    add_prose_paragraph(
        doc,
        "Las palabras de Vesper resonaron en los oídos de Caspian como un martillazo. ¿Era verdad? ¿Había sido su existencia un engaño macabro? Pero no hubo tiempo para el debate filosófico. Las sirenas de alarma del andén —campanas de bicicleta conectadas con cuerdas de pescar— comenzaron a tañer frenéticamente en todo el perímetro defensivo."
    )
    add_prose_paragraph(
        doc,
        "Una explosión de termita desgarró la barricada norte. Entre el humo azulado y las llamas químicas irrumpió el Cazador de Cuero: un tigre siberiano albino de casi cuatrocientos kilos, transformado por la división de cibernética militar de Malvoro. Tenía placas de blindaje de titanio remachadas directamente a las costillas y escápulas, y su mandíbula inferior había sido sustituida por una prensa hidráulica de acero quirúrgico capaz de triturar vigas de ferrocarril."
    )
    add_prose_paragraph(
        doc,
        "El tigre no buscaba comida. Sus ojos eran cámaras térmicas con punteros láser rojos que barrieron el andén hasta fijarse directamente en el hámster dorado encadenado al poste del vagón."
    )
    add_dialogue(
        doc,
        "Cazador de Cuero",
        "OBJETIVO PRIMARIO 042 LOCALIZADO. PROTOCOLO DE RECUPERACIÓN VITAL INICIADO. ELIMINACIÓN DE DAÑOS COLATERALES AUTORIZADA.",
        "bramó la bestia con un modulador de voz metálico instalado en su tráquea de acero."
    )
    add_prose_paragraph(
        doc,
        "La masacre comenzó en un segundo. Las comadrejas defensoras arrojaron lanzas y cócteles molotov improvisados, pero las balas y el fuego rebotaban inútilmente sobre el blindaje de titanio del tigre. De un solo zarpazo, la bestia volcó un vagón de tren de treinta toneladas como si fuera un cajón de manzanas, aplastando las barricadas de la resistencia."
    )
    add_dialogue(
        doc,
        "Madame Vesper",
        "¡Retirada a los túneles profundos! ¡Abandonen el andén!",
        "gritó Vesper mientras sus soldados la arrastraban hacia la esclusa de emergencia."
    )
    add_prose_paragraph(
        doc,
        "Caspian estaba solo, atado con cadenas de cobre a la columna de hierro. El tigre albino avanzaba paso a paso, con la baba ácida goteando de su quijada mecánica sobre las vías del tren. Estaba a escasos cuatro metros de distancia."
    )
    add_prose_paragraph(
        doc,
        "El pánico inundó el pecho de Caspian. Y fue entonces cuando lo notó: de sus poros brotó un vapor dulzón, casi imperceptible, con aroma a vainilla sintética y amoniaco. Al olerlo, el tigre sacudió la cabeza con violencia; las pupilas de sus cámaras parpadearon en rojo sangre y sus circuitos chirriaron sobrecargados. Las palabras de Madame Vesper eran ciertas: su propio miedo era el combustible que enloquecía a los monstruos."
    )
    add_dialogue(
        doc,
        "Caspian",
        "Si mi miedo te hace fuerte... entonces no tendré miedo.",
        "Caspian respiró hondo, obligando a su procesador neural a desacelerar sus latidos mediante un protocolo de meditación lógica."
    )
    add_prose_paragraph(
        doc,
        "Con los dientes, Caspian desenganchó el perno de seguridad de su grillete utilizando una pequeña lámina de acero que ocultaba bajo su pezuña. Libre de las cadenas, no huyó hacia los túneles: corrió directamente hacia el tercer riel electrificado del metro, que aún conducía setecientos cincuenta voltios de corriente continua."
    )
    add_prose_paragraph(
        doc,
        "Con una agilidad que desafiaba la gravedad, Caspian enganchó la cadena de cobre suelta al tercer riel y lanzó el otro extremo —con un lazo corredizo— directamente hacia el charco de agua de condensación donde el tigre albino acababa de plantar sus patas delanteras de titanio."
    )
    add_prose_paragraph(
        doc,
        "El cortocircuito fue colosal. Un arco voltaico blanco y cegador envolvió al tigre cibernético en una jaula de relámpagos. Los servomotores de su blindaje reventaron en llamaradas azules; los circuitos de su cráneo se fundieron con un chillido agudo de condensadores quemados, y el titán albino colapsó pesadamente sobre las vías, arrojando chispas y humo negro hasta quedar completamente inmóvil."
    )

    # -------------------------------------------------------------
    # CAPÍTULO IV
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo IV: La Revelación en el Frasco Negro", "El archivo sin editar de Finch y la condena de la soledad")
    
    add_prose_paragraph(
        doc,
        "Con el Cazador de Cuero fuera de combate, Caspian se arrastró hasta el panel de control del collar de datos del tigre. Utilizando su aguja como punta de prueba, conectó el bus de memoria del autómata a la terminal de mano que llevaba en su zurrón. Necesitaba saber la verdad absoluta sin los filtros de los hologramas de bienvenida."
    )
    add_prose_paragraph(
        doc,
        "En la pantalla parpadearon los archivos clasificados con el sello de máxima confidencialidad: «PROYECTO ARCA — INFORME DE DISPERSIÓN TERMINAL — AUTOR: DR. ALISTAIR FINCH»."
    )
    add_prose_paragraph(
        doc,
        "El video se reprodujo. No era el mensaje heroico del científico noble; era Finch encerrado en un búnker de cuarentena, con una máscara de gas rota y los ojos desorbitados por el terror mientras afuera golpeaban las puertas:"
    )
    add_dialogue(
        doc,
        "Finch (Grabación Real)",
        "El vector Phobos-Alpha ha funcionado más allá de toda previsión. El espécimen 042 es el portador perfecto. Su apariencia diminuta e inofensiva engañará a cualquier superviviente. La corporación Malvoro pagó cuarenta mil millones por este proyecto: una forma de esterilizar ecológicamente las ciudades rebeldes sin destruir la infraestructura física.",
        "Finch reía con un llanto histérico."
    )
    add_dialogue(
        doc,
        "Finch (Grabación Real)",
        "El cerebro aumentado de Caspian cree que está salvando el mundo. Le programamos recuerdos falsos de martirio y heroísmo para asegurar que viaje incansablemente por toda la metrópoli buscando tanques de agua y asambleas de supervivientes. Con cada kilómetro que recorre, con cada salto de su corazón asustado, disemina el catalizador. Para cuando descubra la verdad, no quedará un solo animal cuerdo sobre la faz de la tierra. Dios nos perdone... el hámster es el jinete del apocalipsis.",
        "un estruendo apagó la grabación mientras bestias invisibles derribaban la puerta del científico."
    )
    add_prose_paragraph(
        doc,
        "La pantalla se apagó."
    )
    add_prose_paragraph(
        doc,
        "Caspian quedó sentado sobre los rieles del metro en la más absoluta soledad. Cada recuerdo de su infancia en el laboratorio, cada palabra de cariño del Dr. Finch, cada sueño de reconstruir la civilización animal... todo había sido una mentira matemática diseñada en un laboratorio militar para convertir a un pequeño hámster dorado en el arma biológica más despiadada de la historia humana."
    )
    add_prose_paragraph(
        doc,
        "Miró sus patitas. Podía construir trampas mecánicas complejas, podía descifrar códigos militares y doblegar a tigres de acero. Pero no podía borrar el veneno que su propio corazón palpitante bombeaba al mundo con cada segundo de vida."
    )

    # -------------------------------------------------------------
    # EPÍLOGO
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Epílogo: El Exilio del Héroe Venenoso", "De cómo una criatura decidió ser su propia jaula")
    
    add_prose_paragraph(
        doc,
        "Caspian no subió a la Torre de Bombeo Central. No arrojó su sangre a los depósitos de agua de la metrópoli."
    )
    add_prose_paragraph(
        doc,
        "En lugar de eso, tomó las herramientas del taller subterráneo de Madame Vesper y fabricó para sí mismo una máscara hermética de cuero de fuelle y carbón activado, sellada con resina epoxi contra su hocico diminuto, que filtraba cada exhalación de sus pulmones para que ninguna molécula de feromona escapara al aire exterior."
    )
    add_prose_paragraph(
        doc,
        "Luego se internó en los túneles más profundos del drenaje pluvial, más allá de donde habitaban las ratas topo, en las galerías de roca virgen donde ningún ser vivo tenía hogar. Sabía que mientras permaneciera aislado en las sombras, la locura de la superficie dejaría de alimentarse de su miedo."
    )
    add_prose_paragraph(
        doc,
        "En la penumbra eterna de su madriguera de piedra, Caspian encendió una pequeña bombilla con una batería gastada y comenzó a escribir en las paredes de hormigón con su púa de acero los esquemas de las trampas y antídotos que algún día, cuando él ya no fuera más que huesos limpios, permitirían a los animales cuerdos reconstruir la libertad sin temer al veneno de los hombres."
    )
    add_prose_paragraph(
        doc,
        "El Sujeto 042 no sería el salvador que los humanos soñaron, pero se convirtió en el único monstruo que eligió encerrarse a sí mismo para salvar a los que aún soñaban con el sol."
    )
    
    save_novel(doc, "Historia_4_Proyecto_Arca_Sujeto_042.docx")

if __name__ == "__main__":
    build_book_4()
