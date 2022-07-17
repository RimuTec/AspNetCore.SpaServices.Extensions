FROM mcr.microsoft.com/dotnet/sdk:6.0.302
# Version last checked on 17 Jul 2022
# More recent images may be available at https://hub.docker.com/_/microsoft-dotnet-sdk/


###############################################################################
# Install prerequisites and a some useful tools
RUN apt-get update && \
    apt-get upgrade -y && \
    apt-get install -y \
        procps \
        iputils-ping \
        net-tools \
        lsb-release
# procps: to support command 'ps'
# iputils-ping: to support command 'ping' (https://linuxconfig.org/ping-command-not-found-on-ubuntu-20-04-focal-fossa-linux) [Manfred, 19sep2021]
# net-tools: to support command such as 'arp', 'ifconfig', 'netstat', etc. (https://helpmanual.io/packages/apt/net-tools/) [Manfred, 26sep2021]
# lsb-release: to support commmand 'lsb_release -a' [Manfred, 15jan2022]
# You can safely remove the packages you don't want. [Manfred, 08oct2021]


###############################################################################
# Create non-root user
RUN groupadd -g 1000 -r dev && \
   useradd -u 1000 -r -g dev -m -s $(which bash) dev
# Option '-m' to create home directory (see https://askubuntu.com/a/393470)
# Option '-s' to set shell for this user (see comment in https://askubuntu.com/a/393470)
# Option '-r' creates a system user which does not expire (see https://linuxize.com/post/how-to-create-users-in-linux-using-the-useradd-command/)


###############################################################################
# Create working directory. Ownership will be changed in entrypoint.sh which
# executes *after* the volume has been mounted.
RUN mkdir /work

###############################################################################
# Copy entrypoint script into container, make it executable, then make it the entrypoint for the container:
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
# Option '+x' adds executable flag to the file
ENTRYPOINT ["/entrypoint.sh"]


###############################################################################
# Install Node.js
# Install directly from NodeSource to get latest LTS release:
# For details, see https://github.com/nodesource/distributions#debinstall

WORKDIR /usr/local/bin
RUN curl -fsSL https://deb.nodesource.com/setup_lts.x | bash -
RUN apt-get install -y nodejs


###############################################################################
# Install dotnet-outdated (see https://github.com/dotnet-outdated/dotnet-outdated)
RUN runuser -l dev -c "dotnet tool install --global dotnet-outdated-tool"
# runuser installs it as if the non-root user was installing it. This makes it available to that non-root user
ENV PATH "$PATH:/home/dev/.dotnet/tools"
# If the previous doesn't work, consider https://stackoverflow.com/a/59293118/411428 [Manfred, 13nov2020]
