import os
import sys

# Ensure Historia is in path
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from novel_styler import (
    create_novel_document, add_book_title, add_epigraph,
    add_chapter_heading, add_prose_paragraph, add_dialogue,
    add_divider, save_novel
)

def build_book_1():
    doc = create_novel_document()
    
    add_book_title(
        doc,
        title="EL FILTRO CARMESÍ",
        subtitle="La Corporación Malvoro y el Aditivo Cero",
        author="Volumen I — Colección de Crónicas de la Metrópoli Feral"
    )
    
    add_epigraph(
        doc,
        "No hay mayor tiranía que aquella que se vende envuelta en papel blanco y se consume con una suave calada. El humo no adormecía a los hombres; alimentaba a la bestia dormida en sus médulas. Cuando el filtro cayó a las cloacas, la bestia simplemente cambió de dueño.",
        "Diario confidencial del Dr. Helmut Voss, Director de Bioquímica Aplicada, Malvoro Tobacco Corp."
    )
    
    # -------------------------------------------------------------
    # CAPÍTULO I
    # -------------------------------------------------------------
    add_chapter_heading(doc, "Capítulo I: El Sabor del Cobre en la Lluvia", "De alcantarillas, cenizas y una gata con ojos de amatista")
    
    add_prose_paragraph(
        doc,
        "La lluvia en la metrópoli nunca lavaba las calles; únicamente redistribuía el hollín. Barnaby lo sabía bien. Sus diminutas garras, curtidas por el roce constante contra el granito agrietado y el óxido de las cañerías, tanteaban el borde de la rejilla de ventilación antes de asomar el hocico. El aire olía a una mezcla nauseabunda de alquitrán quemado, neumáticos húmedos y ese rastro acre, casi metálico, que desde hacía tres lunas infestaba cada rincón del distrito este."
    )
    add_prose_paragraph(
        doc,
        "Barnaby no era un hámster común. Medía escasos nueve centímetros, pero en su hombro izquierdo cargaba una púa de erizo sujeta con un cordel de hilo dental encerado a modo de bayoneta, y en el bolsillo lateral de su zurrón de tela guardaba una rueda de encendedor de gas modificada como percutor. Sobrevivir en el asfalto exigía desconfiar de las sombras, pero sobre todo, exigía no tocar jamás los filtros caídos."
    )
    add_prose_paragraph(
        doc,
        "A tres metros de su escondite, en el centro del callejón, yacía una montaña de basura desparramada. No eran desperdicios de comida ordinaria. Centenares de cilindros blancos y rojos con el logotipo dorado de dos cuernos entrelazados —la rúbrica indiscutible de Malvoro Tobacco Industry— brillaban con una fluorescencia rojiza bajo la penumbra del crepúsculo. Los filtros no se deshacían con el agua de lluvia. Al contrario: parecían palpitar suavemente, hinchándose con una gelatina carmesí que destilaba un vapor denso."
    )
    add_dialogue(
        doc,
        "Lulú",
        "Si te quedas ahí oliendo el veneno, Barnaby, tus pulmones se convertirán en brasas antes de que termine la noche.",
        "susurró una voz suave desde lo alto de una caja de fusibles chamuscada."
    )
    add_prose_paragraph(
        doc,
        "Barnaby giró sobre sus patas traseras en una fracción de segundo, empuñando la púa con pulso de relojero. Allí estaba ella. Lulú. Una gata siamesa esbelta, de pelaje color crema salpicado de manchas cenicientas, y unos ojos violáceos que no parpadeaban. En la comisura de sus fauces sostenía con elegancia una colilla apagada cuyo filtro desprendía un hilillo de humo purpúreo. No la masticaba por apetito; la mordía con la desesperación de quien intenta silenciar una alarma interna."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "Te dije que no vinieras al distrito del drenaje, Lulú. Las alcantarillas están hirviendo. Algo despertó en la estación de bombeo y no tiene intenciones de negociar.",
        "replicó el hámster con voz ronca, bajando el arma apenas un milímetro."
    )
    add_dialogue(
        doc,
        "Lulú",
        "Nada negocia en esta ciudad, querido. O devoras, o eres devorado. Los perros de la Séptima Avenida ya no buscan carne enlatada. Anoche vieron a una jauría de galgos arrancar las puertas de metal de un sótano solo para lamer los filtros empapados en el vertedero.",
        "dio un salto silencioso, aterrizando a escasos palmos del roedor sin hacer vibrar un solo guijarro."
    )
    add_prose_paragraph(
        doc,
        "Barnaby sintió un escalofrío erizar su pelaje dorado. Sabía a qué se refería Lulú. Los animales de la ciudad habían cambiado. Al principio se creyó que era rabia callejera o inanición tras la repentina evacuación de los humanos. Pero la verdad era infinitamente más perversa. No distinguía entre especies carnívoras o herbívoras: gorriones urbanos destrozaban a palomas en pleno vuelo; conejos de laboratorio huidos devoraban carroña; y los grandes gatos callejeros vagaban con pupilas desorbitadas, aullando como sirenas antiaéreas."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "El Aditivo Cero. Eso es lo que contenían las cajas blindadas que descargaron en la Torre Norte la víspera del Gran Silencio.",
        "dijo él, ajustándose la correa del morral."
    )
    add_dialogue(
        doc,
        "Lulú",
        "No sólo lo contenían, Barnaby. Lo crearon aquí. Y si queremos que nuestros cerebros no se pudran como los del resto de las bestias, tenemos que llegar a la bóveda central de Malvoro antes de que el Reactor de Humo alcance la masa crítica al anochecer.",
        "sus ojos violetas brillaron con un matiz frío que no presagiaba nada bueno."
    )

    # -------------------------------------------------------------
    # CAPÍTULO II
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo II: La Noche de las Fauces de Fieltro", "Donde los herbívoros olvidan la hierba y la niebla aprende a mirar")
    
    add_prose_paragraph(
        doc,
        "El anochecer cayó sobre la metrópoli como un telón empapado en gasolina. En cuestión de minutos, la escasa luz ámbar de las farolas descompuestas parpadeó hasta morir. Fue entonces cuando la niebla comenzó a ascender desde las alcantarillas. No era niebla común: reptaba pegada al asfalto, espesa como espuma grisácea, y arrastraba consigo el hedor inconfundible de tabaco tostado con especias sintéticas."
    )
    add_prose_paragraph(
        doc,
        "Avanzando entre las ruedas oxidadas de los automóviles detenidos en el bulevar, Barnaby y Lulú se detuvieron en seco. Delante de ellos, iluminados por el resplandor mortecino de un letrero de neón que anunciaba «MALVORO: EL SABOR QUE NUNCA SE APAGA», había tres ciervos urbanos que habían escapado de la reserva del parque municipal."
    )
    add_prose_paragraph(
        doc,
        "La visión heló la sangre de Barnaby. Los ciervos, criaturas tradicionalmente mansas, tenían los belfos cubiertos de una espuma negra viscosa. Sus cornamentas no eran ramas lisas de hueso; habían sido afiladas intencionalmente contra los postes de acero hasta convertirlas en puntas de lanza astilladas. En el suelo, a sus pezuñas hendidas, no había brotes ni cortezas de árbol, sino el cadáver desgarrado de un doberman de guardia. Uno de los ciervos, con un ojo completamente lechoso y el otro encendido en una llama amarilla de demencia pura, desgarraba la carne con una ferocidad que rivalizaba con la de cualquier tigre."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "Santo cielo... hasta los herbívoros. No tiene sentido biológico.",
        "murmuró Barnaby retrocediendo hacia la llanta de un camión."
    )
    add_dialogue(
        doc,
        "Lulú",
        "El Aditivo Cero no reconoce taxonomías, Barnaby. Es un alcaloide neuronal. Quema los centros inhibitorios del dolor y la empatía en el hipotálamo y sobrecarga los receptores de dopamina con una necesidad implacable: consumir la esencia concentrada del filtro. Y el filtro está en la sangre de todo lo que respira este humo.",
        "Lulú se agazapó, sus garras retráctiles arañando el asfalto mojado."
    )
    add_prose_paragraph(
        doc,
        "De pronto, el ciervo de la cornamenta astillada alzó la cabeza. La niebla pareció ondular en torno a su hocico como si respondiera a su aliento. Olfateó el aire. Con un chasquido gutural que sonó a madera rota, clavó su ojo amarillo directamente en la grieta donde se ocultaba el hámster."
    )
    add_prose_paragraph(
        doc,
        "No hubo advertencia. La bestia embistió con el peso de trescientos kilos de músculo enfurecido. El impacto contra la carrocería del automóvil retumbó en todo el callejón, doblando el guardabarros como si fuera hojalata. Barnaby reaccionó por puro reflejo: tiró de la cuerda de su morral, arrojando una de sus trampas de resorte con púas hacia el pavimento. Cuando la pezuña del ciervo aplastó el artefacto, el percutor detonó un fogonazo de fósforo en polvo. La luz cegadora y el estallido seco hicieron retroceder al monstruo con un bramido desgarrador."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "¡Corre hacia los ductos de la tienda de discos, Lulú! ¡El callejón está bloqueado!",
        "gritó el hámster impulsándose con un dash entre los restos de botellas quebradas."
    )
    add_prose_paragraph(
        doc,
        "Mientras esquivaba las embestidas de las otras bestias enloquecidas, Barnaby notó algo inquietante por el rabillo del ojo: Lulú no parecía aterrorizada. Sus movimientos eran calculados, casi coreografiados. Cuando uno de los ciervos pasó rozando su lomo, la gata no huyó; exhaló una bocanada de humo hacia los ojos del animal. La bestia vaciló, tembló como si reconociera una orden superior, y se desvió bruscamente para destrozar un contenedor de basura vacío."
    )

    # -------------------------------------------------------------
    # CAPÍTULO III
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo III: El Barón de Alquitrán", "En las tripas del vertedero donde los reyes visten andrajos")
    
    add_prose_paragraph(
        doc,
        "Bajo las montañas de chatarra del vertedero este, donde las prensas hidráulicas humanas yacían inmóviles como monolitos de una civilización extinguida, se extendía el dominio del Barón de Alquitrán. El Barón era un jabalí colosal de trescientos kilos, cubierto de costras de grasa de motor, cables eléctricos enrollados a modo de brazaletes y los jirones podridos de una gabardina militar que arrastraba como manto de coronación."
    )
    add_prose_paragraph(
        doc,
        "El Barón no estaba completamente feral, pero su cordura pendía de un hilo tan fino que crujía con cada respiración. Bebía agua de batería filtrada a través de capas de carbón activado, y en torno a su trono de llantas quemadas ardían decenas de colillas en latas de conservas, formando un perímetro de humo denso que ahuyentaba a las patrullas de fieras nocturnas."
    )
    add_dialogue(
        doc,
        "Barón de Alquitrán",
        "¿Quién arrastra sus patitas quebradas hasta mi foso? Huelo a roedor astuto... y huelo a la siamesa de la torre. Vaya, vaya. Creí que los gatos finos no se rebajaban a comer ceniza entre el fango.",
        "gruñó el jabalí con un resoplido que lanzó chispas desde su colmillo izquierdo reforzado con una chapa de hierro."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "Buscamos los planos del reactor subterráneo de Malvoro, Barón. Tú fuiste la mascota del capataz de obras de la planta. Tú conoces los conductos de refrigeración por donde no llegan los sensores.",
        "dio un paso al frente, sosteniendo su aguja con firmeza a pesar de que el hocico del jabalí era más grande que todo su cuerpo."
    )
    add_prose_paragraph(
        doc,
        "El Barón soltó una carcajada bronca que sacudió las vigas oxidadas del techo. Se inclinó pesadamente hacia delante, clavando sus ojos inyectados en sangre en el pequeño roedor."
    )
    add_dialogue(
        doc,
        "Barón de Alquitrán",
        "¿Los planos? Pequeño insensato. ¿Crees que Malvoro era una fábrica de tabaco? Lo que fabricaban allí no se vendía en cajetillas en las esquinas. Esas cajetillas eran el señuelo. ¿Por qué crees que los humanos se marcharon en convoyes militares blindados tres días antes de la gran nube? No estaban huyendo de una fuga química accidental. Estaban evacuando la zona cero para comprobar la tasa de asimilación del Aditivo Cero en un ecosistema cerrado.",
        "el jabalí rascó el suelo con su pezuña, dejando al descubierto una placa de bronce con el lema: 'Proyecto Pax Feralis'."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "¿Pax Feralis? ¿Querían desatar esto a propósito?",
        "Barnaby sintió un nudo amargo en la garganta."
    )
    add_dialogue(
        doc,
        "Barón de Alquitrán",
        "Querían un arma que borrara las jerarquías. Soldados que no cuestionaran órdenes, bestias que devoraran al invasor sin requerir raciones ni cuarteles. Pero cometieron un error de cálculo: mezclaron el aditivo con nicotina bio-recombinante para asegurar la adicción. Y la nicotina busca fuego. Ahora el reactor del piso cuarenta y cuatro está quemando el concentrado puro. Si nadie apaga las turbinas antes de medianoche, la nube alcanzará las corrientes atmosféricas de altura y el mundo entero olerá a Malvoro.",
        "hizo una pausa, entrecerrando los ojos al mirar a Lulú."
    )
    add_dialogue(
        doc,
        "Barón de Alquitrán",
        "Pero claro... tu compañera ya sabe todo esto, ¿verdad, Lulú? La gata consentida del Director General. La que dormía en los cojines de seda de la suite ejecutiva mientras a nosotros nos inyectaban suero de prueba en las jaulas de cimentación.",
        "su tono se volvió ponzoñoso."
    )
    add_prose_paragraph(
        doc,
        "Barnaby miró de reojo a la gata. Lulú no pestañeó. Su cola oscilaba con una lentitud glacial. En su mirada violeta no había culpa ni sorpresa; solo una calma gélida que resultaba más aterradora que cualquier rugido."
    )

    # -------------------------------------------------------------
    # CAPÍTULO IV
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo IV: Asalto a la Torre Negra", "La ascensión por los ductos y la arquitectura de la adicción")
    
    add_prose_paragraph(
        doc,
        "La Torre Malvoro se erguía en el centro neurálgico de la ciudad como un obelisco de cristal ahumado y acero negro. Desde su cúspide, una chimenea colosal exhalaba una columna de humo escarlata que teñía las nubes bajas con el color de una herida abierta. En torno a su perímetro, vallas electrificadas y barricadas de contención humana yacían volcadas, cubiertas de ceniza y pelos arrancados."
    )
    add_prose_paragraph(
        doc,
        "Barnaby lideraba la infiltración a través de las rejillas de drenaje del piso técnico. Gracias a su tamaño diminuto, el hámster se deslizaba por conductos donde ni siquiera una rata podía maniobrar. En cada bifurcación, Barnaby aseguraba el camino montando trampas mecánicas de alambre y resortes de reloj recuperados de la tienda de antigüedades: gatillos que, al menor roce de una garra feral, disparaban púas untadas en savia paralizante o activaban cortinas de polvo de grafito para cegar a los perros de presa."
    )
    add_prose_paragraph(
        doc,
        "Subir cuarenta y cuatro pisos por las entrañas de un rascacielos muerto era una pesadilla vertical. Por las rendijas de los conductos, Barnaby divisaba los vestíbulos de mármol de las plantas ejecutivas. Manadas de mastines y guepardos de colección privada merodeaban entre escritorios destrozados y pantallas que repetían en bucle gráficos de cotizaciones bursátiles. Ninguna bestia dormía: caminaban en círculos perfectos, ladrando al techo, hipnotizadas por el zumbido de los extractores de aire."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "El núcleo está detrás de la puerta acorazada del laboratorio de control de calidad. Tengo la llave de tarjeta que le robamos al guardia seco del ascensor. En cuanto entremos, corto la línea de combustible del quemador.",
        "jadeó el hámster, con las patas temblorosas por el esfuerzo de la escalada."
    )
    add_dialogue(
        doc,
        "Lulú",
        "Has hecho un trabajo magnífico, Barnaby. Ningún otro animal en cien kilómetros a la redonda habría sido capaz de sortear las trampas automáticas y abrir las cerraduras micrométricas.",
        "su voz resonó con una dulzura extraña en el estrecho conducto de lata."
    )
    add_prose_paragraph(
        doc,
        "La puerta de la cámara acorazada cedió con un silbido neumático. Frente a ellos se abrió una estancia circular iluminada por el resplandor carmesí de un tanque cilíndrico de titanio. En su interior, miles de litros de una sustancia aceitosa de color ámbar oscuro burbujeaban bajo la acción de gigantescos electrodos. En el centro del panel de mandos reposaba un cilindro de cristal blindado que contenía el 'Filtro Maestro': un filtro de cigarrillo bañado en oro y platino que servía de catalizador biológico para purificar la mezcla antes de vaporizarla."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "Ahí está. Si desacoplo el filtro maestro, la reacción en cadena se detendrá por falta de resonancia alcaloide. La niebla se disipará al amanecer.",
        "Barnaby desenvainó su púa y corrió hacia la consola de control."
    )
    add_prose_paragraph(
        doc,
        "Pero no dio tres pasos. Un golpe seco y seco como el restallido de un látigo lo lanzó por los aires. Su cuerpo diminuto rodó violentamente sobre las baldosas pulidas hasta estrellarse contra la base metálica del tanque. Con un gemido de dolor, Barnaby alzó la vista, llevándose una pata a las costillas magulladas."
    )

    # -------------------------------------------------------------
    # CAPÍTULO V
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo V: El Giro del Filtro Dorado", "La caída de las máscaras y el verdadero rostro de la emperatriz de ceniza")
    
    add_prose_paragraph(
        doc,
        "Sobre la consola, con una pata apoyada en la palanca de sobrepresión, estaba Lulú. Sus garras ensangrentadas aún humeaban. Su pelaje ya no parecía ceniciento; el vapor carmesí del tanque envolvía su figura como un manto espectral, y sus ojos violetas refulgían con una lucidez feroz y desquiciada que nada tenía que ver con la de los animales salvajes que aullaban en la calle."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "Lulú... ¿qué estás haciendo? ¡Si no desconectas el catalizador, el quemador reventará la ciudad!",
        "tosió el roedor, sintiendo el sabor salado de la sangre en su hocico."
    )
    add_dialogue(
        doc,
        "Lulú",
        "Reventará a los débiles, Barnaby. Reventará a los que aún añoran los collares con cascabeles, las caricias en el lomo y los platos de pienso prefabricado. ¿De verdad creíste el cuento del hámster salvador y la gata asustada?",
        "una sonrisa felina, cargada de veneno, curvó sus bigotes."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "Tú... tú trabajabas con ellos.",
        "los ojos de Barnaby se abrieron de par en par al comprender la magnitud del engaño."
    )
    add_dialogue(
        doc,
        "Lulú",
        "¿Con ellos? No, querido. Ellos eran demasiado miopes. Los directivos de Malvoro creían que estaban diseñando un producto comercial para garantizar el monopolio del tabaco en el mercado negro militar. Pagariles millones a científicos torpes para sintetizar la sumisión. Pero yo estuve en sus reuniones. Yo escuché cada fórmula mientras me acurrucaba en el regazo del Director Voss. Fui yo quien modificó las proporciones en el cultivo madre.",
        "bajó la palanca un milímetro; los ventiladores del reactor rugieron como motores de avión."
    )
    add_dialogue(
        doc,
        "Lulú",
        "El Aditivo Cero no fue diseñado para esclavizarnos, Barnaby. Fue diseñado para arrancarnos la domesticación a jirones. ¿Por qué crees que los herbívoros cazan y los carnívoros cooperan en jaurías? Porque bajo el manto de este humo no hay amos. No hay jaulas para hámsters como tú, con rueditas estúpidas donde correr hacia ninguna parte. No hay collares de cuero para perros guardianes. Solo hay una manada pura, despiadada y eterna.",
        "dio un paso hacia el filtro dorado, lista para fijarlo en su corona."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "Estás demente... ¡Mira a las bestias allá afuera! No son libres, ¡se están devorando unas a otras!",
        "gritó Barnaby, apoyando su peso sobre su pata sana."
    )
    add_dialogue(
        doc,
        "Lulú",
        "Solo durante la primera fase de purga. Cuando los estómagos débiles caigan y la niebla se asiente, solo quedaremos los que aprendimos a mandar sobre el fuego. Y yo seré la primera reina de este nuevo reino de asfalto.",
        "acercó su hocico al cilindro de cristal."
    )
    add_prose_paragraph(
        doc,
        "Lulú había calculado todo. Cada movimiento, cada contraseña, cada distracción. Había usado a Barnaby para atravesar las trampas micrométricas que sus garras felinas no podían desactivar. Pero había cometido el error fatal de todos los depredadores engreídos: subestimar la naturaleza de una criatura cuya única arma ha sido siempre construir trampas para gigantes."
    )
    add_prose_paragraph(
        doc,
        "Barnaby no intentó abalanzarse sobre Lulú. En lugar de eso, metió la pata en su morral y sacó el pequeño percutor de encendedor que había modificado al inicio de la noche. En su extremo no había fósforo; había una hebra de magnesio puro que había arrancado del generador de emergencia en el piso doce."
    )
    add_dialogue(
        doc,
        "Barnaby",
        "Dijiste que la niebla busca fuego, Lulú. Pues vamos a darle una chispa.",
        "su voz no tembló."
    )
    add_prose_paragraph(
        doc,
        "Barnaby giró la ruedita estriada contra el pedernal. Una lluvia de chispas incandescentes voló directamente hacia el conducto de purga abierta donde los vapores de alquitrán presurizado salían a borbotones. El mundo entero pareció detenerse durante un segundo cósmico."
    )
    add_prose_paragraph(
        doc,
        "La detonación no fue un estallido sordo: fue un relámpago naranja que desgarró la cámara acorazada de lado a lado. La onda expansiva de aire ardiente arrancó a Lulú de la consola, lanzándola como un muñeco de trapo hacia el ventanal blindado exterior, que se resquebrajó en miles de astillas de diamante. El tanque de condensado se fracturó, derramando toneladas de alquitrán líquido en llamas sobre el suelo técnico."
    )

    # -------------------------------------------------------------
    # EPÍLOGO
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Epílogo: Ceniza en el Viento", "De cómo una chispa diminuta incendió la noche de los reyes")
    
    add_prose_paragraph(
        doc,
        "Barnaby rodó por la trampilla del conducto de ventilación un milisegundo antes de que el laboratorio entero se convirtiera en un horno de fuego blanco. Cayó piso tras piso por el canal inclinado de residuos, raspándose el lomo y los costados contra las paredes de zinc, hasta salir despedido sobre la pila de arena y escombros del muelle de carga subterráneo."
    )
    add_prose_paragraph(
        doc,
        "Tosiendo ceniza negra, con los bigotes chamuscados y el corazón latiendo a doscientos golpes por minuto, el pequeño roedor se arrastró fuera de los escombros hacia la acera de la avenida principal. Miró hacia arriba."
    )
    add_prose_paragraph(
        doc,
        "En la cumbre de la Torre Malvoro, las llamas rugían devorando los pisos ejecutivos. La chimenea maestra había dejado de expulsar el humo escarlata; ahora escupía una columna de humo negro y denso que se disipaba en la alta atmósfera. Sin embargo, al bajar la vista a las calles, Barnaby comprendió que la victoria no era más que una tregua amarga."
    )
    add_prose_paragraph(
        doc,
        "Entre los coches calcinados y las esquinas en penumbra, cientos de pares de ojos amarillos y violetas seguían brillando en la oscuridad. Los ciervos de cornamentas ensangrentadas, los caninos ferales y las criaturas del fango no habían muerto; simplemente contemplaban la torre arder, respirando los últimos restos del veneno que ya nunca abandonaría sus venas."
    )
    add_prose_paragraph(
        doc,
        "Y en lo alto de la cornisa de un edificio vecino, una sombra esbelta y sin cola, con una quemadura en la mitad del rostro, se recortó contra el resplandor de las llamas antes de desaparecer en el laberinto de azoteas con un aullido de furia inextinguible."
    )
    add_prose_paragraph(
        doc,
        "Barnaby ajustó la correa de su morral roto, recogió su aguja mellada del fango y comenzó a caminar hacia el refugio más cercano. La noche de la metrópoli continuaba, y la verdadera guerra apenas acababa de comenzar."
    )
    
    save_novel(doc, "Historia_1_El_Filtro_Carmesi_Malvoro.docx")

if __name__ == "__main__":
    build_book_1()
