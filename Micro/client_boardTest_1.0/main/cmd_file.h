#include "esp_log.h"

#include <dirent.h>
#include <stdio.h>
#include <stdlib.h>

/*	METHODS	*/
int file_check(char *path);
int file_delete(char *path);
int file_read_full(char *path);
int printDirContent(char *path);

/*	VARIABLES	*/
const char *TAGH = "File";
