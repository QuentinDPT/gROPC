# gROPC

gROPC est un package d'acquisition de données d'un OPC.

La partie hébergé sur le serveur s'occupe de se connecter à un server OPC unique.
(un server gROPC ne s'associe qu'à un seul server OPC)

Pour la partie client, il faudra renseigner l'addresse ainsi que le port du server gROPC.

## Procédures

### Lecture

Pour pouvoir lire une valeur OPC :

``` C++
// prérequis
var OPCService = new gROPCService(serverURL);

// fonction
public static string read_a_value(gROPCService OPCService, string OPCValue){
    string resultat = OPCService.Read(OPCValue);
    return resultat;
}
```

### Ecriture

Pour effectuer un ecriture, il faut donner au serveur OPC la valeur de la node qu'on veut pouvoir modifier.
pour cela il faut aller dans les paramètres de l'application gROPC serveur et donner la liste des nodes à whitelist dans la variable "whitelist".

Si la valeur ne peut pas être lue pour une raison de droits, une erreur sera envoyé au client suite à une réponse "NOK" de la part du serveur.

``` C++
// prérequis
var OPCService = new gROPCService(serverURL);

// fonction
public static void write_a_value<T>(gROPCService OPCService, string OPCValue, T value){
    OPCService.Write(OPCValue, value);
}
```

> Les types implémentés sont `int`, `double`, `float`, `bool`, `string`. Si vous devez utiliser un autre type, vous aurez une exception `OPCUnsupportedType`

> Pour corriger cette erreur, il faudra modifier le package ainsi que le serveur pour qu'ili puisse correctement interprêter le nouveau type.

### Abonnements

Lorsque le client souhaite s'abonner à une valeur de l'OPC, il lui sera donné un objet gROPCSubscription associé à un thread serveur gROPC pour qu'il puisse l'interrompre.
Pour interrompre un abonnement, il suffit de demander à l'objet de se desabonner.

Les identifiants client/server d'abonnement, ne correspondent pas à l'identifiant de l'abonnement OPC.

```C++
// prérequis
var OPCService = new gROPCService(serverURL);

// fonction
public static gROPCSubscription subscribe_to_a_value(gROPCService OPCService, string OPCValue){
    var subscription = OPCService.Subscribe<int>(OPCValue);
    subscription.onChangeValue += ma_fonction;
    return subscription.Subscribe();
}

// fonction d'evenement
public static void ma_fonction(object sender, int valeur){
    Console.WriteLine("Nouvelle valeur : " + valeur);
}
```

### Désabonements

```C++
// prérequis
var OPCService = new gROPCService(serverURL);
var subscription = OPCService.Subscribe<int>(OPCValue);

// .Subscribe(...
// .onChangeValue += ...
// ...

// fonction
public static void unsubscribe_to_a_value(gROPCSubscription subscription){
    subscription.Unsubscribe();
}
```

### Evenements d'abonnement

Plusieurs types d'évenements peuvent être levés aucours d'un abonnement.

 - `.onChangeValue`,
 
 Cet événement est lancé lorsque la valeure suivie par l'abonnement change. Cet événement se produit dans un thread (la fonction lancée s'executera donc dans un thread annexe).

 `void fct(object sender, T value)` (T doit être du type de la valeure observée)

 - `.onConnect`,
 
 Cet énénement s'execute au moment où on demande un abonnement à une valeur et que le serveur nous réponds avec un identifiant. Cet événement se produit dans un thread (la fonction lancée s'executera donc dans un thread annexe).
 
 `void fct(object sender)`

 - `.onDisconnect`,
 
 Cet événement s'execute lorsqu'on demande l'arrêt d'un abonnement. Cette événement n'est pas parallélisé.
 
 `void fct(object sender)`

 - `.onConnectionLost`,
 
 Cet événement s'execute lorsque la connection est perdue entre l'application et le serveur renseigné.

 `void fct(object sender, int attemptsCount)`

## Améliorations
 - 