#ifdef   __EXPORT__
#warning  Export included twice !!!
#else
#define  __EXPORT__
#endif

#ifndef  __IMPORT__
#warning  No export before import, please !!!
#else
#undef   __IMPORT__
#endif

/* Mit "PUBLIC" eingeleitete Ausdruecke werden in im folgenden,         */
/* exportiert und sind global sichtbar.                                 */

#ifdef         PUBLIC
#undef         PUBLIC
#endif
#define        PUBLIC


/* Mit "PRIVATE" gekennzeichnete Ausdruecke werden im folgenden als     */
/* 'static' definiert und sind nur innerhalb des Files sichtbar.        */

#ifdef         PRIVATE
#undef         PRIVATE
#endif
#define        PRIVATE      static
