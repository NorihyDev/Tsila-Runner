# Pack 3D complet de Tsila Run

Ce document couvre les personnages, obstacles, objets à collecter et décors des trois zones du jeu. C'est un brief de génération, pas un pack de modèles déjà générés. Les noms correspondent aux assets ou aux éléments des générateurs existants. Certaines pièces peuvent être regroupées dans un même modèle : arbre complet, maison avec toit, montagne enneigée, portique complet.

## Utilisation dans Meshy

Générer un asset à la fois. Ajouter le préfixe commun ci-dessous au prompt de chaque ligne. Utiliser la même référence visuelle pour conserver le style. Les dimensions sont des objectifs à vérifier et corriger dans Blender ou Unity : une IA ne garantit pas des mesures exactes.

Préfixe commun :

> Original stylized 3D asset for Tsila Run, a colorful tropical mobile endless runner. Cohesive cartoon art direction, clean readable silhouette, rounded bevels, teal and cream palette with warm accents, low-poly geometry, simple opaque materials, UV mapped, no baked lighting, no text, single isolated asset, no surrounding scene.

Exporter les modèles en FBX avec textures PNG. Conserver les sources dans `Assets/TsilaRun/Art/AI`, hors des dossiers Generated. Garder les composants de gameplay et leurs colliders ; remplacer leurs visuels. Les objets du pack Blender utilisent un pipeline spécifique : importer un FBX ne suffit pas à le connecter automatiquement au jeu.

## Personnages

| Fichier cible | Prompt à ajouter |
| --- | --- |
| Tsila.fbx | Friendly fictional athletic runner, teal sports outfit, cream accents, sneakers, expressive face, short stylized hair, full body, neutral A-pose, approximately 1.8 meters tall. Separate outfit material from skin and hair, suitable for skeletal rigging. |
| Officer_Lucef.fbx | Fictional pursuing police officer, navy uniform, recognizable cap and badge without lettering, sneakers, expressive cartoon face, full body, neutral A-pose, approximately 1.8 meters tall, suitable for skeletal rigging. |
| RunningPerson.fbx | Fictional civilian jogger, coral sportswear, sneakers, readable silhouette distinct from the hero, full body, neutral A-pose, approximately 1.8 meters tall, suitable for skeletal rigging. |

Préparer un squelette compatible et les animations sur place `Idle`, `Run`, `Jump`, `Slide` pour le contrôleur Animator existant. Vérifier le branchement au prefab. Les quatre tenues de Tsila peuvent partager le modèle : Island Teal, Sunset Coral, Golden Trail et Midnight. Garder un matériau de vêtement permettant la personnalisation actuelle ; générer quatre personnages différents n'est pas nécessaire.

## Tous les obstacles et objets à collecter

| Fichier cible | Prompt à ajouter / contrainte |
| --- | --- |
| Barrier.fbx | Low road safety barrier, cream and coral warning panels without text, sturdy rounded design. Main body 1.7 meters wide, 0.85 meters tall, 0.9 meters deep. Clearly jumpable. |
| Overhead.fbx | Road overhead obstacle with two side posts and a thick suspended warning beam, teal frame and coral accents. Total size 2.24 meters wide, 2.8 meters tall, 0.9 meters deep. Beam underside exactly 1 meter above ground, clear opening beneath for sliding. |
| Tower.fbx | Tall solid roadside obstacle tower, stacked industrial blocks with warning accents, flat stable base. Main body 1.8 meters wide, 3.6 meters tall, 1.1 meters deep. Solid central silhouette, must be dodged. |
| Coin.fbx | Collectible gold coin, thick beveled rim, simple embossed original geometric emblem, no lettering. Diameter 0.6 meters, thickness 0.2 meters. |
| CoinMagnet.fbx | Collectible horseshoe magnet, warm gold body with contrasting tips, chunky rounded cartoon silhouette, compact floating pickup. |
| Shield.fbx | Collectible shield emblem, cyan and cream, rounded solid silhouette, simple raised central motif, compact floating pickup. |
| SpeedBoost.fbx | Collectible orange lightning bolt, chunky beveled solid shape, readable from the back camera, compact floating pickup. |

RunningPerson est aussi un obstacle mobile. Adapter les trois bonus aux colliders existants plutôt que créer de nouvelles tailles de collision.

## Route commune aux trois zones

| Fichier cible | Prompt à ajouter / contrainte |
| --- | --- |
| RoadSection.fbx | Straight flat modular road slab, dark blue teal asphalt, 8 meters wide and 24 meters long, slab thickness 0.36 meters below the driving surface. Three equal gameplay lanes with centers at -2.4, 0 and +2.4 meters. Identical cross section at both ends for seamless repetition, no slopes, no obstacles or scenery. |
| Curb.fbx | Straight cream roadside curb module, 24 meters long, 0.2 meters wide, 0.16 meters tall, identical ends for seamless repetition. |
| LaneStripe.fbx | Flat cream lane separator strip, 24 meters long, 0.055 meters wide, approximately 0.012 meters thick, suitable for placing on a flat road. |

Les marquages peuvent aussi être une texture ou un matériau de la route. Le plan de roulement reste à Y=0, avec les extrémités du module à Z=-12 et Z=+12.

## Zone île

| Fichier cible | Prompt à ajouter / contrainte |
| --- | --- |
| IslandGround.fbx | Flat tropical sandy ground module, 30 meters wide, 24 meters long, 0.5 meters thick, subtle stylized sand surface, seamless front and back edges, no objects. |
| Tree.fbx | Complete stylized tropical tree with trunk and lush green canopy, sturdy trunk and rounded leaf clusters, approximately 4 meters tall and 2.7 meters wide, ground pivot, no surrounding terrain. |
| House.fbx | Small stylized tropical roadside house, warm cream walls, teal window frames, simple doors without text. Main building 3.5 meters wide, 4.2 meters tall, 5 meters deep. Separate shallow roof approximately 3.9 by 5.4 meters. No surrounding terrain. |
| HouseRoof.fbx | Shallow tropical house roof, warm coral finish, approximately 3.9 meters wide, 5.4 meters deep and 0.25 meters thick, single isolated roof piece. Only needed when the house is generated without a roof. |

Pour des pièces d'arbre séparées : tronc 0.45 × 1.6 × 0.45 m ; feuillage environ 2.7 × 3.1 × 2.7 m. Un arbre complet peut remplacer les deux visuels.

## Zone montagne

| Fichier cible | Prompt à ajouter / contrainte |
| --- | --- |
| Cliff.fbx | Modular rocky cliff side descending below a road, chunky stylized stone formations, 3 meters wide, 10 meters tall, 24 meters long, matching ends, no road included. |
| SafetyRail.fbx | Straight stylized roadside safety rail module, 24 meters long, cream metal finish with teal accents, rail thickness approximately 0.15 meters wide and 0.25 meters tall, matching ends. |
| Mountain.fbx | Stylized angular mountain scenery, broad clean rock faces, blue grey stone, approximately 11 meters wide, 15 meters tall, 13 meters deep, isolated mountain with no road. |
| SnowCap.fbx | Stylized angular white snow cap for a mountain summit, approximately 5 meters wide, 5 meters tall, 6 meters deep, simple faceted silhouette, separate mesh. Can alternatively be modeled directly on the matching mountain. |
| BridgeDeck.fbx | Straight modular bridge deck underneath a flat road, stylized stone structure, 8.5 meters wide, 24 meters long, 0.5 meters thick, identical ends, no rails and no raised obstacles. |

## Zone tunnel

| Fichier cible | Prompt à ajouter / contrainte |
| --- | --- |
| TunnelWall.fbx | Straight modular stylized stone tunnel wall, clean interior-facing surface, 1 meter thick, 9 meters tall, 24 meters long, matching ends, no attached scenery. |
| TunnelLightStrip.fbx | Straight tunnel wall light strip housing, teal trim and pale cyan luminous panel, 23 meters long, 0.1 meters wide, 0.16 meters tall, simple solid geometry. |
| TunnelRib.fbx | Vertical stylized stone tunnel support rib, 0.3 meters wide, 9 meters tall, 0.8 meters deep, isolated pillar, clean readable bevels. |
| TunnelCeiling.fbx | Flat modular stylized tunnel ceiling slab, 11 meters wide, 24 meters long, 0.6 meters thick, matching ends, visible underside, no hanging obstacles. |
| TunnelCeilingLight.fbx | Rectangular tunnel ceiling light fixture, cream luminous panel with teal frame, 1.5 meters wide, 5 meters long, 0.08 meters thick. |

Les panneaux lumineux demandent un matériau émissif dans Unity. Le modèle seul ne produit pas l'éclairage. Garder libres les trois voies et le passage de la caméra.

## Pièce supplémentaire déjà présente dans le pack Blender

| Fichier cible | Prompt à ajouter |
| --- | --- |
| FloatingPlatform.fbx | Stylized floating stone platform with flat top, teal trim and cream stone body, compact original cartoon design, isolated object. Match the existing prefab dimensions before integration. |

Cette pièce existe dans le pack importé mais n'est pas un type d'obstacle ou de pickup utilisé par RunnerWorld. Elle n'ajoute pas un mécanisme de plateforme au gameplay.

## Vérification et livraison

- Vérifier les dimensions et les passages du saut/glissade après génération ; ne pas se fier uniquement au prompt.
- Fournir UV, textures et FBX pour chaque asset ; une image de présentation n'est pas un modèle 3D.
- Viser initialement 3 000 à 6 000 triangles par personnage et 100 à 800 par petit obstacle ; ajuster selon le rendu et les mesures sur téléphone.
- Limiter les matériaux, partager les textures et prévoir des LOD pour les gros décors. Ces budgets sont des objectifs, pas des garanties de performance.
- Garder les pivots au sol pour personnages et obstacles ; conserver les racines de gameplay à l'échelle (1,1,1).
- Tester les raccords de route, la visibilité des obstacles, les animations et la caméra dans les trois zones.

Le ciel, le brouillard, l'éclairage, les particules et l'interface demandent aussi un travail visuel dans Unity, mais ne sont pas des modèles 3D à générer dans Meshy.
