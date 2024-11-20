#!/bin/sh

# Path to the JSON configuration file
CONFIG_FILE="/peeringconfig.json"

# Output directory for RPKI-client
RPKI_OUTPUT="/var/lib/rpki-client"
RPKI_CACHE="$RPKI_OUTPUT/cache"
RPKI_TA="$RPKI_OUTPUT/ta"

# File to sync to the routers
RPKI_FILE="bird"

# Folder on remote routers
REMOTE_FOLDER="/etc/bird/rpki/"


# Check if jq is installed (required to parse JSON)
if ! command -v jq >/dev/null 2>&1; then
  echo "jq is required but not installed. Please install jq."
  exit 1
fi

# Read the Rpki field from the JSON file
RPKI_ENABLED=$(jq -r '.Rpki' "$CONFIG_FILE")

# Run RPKI-client if Rpki is true
if [ "$RPKI_ENABLED" = "true" ]; then
  if [ ! -d "$RPKI_CACHE" ]; then
    # Create the cache directory if it doesn't exist
    mkdir -p "$RPKI_CACHE"
    echo "Directory $RPKI_CACHE created."
  fi
  chown -R _rpki-client:_rpki-client $RPKI_OUTPUT
  echo "Running RPKI-client..."
  /usr/sbin/rpki-client -Bv $RPKI_OUTPUT

  if [ $? -ne 0 ]; then
    echo "Error: RPKI-client failed."
    exit 1
  fi
  echo "RPKI-client finished successfully."

  # Extract Hostnames from the JSON file
  HOSTNAMES=$(jq -r '.RouterMapping[].Hostname' "$CONFIG_FILE")

  # Sync the RPKI file to each router
  for HOSTNAME in $HOSTNAMES; do
    echo "Syncing $RPKI_FILE to $HOSTNAME:$REMOTE_FOLDER..."
    rsync -avz "$RPKI_OUTPUT/$RPKI_FILE" "$HOSTNAME:$REMOTE_FOLDER"
    ssh root@$HOSTNAME "chown -R bird:bird /etc/bird; /usr/sbin/birdc configure"

    if [ $? -ne 0 ]; then
      echo "Error: Failed to sync to $HOSTNAME."
    else
      echo "Successfully synced to $HOSTNAME."
    fi
  done

  echo "RPKI sync completed."
else
  echo "RPKI is disabled in the configuration. Exiting."
  exit 0
fi
