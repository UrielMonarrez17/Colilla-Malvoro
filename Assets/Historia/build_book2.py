import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from novel_styler import (
    create_novel_document, add_book_title, add_epigraph,
    add_chapter_heading, add_prose_paragraph, add_dialogue,
    add_divider, save_novel
)

def build_book_2():
    doc = create_novel_document()
    
    add_book_title(
        doc,
        title="EL HAMBRE DEL CIERVO DE ÉBANO",
        subtitle="La Inversión del Ecosistema y el Jardín de Hueso",
        author="Volumen II — Colección de Crónicas de la Metrópoli Feral"
    )
    
    add_epigraph(
        doc,
        "La fábula humana nos enseñó a temer los colmillos del lobo y la garra del oso. Olvidaron que los que comen raíces conocen los secretos de la tierra profunda. Cuando la tierra se corrompe de amargura, el ciervo no huye: afila sus astas y devora la carne de sus verdugos.",
        "Códice Botánico del Invernadero Oeste, Inscripción anónima"
    )

    # -------------------------------------------------------------
    # CAPÍTULO I
    # -------------------------------------------------------------
    add_chapter_heading(doc, "Capítulo I: El Huerto de Esporas Púrpuras", "Donde las margaritas sangran y las liebres olvidan las zanahorias")
    
    add_prose_paragraph(
        doc,
        "Silas vivía dentro de una bombilla rota de seiscientos vatios, encajada entre las vigas retorcidas del viejo Invernadero Botánico Central. Desde aquella altura privilegiada, observaba cómo la ciudad se convertía en un tapiz biológico alienígena. Las paredes de cristal que antaño protegían orquídeas exóticas y helechos tropicales estaban destrozadas; por sus marcos colgaban gruesas lianas de un hongo micótico de color violáceo brillante que palpitaba con un ritmo idéntico al latido de un corazón mamífero."
    )
    add_prose_paragraph(
        doc,
        "A diferencia de los roedores vulgares que huían despavoridos por las cloacas, Silas era un estudioso. Llevaba una pequeña mochila tejida con musgo deshidratado donde guardaba tubos de ensayo improvisados con casquillos de fusibles y una lupa de relojero sujeta a su ojo derecho con un hilo de cobre. Había visto el inicio de la Gran Inversión."
    )
    add_prose_paragraph(
        doc,
        "Ocurrió doce amaneceres atrás. El asfalto comenzó a quebrajarse, pero no por terremotos: filamentos blancos y sedosos —micelios de una cepa desconocida que los documentos del laboratorio llamaban *Phalloides Aeterna*— brotaron por millones a través del alquitrán. Lo aterrador fue su primer huésped. No fueron los carnívoros. Fueron los herbívoros del parque municipal."
    )
    add_dialogue(
        doc,
        "Silas",
        "Día diecisiete de observación. Los conejos del prado norte han dejado de mostrar interés en el trébol silvestre. Su dentadura incisiva ha aumentado de densidad en un trescientos por ciento; las raíces de sus molares han comenzado a secretar una sustancia ácida que disuelve el hueso de sus presas.",
        "susurraba Silas mientras anotaba con una plumilla de gorrión en su libreta de papel de arroz."
    )
    add_prose_paragraph(
        doc,
        "Abajo, en el suelo del invernadero, una docena de liebres de pelaje ceniciento devoraban con metódica frialdad los restos de un zorro rojo. No había gruñidos ni disputas por los trozos de carne: se alimentaban en un silencio sepulcral, sincronizadas, masticando con un crujido seco y continuo que recordaba al de tijeras de podar cortando ramas secas. Cada liebre lucía en la frente una pequeña excrecencia esponjosa de color púrpura que emitía un brillo tenue en la penumbra."
    )
    add_dialogue(
        doc,
        "Bridget",
        "No te quedes mirándolas tanto tiempo, pequeño alquimista. Cuando las esporas te reconocen, la curiosidad se convierte en apetito.",
        "croó una voz profunda y áspera desde una rama de hierro forjado sobre su cabeza."
    )
    add_prose_paragraph(
        doc,
        "Silas alzó la vista. Bridget, la anciana lechuza del campanario de San Judas, estaba posada sobre el tensor de acero. Sus ojos estaban cubiertos por dos cataratas blancas como perlas, pero su cuello giraba con una precisión milimétrica hacia cada respiración viva. Había perdido la vista cuando los humanos bombardearon los depósitos de gas con fósforo blanco, pero a cambio había desarrollado un oído capaz de escuchar a las hormigas caminando sobre el zinc."
    )
    add_dialogue(
        doc,
        "Silas",
        "Bridget, no tiene explicación metabólica. El aparato digestivo de un lepórido no puede sintetizar queratina ni grasa animal sin sufrir una necrosis hepática fulminante. Y sin embargo, las liebres están más vigorosas que nunca. Sus músculos se han duplicado.",
        "dijo Silas, señalando con la plumilla a la manada."
    )
    add_dialogue(
        doc,
        "Bridget",
        "Porque ya no son liebres, Silas. Son los dedos de Kaelan. Y Kaelan está hambriento.",
        "la lechuza erizó sus plumas cenicientas con un escalofrío visible."
    )

    # -------------------------------------------------------------
    # CAPÍTULO II
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo II: La Corona de Espinas Negras", "La aparición de Kaelan y el banquete en la rotonda")
    
    add_prose_paragraph(
        doc,
        "Kaelan no era una leyenda urbana; era un titán de pesadilla. Había sido el semental estrella del zoológico metropolitano, un ciervo rojo de porte aristocrático que los niños solían fotografiar tras las rejas de madera del recinto de recreo. Pero la bestia que ahora cruzaba la Puerta de los Leones no conservaba rastro alguno de docilidad."
    )
    add_prose_paragraph(
        doc,
        "Su pelaje, antaño castaño rojizo, se había teñido de un negro obsidiana tan puro que absorbía la luz de la luna. Medía casi dos metros y medio hasta la cruz, y sus patas traseras poseían la musculatura nudosa de un felino cazador. Pero lo que causaba un pavor reverencial era su cornamenta: una estructura monumental de astas bifurcadas de metro y medio de envergadura, completamente forrada por el micelio carnoso de *Phalloides Aeterna*."
    )
    add_prose_paragraph(
        doc,
        "De las puntas de sus astas colgaban, como macabros frutos de cosecha, cráneos limpios de perros de presa, linces y gatos callejeros, entrelazados con lianas bioluminiscentes que goteaban néctar alcaloide. Sus pezuñas hendidas estaban afiladas como cuchillas de carnicero y dejaban hendiduras humeantes en el asfalto congelado."
    )
    add_prose_paragraph(
        doc,
        "Silas y Bridget observaban desde el tejado del museo adyacente a la rotonda. En el centro de la plaza circular, Kaelan se detuvo. No emitió un bramido de celo común; de su garganta brotó un tono armónico de ultratumba, una vibración grave que hizo tintinear los cristales de los escaparates a tres manzanas de distancia."
    )
    add_prose_paragraph(
        doc,
        "A su llamado, las sombras de las calles convergieron. No eran ciervos solos: centenares de palomas con picos aserrados por calcificaciones, conejos de ojos vacíos, ardillas arborícolas de garras prensiles y cabras enanas del zoológico infantil rodearon la rotonda en círculos concéntricos. Formaban un ejército silencioso. En el centro de la formación, arrastrando sus cadenas, trajeron a una presa: un lobo siberiano de las colinas que había descendido a cazar a la metrópoli, ahora derribado, atado con raíces y quejumbroso."
    )
    add_dialogue(
        doc,
        "Silas",
        "Por los dioses... van a sacrificarlo.",
        "susurró Silas, apretando su frasco de resina contra el pecho."
    )
    add_dialogue(
        doc,
        "Bridget",
        "No es un sacrificio, muchacho. Es una transfusión. Escucha con atención.",
        "murmuró la lechuza girando la cabeza noventa grados."
    )
    add_prose_paragraph(
        doc,
        "Kaelan bajó su majestuosa testuz. Una de las puntas afiladas de su cornamenta penetró con precisión quirúrgica el costado del lobo. De la herida no brotó sangre roja ordinaria: los filamentos del hongo que cubrían las astas del ciervo comenzaron a succionar los fluidos del carnívoro como raíces sedientas en tierra de sequía. A medida que el lobo se desinflaba hasta convertirse en un pellejo seco, las flores violetas de la cornamenta de Kaelan se abrieron, despidiendo una nube de polen fosforescente que cubrió a la multitud reunida."
    )
    add_prose_paragraph(
        doc,
        "Los herbívoros aspiraron el polen con éxtasis místico. Sus cuerpos se estremecieron; sus ojos destellaron con la misma llama violeta de su señor. La cadena trófica de millones de años de evolución había sido destrozada y reconstruida en una sola noche."
    )

    # -------------------------------------------------------------
    # CAPÍTULO III
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo III: El Laboratorio de Savia y Púas", "El hámster ingeniero contra la inteligencia del bosque de brea")
    
    add_prose_paragraph(
        doc,
        "Silas sabía que la observación pasiva ya no era suficiente. El micelio avanzaba cuarenta metros por noche hacia los túneles del metro donde se refugiaban las últimas familias de ardillas, conejillos de indias y ratones que aún no habían sucumbido al hongo. Si no encontraba una sustancia que disolviera las hifas sin matar al portador, toda la vida consciente de la ciudad sería asimilada."
    )
    add_prose_paragraph(
        doc,
        "En su mesa de trabajo —una caja metálica de galletas invertida con patas de tornillo oxidado— Silas trabajaba sin descanso. Había montado una red de defensas en el perímetro del invernadero: trampas de gravedad compuestas por tubos de cobre suspendidos con contrapesos de tuercas, trampas de resortes armadas con resortes de pinzas de la ropa capaces de partir el cuello a una liebre acechante, y zanjas minadas con vidrio molido y ácido de baterías de automóvil."
    )
    add_prose_paragraph(
        doc,
        "Con unas pinzas hechas con agujas de tejer, Silas diseccionó el fragmento de hongo que Bridget había arrancado de la oreja de una cabra muerta. Al aplicar una gota de aceite esencial de pino y azufre, el tejido micótico reaccionó con violencia: se retorció como un gusano escaldado y emitió un silbido agudo, liberando un hedor que Silas reconoció de inmediato."
    )
    add_dialogue(
        doc,
        "Silas",
        "Huele a alquitrán... A tabaco procesado.",
        "abrió los ojos con perplejidad."
    )
    add_dialogue(
        doc,
        "Bridget",
        "El tabaco no crece en el asfalto por capricho divino, pequeño. Antes de que los hombres se hicieran humo, la fábrica del norte vertió miles de toneladas de abono sintético en los colectores del parque para cultivar su hierba modificada. El hongo no nació en el bosque: nació en los vertederos químicos de Malvoro Tobacco Corp.",
        "la lechuza limpió su pico contra una viga."
    )
    add_dialogue(
        doc,
        "Silas",
        "No es una planta, Bridget. ¡Es un circuito! Las partículas de nicotina sintetizada actúan como neurotransmisores conductores de baja resistencia. El micelio no solo devora carne: transporta señales eléctricas. Cada liebre, cada paloma, cada pezuña de Kaelan es una neurona de un cerebro que se extiende bajo el suelo de toda la metrópoli.",
        "Silas sintió que el suelo bajo sus patitas vibraba de pronto con una regularidad aterradora."
    )
    add_prose_paragraph(
        doc,
        "Un estrépito de cristales rotos resonó en la entrada principal del invernadero. Las trampas de la entrada se activaron: tres chasquidos secos de resortes y el chirrido agudo de metal chocando contra roca. Pero no hubo chillidos de agonía. El enemigo no venía en número de tres o cuatro. Venía en legión."
    )

    # -------------------------------------------------------------
    # CAPÍTULO IV
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo IV: El Asedio del Domo de Cristal", "Dientes en la sombra y la caída de la vieja lechuza")
    
    add_prose_paragraph(
        doc,
        "Cincuenta liebres de ceniza entraron en tromba por la brecha del invernadero. Detrás de ellas, una docena de ciervos jóvenes con astas en llamas de fuego fatuo golpeaban los pilares de hierro que sostenían la cúpula. La estructura centenaria crujió como el casco de un barco hundiéndose en una tormenta."
    )
    add_prose_paragraph(
        doc,
        "Silas no perdió la calma. Saltó al interruptor de su trampa principal: un cable de cobre conectado a un condensador de microondas cargado con un panel solar que había instalado en el techo. Al bajar la palanca con su peso corporal, una descarga eléctrica de cuatrocientos voltios recorrió la red de alambre gallinero tendida sobre el suelo húmedo."
    )
    add_prose_paragraph(
        doc,
        "El relámpago azulado iluminó el invernadero con la fuerza de un mediodía artificial. Una veintena de liebres cayó fulminada al instante, con humo negro saliendo de sus orejas. Pero las demás ni siquiera miraron a sus compañeras caídas. Treparon sobre los cadáveres humeantes con una determinación robótica, clavando sus garras en las lianas para ascender hacia la bombilla de Silas."
    )
    add_dialogue(
        doc,
        "Bridget",
        "¡Sal de ahí, Silas! ¡El domo no resistirá! ¡El rey del bosque está en la puerta!",
        "chilló la lechuza abriendo sus enormes alas de plumas grises."
    )
    add_prose_paragraph(
        doc,
        "La puerta monumental del invernadero voló en pedazos. Kaelan entró al galope. Su cornamenta descomunal enganchó la viga maestra de soporte y la arrancó del suelo con un tirón brutal de su cuello titánico. Toneladas de hierro forjado y cristal astillado cayeron en una lluvia mortal sobre la sala de exposiciones."
    )
    add_prose_paragraph(
        doc,
        "Bridget se lanzó en picado desde las sombras. A pesar de su ceguera, sus garras de acero se clavaron con furia implacable en los ojos cubiertos de micelio del ciervo. Kaelan bramó con un sonido que partió los tímpanos de Silas. La bestia sacudió su cabeza como un toro embravecido; una de las púas afiladas de sus astas atravesó el pecho de la anciana lechuza, ensartándola contra la columna de hormigón."
    )
    add_dialogue(
        doc,
        "Bridget",
        "Huye... al corazón... del jardín de piedra... El micelio no tiene amo... Kaelan es solo... el altavoz...",
        "fueron las últimas palabras que escaparon de su pico antes de que su cuerpo se apagara para siempre."
    )
    add_prose_paragraph(
        doc,
        "Aprovechando el segundo en que la bestia forcejeaba para liberar su cornamenta del cadáver de la lechuza, Silas cortó la cuerda de su paracaídas de seda de oruga. Cayó suavemente hacia el conducto de drenaje del suelo, esquivando las mandíbulas chasqueantes de dos liebres enfurecidas, y se deslizó hacia la oscuridad de las cloacas con su zurrón de muestras apretado contra el pecho."
    )

    # -------------------------------------------------------------
    # CAPÍTULO V
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Capítulo V: El Giro de la Raíz Madre", "Donde se descubre que los humanos nunca se fueron")
    
    add_prose_paragraph(
        doc,
        "Las cloacas del distrito botánico no desembocaban en el río; desembocaban en la cripta geológica excavada bajo el parque durante la construcción del metro subterráneo. Arrastrándose por una grieta húmeda entre tuberías rotas, Silas llegó a una caverna monumental que no figuraba en ningún mapa de la superficie."
    )
    add_prose_paragraph(
        doc,
        "La caverna estaba viva. Millones de raíces micóticas púrpuras cubrían las paredes de basalto, convergiendo hacia una estructura colosal en el centro del abismo: un bulbo vegetal del tamaño de un edificio de tres plantas que latía con un fulgor violeta enceguecedor. De su superficie salían gruesos cordones umbilicales que se bifurcaban hacia el techo, conectándose directamente con las raíces de los árboles del parque y las tuberías de drenaje de la metrópoli."
    )
    add_prose_paragraph(
        doc,
        "Silas sacó su frasco de destilado de azufre y resina de pino concentrada. Tenía suficiente mezcla química para prender fuego al bulbo madre y romper la red neural de Kaelan de un solo golpe. Se acercó cautelosamente al núcleo, preparando su pedernal."
    )
    add_prose_paragraph(
        doc,
        "Fue entonces cuando ajustó su lupa de relojero a la membrana translúcida del bulbo."
    )
    add_prose_paragraph(
        doc,
        "Lo que vio a través del tejido celular hizo que el frasco de cristal cayera de sus patas y se rompiera en silencio sobre el lodo."
    )
    add_prose_paragraph(
        doc,
        "Dentro del bulbo no había savia ni semillas alienígenas. Había cuerpos humanos. Cientos de ellos. Hombres, mujeres y niños, suspendidos en una matriz amniótica de néctar ámbar, con zarcillos púrpuras conectados a sus nucas y espinas dorsales. Sus rostros no mostraban dolor ni agonía; sus labios estaban curvados en una leve sonrisa de placidez absoluta, y sus ojos se movían en rápidos ciclos de sueño profundo."
    )
    add_prose_paragraph(
        doc,
        "Una voz suave, modulada por miles de cuerdas vocales humanas hablando al unísono a través de las hifas del hongo, resonó en la mente de Silas con una claridad abrumadora:"
    )
    add_dialogue(
        doc,
        "La Voz de la Raíz",
        "Bienvenido a nuestro reposo, pequeño tejedor de trampas. No tiembles ante la verdad que tus ojos diminutos contemplan.",
        "las raíces vibraron con una armonía maternal."
    )
    add_dialogue(
        doc,
        "Silas",
        "¿Quiénes son ustedes? ¿Los científicos de Malvoro los encerraron aquí?",
        "logró balbucear Silas, encogiéndose sobre sus patas traseras."
    )
    add_dialogue(
        doc,
        "La Voz de la Raíz",
        "Nadie nos encerró, Silas. Nosotros creamos el micelio. Nosotros modificamos el tabaco y el fertilizante para sembrar nuestra propia liberación. Nuestra civilización estaba enferma de codicia, guerras de asfalto y humo venenoso. Estábamos muriendo de soledad en jaulas de cemento. El hongo no fue un accidente: fue la medicina que unió nuestras mentes fracturadas en un solo sueño armónico y eterno.",
        "los zarcillos acariciaron suavemente el pelaje del hámster sin dañarlo."
    )
    add_dialogue(
        doc,
        "Silas",
        "¿Y los animales? ¡Están enloqueciendo! Kaelan está matando a todo ser viviente allá arriba...",
        "reclamó el roedor con desesperación."
    )
    add_dialogue(
        doc,
        "La Voz de la Raíz",
        "Kaelan es nuestro sistema inmunológico, Silas. La superficie debe ser purificada de la vieja violencia depredadora. Los carnívoros reinaron durante milenios sobre el miedo de los mansos. Ahora, los herbívoros limpian las avenidas para que nada interrumpa nuestro sueño. Cuando la última fiera salvaje sea consumida y convertida en abono, Kaelan descansará, el micelio florecerá en jardines puros y los animales cuerdos como tú heredarán las calles bajo un cielo limpio.",
        "una raíz se extendió hacia él, ofreciéndole una gota de néctar púrpura brillante."
    )
    add_dialogue(
        doc,
        "La Voz de la Raíz",
        "Bebe, Silas. Apaga tus miedos de hámster solitario. Únete a la mente colmena que nunca vuelve a sentir frío ni hambre.",
        "la voz prometía paz, calor y descanso eterno."
    )
    add_prose_paragraph(
        doc,
        "Por un instante, Silas vaciló. Recordó las noches gélidas en su bombilla rota, el terror de las garras en la oscuridad, el cansancio infinito de buscar semillas en contenedores de basura infectados. La oferta era tentadora. Bastaba un lengüetazo para no volver a sentir miedo jamás."
    )
    add_prose_paragraph(
        doc,
        "Pero entonces vio en el suelo de la caverna las plumas desgarradas de Bridget. Recordó su graznido valiente, su espíritu indomable que prefería morir ciega y libre antes que ser parte del ganado mental de nadie."
    )
    add_dialogue(
        doc,
        "Silas",
        "Un sueño donde no puedes elegir tus propios pasos no es paz. Es un matadero con música dulce.",
        "Silas dio un paso atrás, con la mirada encendida en dignidad roedora."
    )

    # -------------------------------------------------------------
    # EPÍLOGO
    # -------------------------------------------------------------
    add_divider(doc)
    add_chapter_heading(doc, "Epílogo: Semillas bajo el Asfalto", "De cómo un hámster decidió permanecer despierto")
    
    add_prose_paragraph(
        doc,
        "Silas no quemó la raíz madre. Comprendió que destruir el bulbo mataría instantáneamente a los miles de humanos que dormían en su interior y desataría una marea de esporas tóxicas que asfixiaría a toda criatura viviente en cien leguas a la redonda."
    )
    add_prose_paragraph(
        doc,
        "En lugar de eso, extrajo de su morral tres semillas de roble centenario que había rescatado del jardín antes del colapso: semillas puras, inmunes a la química sintética de Malvoro. Las plantó con cuidado entre las hifas del bulbo, regándolas con las gotas de lluvia que se filtraban por la fisura de roca."
    )
    add_prose_paragraph(
        doc,
        "«Si han de soñar con un bosque», susurró Silas al barro fértil, «que sueñen con árboles de verdad, y no con redes de alambre y tabaco». Luego dio media vuelta y comenzó el largo ascenso hacia la superficie."
    )
    add_prose_paragraph(
        doc,
        "Al emerger por una alcantarilla en el distrito oeste, la noche aún cubría la ciudad. A lo lejos, sobre el puente de la autopista, se recortaba la silueta titánica de Kaelan el Ciervo de Ébano, con sus astas púrpuras brillando contra la niebla. La bestia lo miró a la distancia durante un largo instante. No atacó. Quizá reconoció en el pequeño hámster al único ser vivo que conocía el secreto de su amo dormido."
    )
    add_prose_paragraph(
        doc,
        "Silas ajustó su lente de relojero, apretó su plumilla y continuó su camino por el callejón. El mundo estaba loco, pero mientras una sola criatura recordara cómo pensar por sí misma, la noche nunca sería invencible."
    )
    
    save_novel(doc, "Historia_2_El_Hambre_Invertida.docx")

if __name__ == "__main__":
    build_book_2()
