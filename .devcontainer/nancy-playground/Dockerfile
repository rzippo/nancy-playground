FROM debian:bookworm

RUN apt-get update && \
    apt-get install -y sudo wget curl libicu72 git \
    openjdk-17-jdk \
    # To run Chromium and Firefox
    libxkbcommon0 libgbm1 libgtk-3-0

# Create the non-root user, with sudo access
ARG USER_UID=1000
ARG USER_GID=$USER_UID
ARG USER_NAME=npdev
RUN groupadd --gid $USER_GID ${USER_NAME} \
    && useradd --uid $USER_UID --gid $USER_GID -m ${USER_NAME} \
    && echo ${USER_NAME} ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/${USER_NAME} \
    && chmod 0440 /etc/sudoers.d/${USER_NAME}

# Add powershell 7.6.1, using deb package
USER root
RUN wget https://github.com/PowerShell/PowerShell/releases/download/v7.6.1/powershell_7.6.1-1.deb_amd64.deb
RUN dpkg -i powershell_7.6.1-1.deb_amd64.deb
## Cleanup
RUN rm powershell_7.6.1-1.deb_amd64.deb

# Add dotnet 8.0, 9.0 and 10.0, using user-local install
USER ${USER_NAME}
RUN wget https://dot.net/v1/dotnet-install.sh -O /home/${USER_NAME}/dotnet-install.sh && \
    chmod +x /home/${USER_NAME}/dotnet-install.sh
RUN /home/${USER_NAME}/dotnet-install.sh --channel 8.0
RUN /home/${USER_NAME}/dotnet-install.sh --channel 9.0
RUN /home/${USER_NAME}/dotnet-install.sh --channel 10.0
## Cleanup
RUN rm /home/${USER_NAME}/dotnet-install.sh

ENV PATH="/home/${USER_NAME}/.dotnet:${PATH}"
ENV DOTNET_ROOT="/home/${USER_NAME}/.dotnet"

# Install dontnet tools for test coverage and report generation
ENV PATH="/home/${USER_NAME}/.dotnet/tools:${PATH}"
RUN dotnet tool install --global coverlet.console --version 10.0.0
RUN dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.5.6

# Add ANTLR command line tools
USER root
RUN wget https://www.antlr.org/download/antlr-4.13.2-complete.jar -O /usr/local/lib/antlr-4.13.2-complete.jar
COPY --chown=root --chmod=755 common/antlr4 /usr/local/bin/antlr4
COPY --chown=root --chmod=755 common/grun /usr/local/bin/grun

# This assumes CLASSPATH is currently empty
ENV CLASSPATH=".:/usr/local/lib/antlr-4.13.2-complete.jar"

# Set the default user as non-root
USER ${USER_NAME}
