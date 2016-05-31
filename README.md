# This is a VS2015 plugin for several handy refactorings and code fixes missing in VS2015

## Currently implemented refactorings

### Inject via constructor

Activated when there is a field declared inside a class, and gives an options to inject this field's value via constructor (DI)

### Introduce field

Activated when there is a parameter for constructor, and automatically adds private field and initializes it within constructor.

### Propagate constructor parameters

Activated when there is a base constructor call but not all necessary params are supplied (for example, base class constructor is modified). Then it offers to propagate necessary params in current constructor to base.

Currently, no VSIX is deployed to Visual Studio Gallery, so if you want to use it, you have to build it yourself.