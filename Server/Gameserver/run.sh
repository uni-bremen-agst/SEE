#!/bin/sh
set -e

SOURCE=/multiplayer_data
TARGET=/app/server_Data/StreamingAssets/Multiplayer

echo '** Collecting files...'
rm -rf "$TARGET"
mkdir -p "$TARGET"
for _file in "$SOURCE"/*; do
  [ -e "$_file" ] || break
  if [ "$(echo "${_file##*.}" | tr '[:upper:]' '[:lower:]')" = "zip" ]; then
    echo "    - Extracting: ${_file}"
    unzip -qq "${_file}" -d "$TARGET"/
  else
    echo "    - Linking: ${_file}"
    ln -s "${_file}" "$TARGET"/
  fi
done
echo '** Done collecting files!'

/app/server.x86_64  \
    -launch-as-server  \
    -port 7777  \
    -domain "$SEE_BACKEND_DOMAIN"  \
    -id "$SEE_SERVER_ID"  \
    -password "$SEE_SERVER_PASSWORD"
