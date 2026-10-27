import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from novel_styler import (
    create_novel_document, add_book_title, add_epigraph,
    add_chapter_heading, add_prose_paragraph, add_dialogue,
    add_divider, save_novel
)

def build_book_3():
    doc = create_novel_document()
    
    add_book_title(
        doc,
        title="LA FRECUENCIA 47",
        subtitle="El Silencio de los Amos y las Torres del Éter",
        author="Volumen III — Colección de Crónicas de la Metrópoli Feral"
    )
    
    add_epigraph(
        doc,
        "El oído no tiene párpados. No puedes cerrarlo al mundo como cierras los ojos ante la luz hiriente. Cuando la señal a cuarenta y siete hercios comenzó a sonar en los tejados, los hombres se volvieron polvo blanco en silencio, pero los animales... los animales escucharon los gritos de sus propios ancestros salvajes en cada hueso.",
        "Bitácora del Ingeniero Leopold Vance, Telecomunicaciones de Defensa Urbana"
    )

    # -------------------------------------------------------------
    # CAPÍTULO I
    # -------------------------------------------------------------
    add_chapter_heading(doc, "Capítulo I: Las Campanas que Nadie Oye", "De los hombres convertidos en sal y un hámster con orejeras de cobre")
    
    add_prose_paragraph(
        doc,
        "El Dr. Vane no recordaba cuándo había sido la última vez que escuchó el silencio. No el silencio de la soledad, sino esa ausencia de presión en los tímpanos que los mortales dan por sentada. Desde que las tres grandes antenas de la Torre Central comenzaron a parpadear con sus balizas carmesí en el cenit de la metrópoli, el aire vibraba con una oscilación continua, sorda y viscosa a cuarenta y siete ciclos por segundo."
    )
    add_prose_paragraph(
        doc,
        "Los humanos no la resistieron. Aquella fatídica tarde de otoño, cuando la señal se acopló con la red de telefonía móvil y los transformadores de alta tensión, la frecuencia entró en resonancia armónica con los iones de sodio y potasio del sistema nervioso humano. No hubo histeria colectiva ni guerras en las calles: los peatones se detuvieron en seco, dejaron caer sus maletines y teléfonos, y en cuestión de tres latidos cardíacos, sus cuerpos se deshidrataron molecularmente, colapsando sobre las aceras en pequeños montículos de sal mineral blanca y ceniza inerte."
    )
    add_prose_paragraph(
        doc,
        "Para los animales, la tragedia fue mucho peor. El oído canino, felino y de las aves captó la onda con una intensidad magnificada por diez. Para un perro o un gato, la frecuencia 47 no era un zumbido: era el aullido atronador de una manada invisible que exigía despedazar todo lo que se moviera. La corteza cerebral domesticada se incendió en espasmos epilépticos, dejando únicamente al descubierto el instinto depredador más sanguinario y homicida."
    )
    add_prose_paragraph(
        doc,
        "Vane había sobrevivido gracias a su condición de roedor menudo y a su ingenio obstinado. En la cabeza llevaba unos diminutos cascos acústicos fabricados con casquetes de bellotas forrados con cera de abeja, plomo batido en láminas microscópicas y dos bobinas de cobre que disipaban el campo electromagnético en espirales sobre sus orejas redondas. Su cuerpo medía poco más de ocho centímetros, pero caminaba con la prestancia de un catedrático entre las estatuas de sal de sus antiguos dueños."
    )
    add_dialogue(
        doc,
        "Vane",
        "Presión atmosférica normal. Humedad al ochenta y dos por ciento. La modulación de onda sigue subiendo en armónicos de segundo orden. A este paso, dentro de dos días hasta las lombrices saldrán del lodo para morder las piedras.",
        "anotaba Vane sobre una tira de cinta de teletipo perforada."
    )
    add_prose_paragraph(
        doc,
        "Un estrépito colosal a sus espaldas lo obligó a rodar bajo el parachoques de un taxi abandonado. Un montículo de sal blanca voló por el aire cuando una pezuña acorazada lo dispersó como harina en el viento."
    )

    # -------------------------------------------------------------
    # CAPÍTULO II
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo II: El Caballero Sordo", "La alianza entre el perro de trinchera y el estratega diminuto")
    
    add_prose_paragraph(
        doc,
        "De la nube de polvo blanco emergió una figura monumental. Era el Capitán Fang. Un bóxer de salvamento y rescate de cuarenta y dos kilos de músculo compacto, pelaje atigrado cubierto de cicatrices y un arnés táctico militar con placas de kevlar desgastadas. En su cuello no llevaba collar civil, sino una placa de identificación de la Unidad Canina de Respuesta Inmediata."
    )
    add_prose_paragraph(
        doc,
        "Fang estaba completamente sordo. Cinco años antes de la catástrofe, una explosión de gas en una fábrica de pinturas le había destrozado los tímpanos, salvándolo paradójicamente del apocalipsis acústico que había enloquecido al resto de su especie. Mientras los dobermans de la policía y los galgos de carreras vagaban por los bulevares con los ojos en blanco, mordiéndose sus propias patas en accesos de rabia psicótica, Fang mantenía su mirada castaña, noble y lúcida."
    )
    add_prose_paragraph(
        doc,
        "El gran perro bajó su hocico arrugado hasta el nivel del suelo, olfateando con suavidad el guardabarros del taxi. Al divisar a Vane, resopló con afecto, agitando el muñón de su cola en dos oscilaciones breves y medidas."
    )
    add_dialogue(
        doc,
        "Vane",
        "Llegas tarde, Capitán. El pulso de las dieciocho horas ya comenzó. Si no cruzamos la Avenida del Sol antes de que la estación repetidora alcance el pico de amplitud, las aves nos van a hacer trizas.",
        "le habló Vane haciendo señas con las patas delanteras: el código gestual militar que ambos habían perfeccionado."
    )
    add_prose_paragraph(
        doc,
        "Fang inclinó la cabeza, permitiendo que el hámster trepara por la solapa de su arnés hasta acomodarse en una pequeña cesta de cuero fijada detrás de su oreja izquierda. Aquella era su simbiosis: el perro aportaba una fuerza demoledora, dientes capaces de partir barras de aluminio y una lealtad inquebrantable; el hámster aportaba los ojos de orientación científica, las llaves mecánicas para sortear alambradas y el mapa de las frecuencias de la ciudad."
    )
    add_dialogue(
        doc,
        "Vane",
        "Avanza por el callejón de correos, muchacho. Y mantén la quijada apretada. El Coro del Éter está en el tejado.",
        "Vane le dio dos golpecitos suaves en la placa del arnés."
    )

    # -------------------------------------------------------------
    # CAPÍTULO III
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo III: El Coro del Éter", "Plumas arrancadas, graznidos de radio y la trampa sónica")
    
    add_prose_paragraph(
        doc,
        "El cielo sobre la plaza central parecía una herida en carne viva teñida de púrpura por los focos de vapor de sodio. Sobre los cables de alta tensión y las cornisas de los bancos no había gárgolas: había miles de palomas, cuervos y gaviotas marinas. Ninguna cantaba como un pájaro; emitían chirridos modulados que imitaban las señales de fax y los tonos de llamada de teléfonos móviles muertos."
    )
    add_prose_paragraph(
        doc,
        "Muchas de ellas se habían arrancado las plumas del pecho con sus propios picos en un intento desesperado por rascarse el dolor acústico que la frecuencia generaba en sus sacos aéreos. Volaban en espirales erráticas, chocando contra las farolas y levantándose ensangrentadas para continuar su danza de histeria."
    )
    add_prose_paragraph(
        doc,
        "De pronto, una paloma de ojos rojos fijó su mirada en el perro y el hámster. Emitió un alarido gutural que sonó como estática de televisión sintonizada en un canal vacío. Al instante, la bandada entera —más de trescientas aves enloquecidas— se lanzó en picado como una lluvia de proyectiles negros con picos afilados por el odio."
    )
    add_dialogue(
        doc,
        "Vane",
        "¡Posición defensiva, Fang! ¡Bajo el pórtico!",
        "chilló el hámster mientras desenrollaba un cable de su zurrón."
    )
    add_prose_paragraph(
        doc,
        "Fang se plantó como una roca bajo el arco de piedra del edificio de telégrafos. Sus fauces se abrieron en un ladrido silencioso para él, pero aterrador para cualquier fiera. Cuando las primeras aves intentaron picotearle los ojos, el bóxer lanzó dentelladas demoledoras que quebraban alas y espinazos en el aire. Sin embargo, el número de enemigos era abrumador: picos acerados golpeaban sus costados acorazados, buscando la carne desprotegida de sus patas y hocico."
    )
    add_prose_paragraph(
        doc,
        "Vane no se quedó a mirar. En el suelo del pórtico yacía un viejo megáfono antidisturbios abandonado por la policía municipal. El hámster corrió por el lomo de Fang, saltó al mango del megáfono y conectó los dos polos de su bobina de cobre a la batería de nueve voltios del aparato. Luego golpeó el micrófono con su púa de acero con una cadencia precisa: punto, raya, punto, punto."
    )
    add_prose_paragraph(
        doc,
        "El megáfono emitió un acople ensordecedor a noventa y cuatro hercios: el doble exacto de la frecuencia 47. El contra-tono armónico creó un vacío acústico en el aire. Las aves en vuelo se detuvieron en seco, sacudieron sus cabezas como si hubieran chocado contra una pared de cristal invisible y comenzaron a caer al suelo como frutos maduros desprendidos de un árbol, desorientadas y aturdidas."
    )
    add_dialogue(
        doc,
        "Vane",
        "¡Ahora, Fang! ¡Hacia la Torre de Transmisión antes de que se recalibre el modulador!",
        "el hámster saltó de vuelta a su cesta mientras el perro arrancaba en un sprint demoledor."
    )

    # -------------------------------------------------------------
    # CAPÍTULO IV
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo IV: La Cripta de los Transmisores", "La máquina que sueña con sirenas y el guardián de hierro")
    
    add_prose_paragraph(
        doc,
        "La base de la Gran Torre de Telecomunicaciones era un búnker de hormigón reforzado en el centro de la plaza. En el vestíbulo, los monitores de vigilancia aún brillaban con líneas verdes de fósforo. Filas interminables de servidores informáticos zumbaban con la furia de turbinas, alimentados por generadores geotérmicos automatizados que no necesitaban combustible fósil."
    )
    add_prose_paragraph(
        doc,
        "El aire dentro del búnker olía a ozono quemado y baquelita recalentada. En el centro de la sala de control no había operadores humanos; los esqueletos de los técnicos yacían sobre los teclados, reducidos a sal cristalina y botones de uniforme militar. En su lugar, erguido sobre un pedestal de acero en la cúspide de la pirámide de servidores, montaba guardia el 'Alfa Mecánico'."
    )
    add_prose_paragraph(
        doc,
        "Era un prototipo militar cuadrúpedo de la división de defensa urbana: una pantera de titanio y fibra de carbono de doscientos kilos, con cámaras ópticas de visión nocturna en las cuencas oculares y un transmisor de microondas en lugar de lengua. De su lomo salían gruesos cables trenzados que la conectaban directamente a la consola matriz de la torre."
    )
    add_dialogue(
        doc,
        "Vane",
        "El Proyecto Heraldo. Dios mío... los rumores eran ciertos.",
        "murmuró Vane, ajustando sus cascos de cobre para filtrar el zumbido ensordecedor de los relés."
    )
    add_prose_paragraph(
        doc,
        "La pantera mecánica alzó la cabeza de acero. Sus lentes ópticas zumbaron enfocando a Fang. De su altavoz frontal no brotó un rugido de animal, sino una voz digital sintética y distorsionada por décadas de polvo en las pistas magnéticas:"
    )
    add_dialogue(
        doc,
        "El Heraldo",
        "ALERTA ROJA. ESTADO DE SITIO METROPOLITANO DECLARADO. ORDEN DE CUARENTENA BIOLÓGICA ACTIVA. TODO ORGANISMO NO IDENTIFICADO ES CATALOGADO COMO VECTOR HOSTIL.",
        "los servomotores de sus patas delanteras se tensaron con un chasquido hidráulico."
    )
    add_dialogue(
        doc,
        "Vane",
        "¡Fang, distráelo! ¡No dejes que use el emisor de pulsos directo o freirá tus terminaciones nerviosas!",
        "gritó el hámster saltando al suelo técnico con una llave Allen en el hocico."
    )
    add_prose_paragraph(
        doc,
        "Fang rugió y se lanzó al ataque. La colisión entre el perro de carne y hueso y la bestia de titanio resonó como dos locomotoras chocando en un andén. Fang mordió con toda la potencia de sus mandíbulas la manguera hidráulica de la pata trasera de la máquina; el aceite a presión bañó su pelaje, pero el autómata respondió con un zarpazo de acero templado que rasgó la placa de kevlar del arnés de Fang, haciéndolo retroceder con un quejido sordo."
    )
    add_prose_paragraph(
        doc,
        "Vane corrió entre las rejillas de ventilación bajo los pies de los combatientes. Necesitaba llegar a la consola de alimentación principal. Subió por los manojos de cables como un funambulista, esquivando arcos voltaicos y relés que chisporroteaban a escasos centímetros de sus bigotes."
    )

    # -------------------------------------------------------------
    # CAPÍTULO V
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo V: El Giro del Heraldo Ciego", "Donde se descubre que la máquina solo lloraba por sus creadores")
    
    add_prose_paragraph(
        doc,
        "Con un último esfuerzo, Vane alcanzó el panel de acceso de emergencia en la espalda de la pantera mecánica mientras Fang la mantenía inmovilizada contra un armario de servidores con una embestida frontal colosal. El hámster insertó su púa de acero en la ranura de bypass manual y giró el trinquete."
    )
    add_prose_paragraph(
        doc,
        "La pantera se congeló. Sus pupilas infrarrojas parpadearon de rojo a ámbar. Los cables de su lomo perdieron tensión y el zumbido de cuarenta y siete hercios que torturaba a la ciudad se redujo a un murmullo agónico."
    )
    add_prose_paragraph(
        doc,
        "Fue en ese instante cuando la pantalla central de la sala de control se iluminó con el registro histórico de las últimas órdenes ingresadas antes del colapso. Vane, que sabía leer el código binario y los caracteres del viejo alfabeto, leyó el texto con los ojos nublados por el asombro."
    )
    add_prose_paragraph(
        doc,
        "No había sido un arma de ataque. El sistema de frecuencia 47 había sido diseñado por los médicos de la defensa civil como un protocolo de analgesia sónica masiva para calmar los espasmos de la población humana cuando la fuga química de la refinería Malvoro saturó el aire de neurotoxinas letales. Los humanos no murieron por la frecuencia; la frecuencia fue el sedante final que sus propios científicos les administraron para que no sufrieran al disolverse por el veneno químico del tabaco militar."
    )
    add_prose_paragraph(
        doc,
        "Y la máquina... la máquina no odiaba a los animales. De sus altavoces, ya sin la distorsión del protocolo de combate, brotó la grabación original en bucle de la voz de una niña pequeña, la hija del ingeniero jefe, que había dejado grabada en el módulo de memoria de la pantera:"
    )
    add_dialogue(
        doc,
        "Voz Infantil en la Máquina",
        "Papá... si no despierto mañana, cuida a los pájaros del parque. Sigue tocando la música de la campana para que no sientan miedo en la oscuridad...",
        "la voz se apagaba en una estática cálida."
    )
    add_prose_paragraph(
        doc,
        "El Heraldo Mecánico no estaba atacando a la ciudad por odio. Creía que la señal estaba consolando a las criaturas vivas, sin comprender que los oídos animales no podían soportar la melodía que los humanos habían compuesto para su propio entierro."
    )
    add_dialogue(
        doc,
        "Vane",
        "Pobres tontos... Construyeron una nana de hierro y terminaron convocando al demonio.",
        "Vane acarició la chapa fría de la máquina."
    )
    add_prose_paragraph(
        doc,
        "Con una determinación triste pero serena, el hámster accionó la palanca de apagado definitivo. Los disyuntores maestros saltaron con un chasquido metálico rotundo. Las balizas rojas de la torre se apagaron una a una en el cielo nocturno."
    )

    # -------------------------------------------------------------
    # EPÍLOGO
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Epílogo: El Retorno del Silencio", "De cómo una noche muda devolvió los latidos a la metrópoli")
    
    add_prose_paragraph(
        doc,
        "El silencio cayó sobre la ciudad no como una ausencia, sino como un bálsamo celestial. Afuera, en las calles, los perros que mordían el asfalto cayeron exhaustos, respirando con calma por primera vez en semanas. Las aves en las cornisas cerraron sus picos ensangrentados y escondieron sus cabezas bajo las alas para dormir un sueño reparador."
    )
    add_prose_paragraph(
        doc,
        "El Capitán Fang se dejó caer pesadamente sobre el suelo de la sala de control, apoyando su enorme cabeza entre las patas delanteras. El hámster descendió con cuidado por su pelaje y se acurrucó contra su cuello cálido, quitándose por fin los cascos de cobre que le habían martilleado las sienes durante meses."
    )
    add_prose_paragraph(
        doc,
        "Por la ventana rota del búnker entraba la primera brisa limpia de la madrugada, cargada con el olor a tierra mojada y hojas verdes lejanas, sin rastro de estática ni zumbidos fantasmales."
    )
    add_prose_paragraph(
        doc,
        "«Buen trabajo, muchacho», susurró Vane acariciando la oreja rasgada del perro. Fang no pudo oír las palabras, pero sintió el latido diminuto y constante de su amigo contra su pecho. Y por primera vez desde que los hombres se hicieron polvo, el viejo perro cerró los ojos y soñó en paz."
    )
    
    save_novel(doc, "Historia_3_La_Frecuencia_47.docx")

if __name__ == "__main__":
    build_book_3()
