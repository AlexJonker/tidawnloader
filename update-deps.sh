#!/bin/bash

# Dotnet
dotnet tool install --global dotnet-outdated-tool
dotnet outdated --upgrade --recursive --version-lock Major

# Node
npm upgrade
npm i