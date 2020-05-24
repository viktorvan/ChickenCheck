# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## 1.2.0 - New frontend model and Api interface
* Refactor frontend model to use a flat model
* Make the api more generic cmd/queries

## 1.1.2 - Bugfix
* Handle moving tag, when re-releasing an existing version.

## 1.1.1 - Build cleanup
* Remove local nuget source in build script.

## 1.1.0 - Add Release notes
* Add view for release notes
* Reference ViktorVan.Fake.AzureCLI as single file 

## 1.0.5 - Build fixes
* revert FakeUtils
* cleanup build.fsx

## 1.0.4 - Build fixes
* Better release tagging in build.fsx
* Upgrade FakeUtils.

## 1.0.3 - Fixes
* Add missing auto-generated ReleaseNotes.fs file
* Update build script to correctly handle relase tagging
* Update build script with FakeUtils 1.0.5

## 1.0.2 - Show release version in navbar
* Parse release notes and show version in navbar
* Update FSharp.Core to 4.7.0

## 1.0.1 - Tag release 

## 1.0.0 - First release
* Functionality to list chickens and their eggs per date
* Statistics for total egg count