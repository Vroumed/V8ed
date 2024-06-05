FROM mcr.microsoft.com/dotnet/sdk:8.0 as build

# Copy project to container
COPY . /src
WORKDIR /src

# Process version.tt into version.cs
RUN dotnet tool install -g dotnet-t4 && cd DeltaDash.Desktop && ~/.dotnet/tools/t4 version.tt

# Restore and build project
ARG PLATFORM
ARG BUILD_TYPE=Debug
ENV NUGET_PACKAGES /cache/nuget
RUN --mount=type=cache,target=/cache \
dotnet publish --sc -c $BUILD_TYPE -r $PLATFORM --verbosity detailed -o /output DeltaDash.Desktop

# Export build artifacts
FROM scratch
COPY --from=build /output .
