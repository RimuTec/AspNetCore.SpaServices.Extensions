#!/bin/sh

echo Running entrypoint.sh

#################################################################################################################
# Provided the Dockerfile doesn't change the user, this script will run as 'root'. However, once VS Code connects
# it will connect remotely as user 'dev' [Manfred, 19sep2021]


#################################################################################################################
# Change ownership of all directories and files in the mounted volume:
chown -R dev:dev /work
# Option '-R' applies the ownerhip change recursively on files and directories in /src


#################################################################################################################
# Finally invoke what has been specified as CMD in Dockerfile or command in docker-compose:
"$@"
