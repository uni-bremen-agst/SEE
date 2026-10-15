# Dashboard Artifacts Storage

The files in this directory are the master-copies of the artifacts of the projects migrated via
/Results/Dashboard/upload=true in axivion_config.

They are managed by the Dashboard Server and thus should not be manually dealt with. 

Notable exception is e.g. ``cidbman version remove`` for deleting analysis versions
to shrink database size.
Note, that you will need to consult the Dashboard Server ``Projects`` page in order to find out
the current project path as it will change after every analysis run. Also if an analysis
is currently running on an edited database, it won't be able to succeed any more.

## Backing up the files in this folder
* When backing up files from this folder, be sure to also back up the file ``dashboard2.db`` as well
* It is safer to temporarily stop the Dashboard Server while creating the backup
* Should you need to restore from a backup, please consult ``axivion.support@qt.io``

## .trash files
Under certain circumstances, files no longer necessary cannot be deleted. At Dashboard Server
startup, such files are cleaned up. To prevent data loss, when a file is falsely recognized
as not needed anymore, the files are not deleted but renamed to
``old-name.date-when-detected-as-leftover.trash``. The Dashboard server will never delete
(or use) this ``.trash`` files but they can be deleted manually.
