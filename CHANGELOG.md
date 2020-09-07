# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
* Expenses page
* Statistics charts
* Changelog page, link from version in footer https://fsprojects.github.io/FSharp.Formatting/markdown.html


## [1.8.2] - 2020-09-07
### Added
* DB Backup job

## [1.8.1] - 2020-08-20
### Added
* Show total eggs in statistics

## [1.8.0] - 2020-08-18
### Added
* Plausible Analytics
### Fixed
* DateTime-format in database

## [1.7.0] - 2020-08-17
### Added
* Role-based authorization

## [1.6.2] - 2020-08-11
### Changed
* Use http, and instead set scheme according to x-forwarded-proto.

## [1.6.0] - 2020-08-08
### Changed
* Deploy with helm
* Use https

## [1.5.0] - 2020-08-05
### Added
* Authentication with Auth0

## [1.4.10] - 2020-08-01
### Fixed
* Run server in timezone /Europe/Stockholm

## [1.4.9] - 2020-08-01
### Changed 
* Build-script, git push on deploy

## [1.4.8] - 2020-07-31
### Removed
* Ingress configuration (moved to cluster-wide configuration)

## [1.4.7] - 2020-07-30
### Added
* Ingress configuration

## [1.4.6] - 2020-07-29
### Added 
* NotFound handler

### Fixed
* Styling bug for datepicker

## [1.4.5] - 2020-07-29
### Fixed
* Check database status in /health

## [1.4.4] - 2020-07-29
### Fixed
* Redirect to 'today' when requesting url for future date.

## [1.4.3] - 2020-07-28
### Added
* Kubernetes deployment

### Changed
* Simpler build process, publish all runtimes.

## [1.4.0] - 2020-07-28
### Added
* Health-check endpoint

### Changed
* Use Debian based docker images instead of Alpine. There does not seem to be sqlite support for linux-musl-arm64.

## [1.3.1] - 2020-07-26
### Added
* Docker images 
* Web tests and run on docker image

### Changed 
* Simpler webpack setup.
* Update Changelog format

### Removed
* Azure deployment

## [1.3.0] - 2020-07-20
### Added
* Turbolinks support
* Html views rendered in backend
* Simple frontend scripts for interactivity

### Removed
* Elmish SPA

## [1.2.0] - 2019-11-28
### Changed
* Refactor frontend model to use a flat model
* Make the api more generic cmd/queries

## [1.1.0] - 2019-10-26
### Changed
* Build script: tag-handling
* Remove local nuget source in build script.

## [1.0.0] - 2019-10-16
### Added 
* List chickens
* Add/remove eggs
