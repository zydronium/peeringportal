FROM debian:bookworm-slim

# Enable backports
RUN echo "deb http://deb.debian.org/debian bookworm-backports main" > /etc/apt/sources.list.d/backports.list && \
    apt-get update && \
    apt-get install -y rsync jq ssh && \
	apt-get install -y -t bookworm-backports rpki-client && \
    rm -rf /var/lib/apt/lists/*

# Copy your custom script
COPY RPKI-Client/arin.tal /etc/tals/arin.tal
COPY RPKI-Client/run.sh /run.sh
RUN chmod +x /run.sh
# Set the default command for the container
CMD ["rpki-client"]
