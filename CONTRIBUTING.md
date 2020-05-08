# Contributing

Thank you for considering a contribution to NINA!
There are many areas where you can contribute, ranging from improving the documentation, writing tutorials, submitting bugs or even writing code for new features inside NINA itself.
When contributing code or documentation to this repository, please first discuss your plans via an issue, discord or mail with the repo owners, before making a change

# Table of Contents
[TOC]

# Bug Reporting

If you encounter a bug with N.I.N.A. you can report this via the [issue tracker](https://bitbucket.org/Isbeorn/nina/issues?status=new&status=open)  

## Checklist
1. Ensure that the problem still persists on the latest version
2. If the problem still persists, check the issue tracker if there is already an issue open for it
  * An issue for your problem exists  
    * Check that this issue describes the exact same issue that you are having
    * Add all information that you have to the issue. Just adding "me too" won't help us much to resolve it.
    * The more info is available for an issue, the better and faster we can track down the root cause and fix it!
  * If no issue is already existing create a new one

## What to include

For reporting bugs please use the following guideline to describe the problem:
  
```
[ ] Is the issue reproducible?  
[ ] Are you running the latest version?  
[ ] Are all prerequisites that are mentioned inside the manual met?  

# Description #

<Put a short description about the issue here>

# Steps to Reproduce #
* <Step 1>
* <Step 2>
* <and so on>

## Expected behaviour ##
<describe what should happen>
## Actual behaviour ##
<describe what actually happened>
```

Also attach your log file of that session (if applicable), which can be found inside %localappdata%\NINA\Logs

# Contributing documentation

* Documentation is maintained separately in a different [repository](https://bitbucket.org/Isbeorn/nina.docs/src)
* For a detailed guide on how to contribute to the documentation go to [N.I.N.A.'s documentation contributing guide](https://bitbucket.org/Isbeorn/nina.docs/src/develop/CONTRIBUTING.md)

# Contributing code

## Quick Start
1. Fork the repository
2. **Sync LFS files into the fork**
```
git lfs fetch --all <nina root origin>
git lfs push --all <your fork origin>
git lfs pull
```
3. Add your changes
4. Check that unit tests are passing
5. Make sure no unnecessary files are accidentally checked in.
6. Add a short description about your changes to the correct section inside "RELEASE_NOTES.md"
7. Push the change to your forked repository using a good commit message
8. Submit a pull request
9. During the pull requests there will be discussions and constructive feedback.
   Required changes that might be requested during this phase have to be implemented.
   Once this is done, the pull request can be merged.

## Coding rules

* Always be backwards compatible when having some major rework of a module (e.g. settings change)
* Follow clean code guidelines. There are many resources about this topic available online.
* Try to unit test your code

## Branching model

This project is utilizing a standard git flow where it has the following branches  
* master: all officially released code  
* hotfix/<hotfixname>: used to fix critical issues inside master  
* release/<version>: when preparing a release with new features a temporary release branch is created for that new release  
* bugfix/<bugfixname>: issues that are found during a release will be fixed here  
* develop: a general develop branch that will contain unreleased new features  
* feature/<featurename>: new features that will be developed and merged to the develop branch  

[A more in-depth guide about this model can be found here](https://nvie.com/posts/a-successful-git-branching-model/)

## Versioning in N.I.N.A.

N.I.N.A. utilizes the versioning scheme MAJOR.MINOR.PATCH.CHANNEL|BUILDNRXXX  
There is currently no automation used and versions are maintained manually.  

MAJOR version increases for big changes, like changing technologies etc.

MINOR version will increase for every new released version

PATCH version is reserved to apply Hotfixes to a released versions

CHANNEL|BUILDNR will not be displayed for Released versions, as these are only used to identify Release, RC, Beta and Develop versions

CHANNEL consists of the following values:  
* 1: Nightly
* 2: Beta
* 3: Release Candidate
* 9: Release

BUILDNR should be incremented each nightly build (only in develop,beta and rc versions) by using 3 digits.

Examples:
Release: 1.8.0.9001             (Displayed as "1.8")  
Release: 1.8.1.9001             (Displayed as "1.8 HF1")  
Release Candidate: 1.8.0.3001   (Displayed as "1.8 RC1")  
Beta: 1.8.0.2004                (Displayed as "1.8 BETA4")  
Develop: 1.8.0.1022             (Displayed as "1.8 NIGHTLY #022")  

## Database enhancements

N.I.N.A. uses an SQLite database to store various data. The database is located inside %LOCALAPPDATA%\NINA\NINA.sqlite. 
This database will be automatically created by the EntityFramework based on the files inside <SolutionDir>\NINA\Database\Initial and <SolutionDir>\NINA\Database\Migration
* Files inside "Initial" folder will be called when the database needs to be created from scratch.
	* Do not alter these files! Changes here won't get applied for an existing database (e.g. from a previous version)
	* In case additions have to be made to the database, add a new migration file as described below
* Files inside migration will be called depending on the current version that is returned via "PRAGMA user_version" of the existing database
	* The migration is kept simple and follows a naming convention. The migration .sql file matches the version it should migrate to. (Files are named like "1.sql", "2.sql" etc.)
	* This requires that the same user_version is set as the file name specifies inside the file (e.g. "1.sql" must contain a "PRAGMA user_version = 1;" statement)
	* Only those files where the version is greater than the current database user_version will be executed
* During migration the foreign key constraints are deactivated, for easier data manipulation. Be cautious to not corrupt data that way!
* After a migration a VACUUM will be performed

## Setting up the developer environment

* Install Visual Studio Community 2017 or better
* Install [ASCOM](https://ascom-standards.org/Downloads/Index.htm)
* Recommended Visual Studio Extensions:
    * [CodeMaid](http://www.codemaid.net/): A code cleanup Utility
    * [XAML Styler](https://github.com/Xavalon/XamlStyler/) A XAML style formatter
    * [MarkdownEditor](https://github.com/madskristensen/MarkdownEditor) To edit Markdown and auto generate HTML files
* Other external dependencies are automatically installed via nuget (except for some camera vendor DLLs)
* (Optional) External Camera Vendor SDK DLLs have to be manually put inside the project to \NINA\External\ &lt;x64 and x32&gt;\
    * To get Canon and Nikon DLLs you have to register as a developer for canon and nikon separately on their websites
	* Altair SDK: Join the altair development group at https://groups.google.com/forum/#!forum/altair-development-group
    * Due to not being publicly available, they must not be put into a public repository
* (Optional) To be able to build the setup projects you need to install [WiX](http://wixtoolset.org/) and their [Visual Studio plugin](https://marketplace.visualstudio.com/items?itemName=RobMensching.WixToolsetVisualStudio2017Extension)

## Automated Unit Tests (AUT)

* The project is using the [NUnit unit-testing framework](https://nunit.org/) to write and run AUTs
* Additionally to write easy to read assertions [Fluent Assertions](https://fluentassertions.com/) are used
  * These might seem verbose at first, but they really help reading and understanding the assertions
* For detailed information about how to use these frameworks please go to their respective homepages

## Running AUTs in Visual Studio

* First double check that your processor architecture for AUTs is set to x64
  * Test -> Test Settings -> Processor Architecture for AnyCPU Projects -> x64
  * You also might need to uncheck and re-check "Keep Test Execution Engine Running" for this setting change to become active
* Prior to running the tests, the project configuration should be set to [Debug][x64]
* Activate the test explorer
  * Test -> Windows -> Test Explorer
* Inside the test explorer you will see all detected AUTs (after building the project)
* To run all AUTs simply click on "Run All"
* You can also run and/or debug single AUTs by right clicking inside the respective method and selecting "Run Test" or "Debug Test"

## Localization

* All strings that are displayed inside the User Interface should be localized using the Locale Manager
  * In Code Behind: Locale.Loc.Instance["[Label key]"]
  * In XAML: 
    * Import namespace: xmlns:ns="clr-namespace:NINA.Locale"
    * Use via binding like Text="{ns:Loc [Label key]}"
* To introduce a new label you just need to add the new key and value into "NINA/Locale/Locale.resx" file. The other localized files will be managed by an external integration automatically.
* All translations are managed by an external page  at [Crowdin](https://nina.crowdin.com) and automatically integrated into the repository
  * For more information on how to contribute to the localization refer to our documentation in the contributing section


## Pull Requests

* Before making large changes, that will change existing patterns or disrupt ongoing features, please first discuss this via an issue or in discord, before starting to work on the changes! This way we can make sure, that it is the proper time for this change.  
* Make sure that only relevant changes are inside the pull request  
* Validate that all unit tests are sill passing  
* Test your changes *thoroughly* and give a short overview on how you tested your changes in the pull request's description
* Add yourself to the AUTHORS file, so you will be given proper credit!  
* Create **one pull request per feature/fix**
* Create your pull requests for new features only against the **develop** branch  
  * Only critical Hotfixes may be created against *master* branch and require a new PATCH version as described in [Versioning in N.I.N.A.]  
  
* Fill out the pull request description template
  
```
What is the purpose of this Pull Request?

How were the changes tested?

Are there relevant Issues in the tracker that this PR will fix?

Screenshots

Notes
```

## NINASetupBundle Prerequisites

* To provide release notes for the setup bundle, there is a build event using "pandoc" that creates an rtf file out of RELEASE_NOTES.md
* It is expected inside the folder "%LOCALAPPDATA%\Pandoc\pandoc.exe"
* Setup can be downloaded at https://pandoc.org/installing.html