# Contributing

Thank you for considering a contribution to NINA!
There are many areas where you can contribute, ranging from improving the documentation, writing tutorials, submitting bugs or even writing code for new features inside NINA itself.
When contributing code or documentation to this repository, please first discuss your plans via an issue, discord or mail with the repo owners, before making a change

## Contributing code

### Quick Start
1. Fork the repository
2. Add your changes
3. Check that unit tests are passing
4. Make sure no unnecessary files are accidentally checked in.
5. Push the change to your forked repository using a good commit message
6. Submit a pull request
7. During the pull requests there will be discussions and constructive feedback.
   Required changes that might be requested during this phase have to be implemented.
   Once this is done, the pull request can be merged.

### Coding rules

* Always be backwards compatible when having some major rework of a module (e.g. settings change)
* Follow clean code guidelines. There are many resources about this topic available online.
* Try to unit test your code

### Branching model

This project is utilizing a standard git flow where it has the following branches
* master: all officially released code
* hotfix/&lt;hotfixname&gt;: used to fix critical issues inside master
* release/&lt;version&gt;: when preparing a release with new features a temporary release branch is created for that new release
* bugfix/&lt;bugfixname&gt;: issues that are found during a release will be fixed here
* develop: a general develop branch that will contain unreleased new features
* feature/&lt;featurename&gt;: new features that will be developed and merged to the develop branch

[A more in-depth guide about this model can be found here](https://nvie.com/posts/a-successful-git-branching-model/)

### Setting up the developer environment

* Install Visual Studio Community 2017 or better
* External dependencies are automatically installed via nuget (except Camera vendor DLLs)
* External Camera Vendor SDK DLLs have to be manually put inside the project to \NINA\External\ &lt;x64 and x32&gt;\
    * To get Canon and Nikon DLLs you have to register as a developer for canon and nikon separately on their websites
	* Altair SDK: reach out to AltairAstro. They can provide you with their sdk. Contact details at https://cameras.altairastro.com/
	* ASI SDK: SDK is available at https://astronomy-imaging-camera.com/software-drivers section "For Developers"
	* Atik SDK: SDK is available at https://www.atik-cameras.com/downloads/
    * Due to licensing of those files, they must not be put into a public repository
* (Optional) To be able to build the setup projects you need to install [WiX](http://wixtoolset.org/) and their [Visual Studio plugin](https://marketplace.visualstudio.com/items?itemName=RobMensching.WixToolsetVisualStudio2017Extension)