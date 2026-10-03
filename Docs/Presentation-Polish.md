# Présentation et lisibilité du jeu

Le menu montre le titre, le personnage sur sa plateforme et deux actions : PLAY et SHOP. La police [Rajdhani](https://fonts.google.com/specimen/Rajdhani) est intégrée avec TextMeshPro pour le titre et les actions du menu ; sa licence OFL accompagne la source. Le fond utilise un dégradé navy et un halo vert. La plateforme a un bord émissif, un éclairage violet et un bloom limité au menu. Les boutons ont une entrée décalée, un rebond discret et un son bref. Les contrôles apparaissent dans un tutoriel au premier PLAY.

Le Canvas utilise 1080 × 1920, Match 0.5 et la Safe Area. En course, le HUD permanent se limite à la distance, aux pièces et à la pause. Les missions ne s'affichent que lorsqu'elles sont terminées ; les bonus apparaissent pendant leur activation. Le menu reste accessible depuis la pause.

## Décors et modèles

Une seule surface de route est utilisée. Les collections de pièces détachées, les packs regroupant de nombreux arbres et les tunnels superposés sont conservés dans Art et retirés du parcours. Les pivots et dimensions des modèles statiques sont mesurés dans leur parent réel, y compris les transformations de la hiérarchie d'origine des GLB. La normalisation utilise un parent aux axes du jeu, pour ne pas déformer les modèles dont la racine FBX est tournée. Les deux LOD utilisent explicitement leurs matériaux mobiles texturés.

Sur l'île, un palmier, un arbre et une maison alternent sur trois tronçons : chacun revient tous les 72 mètres. Un bâtiment plus éloigné apparaît tous les 120 mètres. Les objets restent à l'extérieur de l'enveloppe de la route. Le sol est du sable avec des variations et un grain discret, légèrement sous le niveau du bitume. Les rochers et arbres de montagne sont également espacés. La répartition dépend du tronçon absolu, de sorte que le recyclage des routes ne change pas les décors visibles à chaque image.

Le GLB ModularTunnel est une collection de pièces séparées, avec des supports au milieu des voies. Il reste disponible dans Art. Le parcours utilise des murs continus, un plafond et des bandes de lumière, avec les trois voies dégagées. Les fichiers GLB bruts ajoutés après la préparation du pack ne sont pas automatiquement des modèles jouables : le manifeste décrit les 30 modèles préparés et vérifiés.

## Animations et collisions

Les animations de course et de saut utilisent le squelette des personnages fournis. Les poses sont recalées sur le sol ; une seconde animation de déplacement du modèle, qui entrait en conflit avec le squelette, a été retirée. Le début de poursuite garde l'officier à distance du joueur et de la caméra ; la caméra passe directement au cadrage d'introduction lors du lancement.

La roulade garde l'échelle du personnage à 1. Elle replie le corps et effectue un tour, synchronisé avec la durée de glissade. Le collider bas est de 1,45 m ; l'ouverture des obstacles concernés est de 1,55 m. Le joueur debout mesure 1,80 m et doit toujours rouler pour passer. Les parties hautes des modèles Overhead sont relevées de 55 cm, sans changer leurs couleurs ni leurs dimensions horizontales. Si le passage empêche encore de se relever, la pose finale reste basse. Les animations sont procédurales ; elles ne remplacent pas une animation de motion capture.

## Vérification

Résultats : **34 tests sélectionnés réussis, 0 échec**, sous Unity 6000.6.3f1 ([rapport](Verification/Presentation-EditorTests.xml)). Les tests couvrent les poses avec l'Animator réel, les deux LOD de roulade à chaque image, le dégagement des décors et du tunnel, l'échelle du personnage, le tutoriel, le lancement, les contrôles, les collisions continues, les pièces et le recyclage sur une longue course. Le test qui régénère toute la scène est exclu pour préserver la scène travaillée ; les captures sont exécutées séparément.

Après le dernier réglage du plafond à 9 mètres, les **6 contrôles de présentation ont été relancés et réussis** ([rapport final](Verification/Presentation-FinalChecks.xml)), avec une assertion garantissant que le plafond reste au-dessus de la caméra.

Les **37 captures** incluent le menu, la course, la poursuite, la boutique, la pause et les trois environnements à quatre résolutions portrait, ainsi que cinq étapes de roulade ([contrôles de débordement](Verification/Presentation-Captures.txt)). Elles figent temporairement les maillages skinnés pour les rendus manuels successifs dans une seule image Editor : les buffers de bones GPU ne se rafraîchissent qu'une fois par image. Ce traitement ne modifie pas les personnages en jeu.

Aperçus : [menu](Verification/Presentation-Menu.png), [course](Verification/Presentation-Run.png), [poursuite](Verification/Presentation-Intro.png), [roulade](Verification/Presentation-Roll.png), [montagne](Verification/Presentation-Mountain.png), [tunnel](Verification/Presentation-Tunnel.png).

Pour réappliquer la présentation : Tools > Tsila Run > Polish Menu and Gameplay, hors du mode Play. Le pack Meshy réapplique aussi les animations améliorées et la répartition des décors.

Les performances, le capteur de mouvement et le rendu final sur téléphone restent à vérifier sur appareil. Les modifications du projet ne reconstruisent pas l'ancien APK.
