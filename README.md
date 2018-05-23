# N17 Solutions Octopus Deploy CLI
Allows creation and promotion of releases within Octopus Deploy

# Create Release
dotnet octopus release -s|--server [SERVER] -k|--api-key [APIKEY] -p|--project-name [PROJECT_NAME] -sv|--sem-ver [SEMVER]

# Promote Release
dotnet octopus promote -s|--server [SERVER] -k|--api-key [APIKEY] -p|--project-name [PROJECT_NAME] -sv|--sem-ver [SEMVER] -e|--environment [ENVIRONMENT_NAME]
