# Tsila Run — édition Method

Le jeu utilise les modèles, les textures et le logo fournis dans `Assets/TsilaRunArt`. La palette est vert néon, violet et vert sombre. Le logo Method et la mention « © Copyright by Method » restent visibles dans la zone sûre de l'écran.

## Jouer dans Unity

1. Ouvrir le projet avec Unity **6000.6.3f1**, puis attendre la compilation.
2. Quitter le mode Play et choisir **Tools > Tsila Run > Create or Update Mobile Prototype**. Enregistrer les scènes ouvertes si Unity le demande.
3. L'outil importe les assets Method et ouvre `Assets/TsilaRun/Generated/Scenes/TsilaRun.unity` avec toutes les références raccordées.
4. Mettre la vue Game en portrait, par exemple **720 × 1280**, puis cliquer sur Play dans Unity.
5. Cliquer sur **JOUER**. **C'EST PARTI** permet de passer l'introduction.

Clavier de test : A/D ou flèches gauche/droite pour changer de voie, Espace/flèche haut pour sauter, S/flèche bas pour glisser, Échap pour mettre en pause. Sur téléphone, utiliser les quatre gestes de glissement.

## Menu et magasin

Le menu montre le véritable personnage 3D sur un socle, avec une animation d'attente et une rotation lente de gauche à droite. Le titre TSILA RUN pulse légèrement. **MAGASIN** affiche uniquement **TSILA ORIGINAL — ÉQUIPÉ** : aucun article à acheter et aucune dépense possible depuis cette interface.

Les pièces collectées restent sauvegardées localement. Les données des anciennes tenues sont conservées pour éviter une perte de progression, mais le jeu affiche toujours le modèle Method de base.

Le bouton **MENU** est accessible pendant la course et l'introduction. **RETOUR AU MENU** existe aussi sur la pause et les résultats. Il termine la course, sauvegarde les pièces et le record, remet le joueur et le monde à zéro et restaure le temps normal. Le retour depuis l'arrière-plan demande toujours une reprise explicite d'une course interrompue.

## Modèles et animations

- Tsila, l'agent et les autres coureurs utilisent les squelettes et clips fournis : Idle, Run, Jump et Slide, avec transitions courtes. Les mouvements restent sur place ; le gameplay déplace le monde.
- La pose basse du clip Slide est maintenue jusqu'à ce que le collider puisse se relever. Le personnage n'est plus écrasé par une mise à l'échelle verticale.
- Les barrières, obstacles hauts, portiques, pièces et routes utilisent les nouveaux meshes. Les colliders restent les volumes simples déjà utilisés par le jeu.
- Les pivots des pièces et de la route sont adaptés à la convention du jeu. Les trois décors recyclés restent présents : île, montagnes et souterrain.
- Les sources JSON, textures et l'importeur restent dans `Assets/TsilaRunArt`. L'importeur reconstruit son sous-dossier `Generated`. Les wrappers de gameplay et la scène sont générés dans `Assets/TsilaRun/Generated`.

## Résolution et fluidité

L'interface s'adapte à la résolution et à la zone sûre, y compris aux sorties portrait **2160 × 3840**. Cela ne transforme pas les textures source 1K ni le logo fourni en textures 4K. Le jeu ne force pas un rendu 4K sur un téléphone : il utilise la résolution de la fenêtre/de l'appareil et vise 60 images/s.

Le menu réutilise le même personnage et la même caméra que la course. Aucun rendu vers une texture supplémentaire, post-traitement lourd ni ombre dynamique n'est ajouté. Les routes, obstacles et pièces restent limités par leurs pools. La fluidité réelle, la chauffe et la consommation mémoire doivent être mesurées sur téléphone ; aucune performance mobile mesurée n'est annoncée.

Les instructions Android/iOS sont dans [TsilaRun-Guide.md](TsilaRun-Guide.md). Les résultats exécutés sont dans [TsilaRun-Verification.md](TsilaRun-Verification.md). Tester sur téléphone les boutons du menu, la pause, le retour depuis l'arrière-plan, les gestes et les collisions avant publication.
