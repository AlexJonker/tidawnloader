#!/bin/bash

# Dotnet
dotnet tool install --global dotnet-outdated-tool
dotnet outdated --upgrade --recursive

# Node
npm upgrade
npm i