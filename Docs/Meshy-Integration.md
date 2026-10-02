# Modèles Meshy intégrés dans Tsila Run

Les fichiers GLB originaux sont conservés dans `Assets/TsilaRun/Art/AI`. Les exports optimisés et leurs textures se trouvent dans `Assets/TsilaRun/Art/Meshy/Source`, et les matériaux URP, animations et prefabs dans `Assets/TsilaRun/Art/Meshy/Generated`.

## Correspondance des 12 fichiers

| GLB fourni | Utilisation dans le jeu |
| --- | --- |
| tsila.glb | Personnage joueur, menu et boutique |
| officier.glb | Personnage poursuivant, avec sa tenue Meshy originale |
| road.glb | Route commune aux trois zones, modules de 24 mètres |
| border.glb | Bordures de la route |
| barriere.glb | Tour haute à contourner : la forme fournie n'est pas une barrière basse |
| obstacle-slide.glb | Portique, ouverture ajustée pour la glissade |
| coins.glb | Pièces à collecter |
| aimant.glb | Bonus aimant |
| arbre-tropical.glb | Arbres de la zone île |
| palmier.glb | Palmiers de la zone île |
| house.glb | Maisons de la zone île |
| immeuble.glb | Immeubles de la zone île |

La barrière basse, le coureur adverse, le bouclier, le bonus de vitesse et les décors de montagne/tunnel conservent les assets existants. Les trois zones restent disponibles.

## Préparation

- Réduction des maillages pour éviter d'utiliser les personnages et maisons de plus d'un million de triangles dans le jeu.
- Personnages d'environ 20 000 triangles pour le menu et de 8 000 triangles pour le LOD de gameplay. Les valeurs exactes sont dans `Manifest.json`.
- Soudure des sommets de couture sur les copies optimisées, nouvelles UV et projection des couleurs depuis les modèles originaux. Cela conserve aussi les transformations de texture des GLB quantifiés.
- Textures de couleur en 2048 pixels pour les personnages, 1024 pour les objets et décors. Matériaux URP opaques ; feuillages visibles des deux côtés.
- Les normal maps sont disponibles mais désactivées dans les matériaux mobiles actuels.
- Squelettes et animations de base `Idle`, `Run`, `Jump`, `Slide` ajoutés aux deux personnages, car les GLB fournis n'en contenaient pas. Ces animations sont une première intégration procédurale, pas des animations de motion capture.
- Visages, cheveux, mains et chaussures utilisent un matériau séparé des vêtements. La boutique teinte les vêtements sans remplacer les textures du visage.
- Les composants, dimensions des colliders, contrôles, recyclage du monde et objets de progression restent ceux du jeu.

## Ouvrir le jeu

Captures vérifiées dans l'Editor : [menu](Verification/Meshy-Menu.png), [jeu](Verification/Meshy-Run.png), [poursuite](Verification/Meshy-Intro.png), [montagne](Verification/Meshy-Mountain.png) et [tunnel](Verification/Meshy-Tunnel.png). Les 32 captures couvrent huit écrans/états à quatre résolutions portrait, de 720 × 1280 à 2160 × 3840. Les captures manuelles utilisent un LOD fixe pour éviter la superposition de deux niveaux hors de la boucle normale de rendu.

Les **27 tests Editor sélectionnés passent** : [résultats XML](Verification/Meshy-EditorTests.xml). Ils couvrent les modèles texturés, les LOD, le mouvement réel de l'Animator, la glissade, les références de scène, les contrôles tactiles synthétiques, les collisions, la collecte, les menus, la boutique et une simulation de 110 km. Le test de régénération de scène est exclu car il réécrit la scène ; la capture automatique déjà exécutée séparément n'est pas répétée pendant cette suite.

La simulation a révélé un chevauchement rare entre une pièce et un coureur mobile. Le placement des pièces réserve maintenant tout le déplacement relatif possible du coureur, avec un test de régression dédié. Le test long utilise une graine fixe et autorise les traînées de pièces à dépasser l'origine de la dernière rangée dans le module de route suivant. Le test d'animation applique la transition de l'Animator avant d'avancer son horloge.

Ouvrir `Assets/TsilaRun/Generated/Scenes/TsilaRun.unity`, attendre la compilation et lancer Play. Le pack est déjà connecté aux prefabs et à la scène.

Pour réappliquer les exports préparés : sortir du mode Play et choisir **Tools > Tsila Run > Apply Meshy Art Pack**. Chaque application sauvegarde les anciens prefabs et la scène dans `Logs/MeshyBackup-*`.

Le chargeur de visuels préfère les prefabs Meshy lorsqu'ils existent et utilise le pack Blender existant pour les éléments manquants. Les GLB ne demandent aucun nouveau package Unity : les conversions FBX fournies sont utilisées par le jeu.

## Refaire les exports depuis les GLB

Le script `Tools/prepare_meshy.py` fonctionne dans un processus Blender séparé, sans modifier la scène Blender ouverte. Lancer Blender en arrière-plan avec `--python Tools/prepare_meshy.py`, puis appliquer le pack dans Unity. On peut limiter la préparation avec `-- tsila officier` ou d'autres noms de fichiers sans extension.

Les proportions du squelette sont adaptées aux deux modèles fournis. Un nouveau personnage dans une pose ou des proportions différentes demandera de vérifier et d'ajuster le rig, pas seulement de remplacer le fichier.

Les performances restent à mesurer sur un téléphone réel ; les budgets de triangles et les vérifications dans l'Editor ne garantissent pas une fréquence d'images sur tous les appareils. L'ancien APK n'est pas mis à jour par une modification de la scène : reconstruire l'application avant un test Android.
