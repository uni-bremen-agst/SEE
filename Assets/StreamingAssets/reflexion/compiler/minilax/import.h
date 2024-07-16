#ifdef  __EXPORT__
#warning Export included before Import !!!
#endif

#ifdef  __IMPORT__
#warning Import included twice !
#else
#define __IMPORT__
#endif

/* Mit "PUBLIC" eingeleitete Ausdruecke werden als "extern" deklariert. */
/* Sie werden somit als bereits definiert angenommen.                   */

#ifdef         PUBLIC
#undef         PUBLIC
#endif
#define        PUBLIC    extern


/* PRIVATE gekennzeichnete Ausdruecke duerfen nicht includiert werden,  */
/* da sie nicht im Sichtbarkeitsbereich der Aktuellen Datei sind.       */
/* Wird ein als PRIVATE deklarierter Ausdruck includiert wird ein       */
/* Fehler erzwungen.                                                    */

#ifdef         PRIVATE
#undef         PRIVATE
#endif
#define        PRIVATE   xxxxxx
