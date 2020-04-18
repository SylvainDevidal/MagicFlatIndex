# MagicFlatIndex
Gestionnaire de fichiers séquentiels indexés

## Cette solution se décompose en deux projets :
- **MagicFlatIndex** : il s'agit du gestionnaire de fichiers séquentiels indexés lui-même
- **TestFlatFile** : il s'agit d'un programme console permettant de tester le bon fonctionnement du gestionnaire

## Pourquoi ce programme ?
Inconditionnel des bases de données, j'ai cependant toujours en tête l'époque où avec trois fois rien (*CPU, RAM, disque*) on arrivait à envoyer des hommes sur la Lune. Aujourd'hui, la moindre montre connectée contient plus de puissance de calcul que l'ensemble des projets Appolo n'ont jamais eu. Et pourtant il nous faut toujours plus de ressources pour effectuer le moindre traitement, rien que pour rédiger un SMS sur un téléphone on utilise une application qui consomme plusieurs centaines de mégooctets !
Ceci est vrai notamment avec la gestion des données : les types XML et maintenant JSON ont, certes, apporté leur lot d'évolutions (données structurées, à taille variable, etc.) mais aussi ont transformé le moindre accès aux données en une véritable usine à gaz.
J'ai donc voulu renouer avec la simplicité et la performance en retournant aux rudiments de l'informatique : le fichier séquentiel indexé.

## Qu'est-ce que c'est que ce "fichier séquentiel indexé" ?
Un fichier séquentiel est un fichier contenant des *records*, tous du même type, tous de même taille. Ainsi, il est très aisé de retrouver la ligne 1653 dans le fichier : il suffit de faire un `File.Seek(1653 * RecordSize)` et on est bien positionné dans le fichier. Pas besoin de lire tout le fichier, encore moins de le charger en mémoire. Non, on va directement manipuler les quelques octets dont on a besoin sur le disque.
Le problème c'est que notre client 1653 n'est pas forcément à la position 1653 dans notre fichier... On a pu avoir des trous dans la numérotation, des insertions dans un ordre aléatoire, etc. Il convient donc de gérer aussi en parallèle un fichier d'index. Ce fichier ne contient que des couples "id = position". Ce fichier est continuellement chargé en mémoire, et permet de faire le lien entre la "clé" du record et la position dans le fichier.

## Est-ce que c'est aussi performant que ça ?
**Oui, oui, oui et encore oui.** *Mais pas toujours.* Si vous stockez des données, si possible de taille moindre (quelques centaines d'octets), que vous n'avez pas besoin de rechercher ces données autrement que par leur identifiant, alors oui. Aucun SGBD sur la planète ne saura être plus rapide. Même si vous travaillez avec des SGBD *in memory*, la simple couche de données (ODBC, OLEDB, etc.) et l'interprétation du langage (SQL) prendra plus de temps qu'il n'en faudra au séquentiel indexé pour restituer et transformer vos données.
Mais histoire de vous mettre l'eau à la bouche : le test insère 10 millions de lignes 60 secondes sur un disque magnétique, puis recherche 100 000 lignes, les charges, puis les modifie ou supprime **en moins de 2 secondes**...

## Si c'est si bien que ça, n'y a-t-il aucune limitation ?
Biensûr que si, évidement... Si vous voulez rechercher tous les produits dont le nom commence par un mot, ou tous les clients qui ont commandé pour plus de 1000 euros les 6 derniers mois... oubliez. Vous n'aurez pas d'autre solution que de multiplier les fichiers d'index, charger massivement des données en mémoire, faire des calculs dans tous les sens... et même là, vous ne retrouverez jamais les performances d'un SGBD ni la simplicité de requêtage du SQL. Le séquentiel index, c'est avant tout pour travailler... de manière séquentielle sur des données simples. Bref, tout ce que vous voudriez stocker en JSON ou en XML, mais pas ce que vous voudriez stocker dans un SGBD.

## Quelles sont les limitations de cette librairie ?
Outre les limites inhérentes au séquentiel indexé que j'ai mentionné dans le paragraphe précédent, cette librairie a été crée avant tout pour illustrer le principe de fonctionnement. Chaque fichier de données ne dispose donc que d'un unique index, qui porte sur l'identifiant. Cet index est forcément de type `int`. Aussi, il ne s'agit pas d'un SGBD : ne vous attendez pas à ce que le code se comporte bien en environement multi-threadé. La librairie pose explicitement des verrous sur les fichiers accédés durant toute la durée de l'instance. Si vous voulez implémenter cette librairie dans un environement partagé, il faudra écrire votre propre "serveur" de données : une unique instance du gestionnaire sera utilisée par l'ensemble des process.
Enfin, mais c'est plus un choix afin de garder un code simple et concis, cette librairie ne travaille qu'avec des données de taille fixe. Ainsi, si vous désirez stocker une description de votre produit, si 99% font moins de 20 caractères, mais qu'une seule fait plus de 20 000 caractères, vous serez obligé de stocker chaque description de produit dans un espace de 20 000 octets, que vous en ayez besoin ou non !

## Quelles sont les évolutions possibles ?
Actuellement le gestionnaire ne bouche pas les trous libérés par des suppressions. Aussi, aucune méthode de réorganisation du fichier (tri, compression des trous) n'est présente.
Si votre programme plante sauvagement et que la méthode `Dispose()` n'est pas appelée, alors le fichier d'index ne sera pas mis à jour. Vous aurez alors des données inconsistantes par rapport à l'index. Une méthode `RebuildIndex()` permet de corriger le problème.
Enfin, actuellement il n'y a qu'un seul index par fichier, qui porte uniquement sur l'Id. Il serait judicieux d'implémenter la possibilité de rajouter des index (soit mono-colonne, soit multi-colonne) afin de permettre des recherches un peu plus avancées telles que retrouver les produits de la famille X ou les clients dans le département Y.
Il ne devrait pas être très compliqué de stocker des string (ou autres types de taille variable) non pas avec une taille fixe, mais avec une taille variable. Il suffirait pour se faire de rajouter en début de chaque propriété du record sa taille. Cependant, la sérialisation/désérialisation de `byte[]` à `object` ne sera pas des plus aisées.
