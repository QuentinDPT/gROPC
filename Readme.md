# gROPC

gROPC est un package d'acquisition de données d'un OPC.

La partie hébergé sur le serveur s'occupe de se connecter à un server OPC unique.
(un server gROPC ne s'associe qu'à un seul server OPC)

Pour la partie client, il faudra renseigner l'addresse ainsi que le port du server gROPC.

## Procédures

### Abonnements

Lorsque le client souhaite s'abonner à une valeur de l'OPC, il lui sera donné un identifiant de thread serveur gROPC pour qu'il puisse l'interrompre.
Pour interrompre un abonnement, il suffit de donner l'identifiant à la methode unsubscribe.

![#f03c15](https://via.placeholder.com/15/f03c15/000000?text=+) `Attention, il n'y a pas de verification lors d'une demande d'unsubscribe. il es possible d'arrêter un abonnement d'un autre client`

Les identificatns client/server d'abonnement, ne correspondent pas à l'identifiant de l'abonnement OPC.

### Ecriture

Pour effectuer un ecriture, il faut donner au serveur OPC la valeur de la node qu'on veut pouvoir modifier.
pour cela il faut aller dans les paramètres de l'application gROPC serveur et donner la liste des nodes à whitelist dans la variable "whitelist".

Si la valeur ne peut pas être lue pour une raison de droits, une erreur sera envoyé au client suite à une réponse "NOK" de la part du serveur.



## Améliorations

 - donner des GUID aux client pour ne plus avoir de problèmes d'identifiants qui se suivent (trop simple pour arrêter d'aurtes clients),
 - associer chaque identifiants à un client pour eviter de fermer un abonnement qui n'appartien pas au bon client,
 - en savoir plus sur le comportement de l'ensemble applicatif en cas de perte de connection.